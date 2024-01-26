Shader "ZibraSmokeAndFires/SmokeShader"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite On
            ZTest Always

            HLSLPROGRAM

            // Physically based Standard lighting model
            #pragma multi_compile_local __ HDRP
            #pragma multi_compile_local __ CUSTOM_REFLECTION_PROBE
            #pragma multi_compile_local __ VISUALIZE_SDF
            #pragma multi_compile_local __ FLIP_BACKGROUND
            #pragma multi_compile_local __ UNDERWATER_RENDER
            #pragma multi_compile_local __ DOWNSCALE
            #pragma instancing_options procedural:setup
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityImageBasedLighting.cginc"

            struct VSIn
            {
                uint vertexID : SV_VertexID;
            };

            struct VSOut
            {
                float4 position : POSITION;
                float3 raydir : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };
            
            struct PSOut
            {
                float4 color : COLOR;
            };
            
            // Camera params
            float2 TextureScale;

            // Input resources
            sampler2D Background;
            float4 Background_TexelSize;
           
            sampler2D ParticlesTex;

            // built-in Unity sampler name - do not change
            sampler2D _CameraDepthTexture;
            
            #include <RenderingUtils.cginc>

            float2 GetFlippedUV(float2 uv)
            {
                if (_ProjectionParams.x > 0)
                    return float2(uv.x, 1 - uv.y);
                return uv;
            }

            float2 GetFlippedUVBackground(float2 uv)
            {
                uv = GetFlippedUV(uv);
#ifdef FLIP_BACKGROUND
                // Temporary fix for flipped reflection on iOS
                uv.y = 1 - uv.y;
#else
                if (Background_TexelSize.y < 0)
                {
                    uv.y = 1 - uv.y;
                }
#endif
                return uv;
            }

            float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
            {
                float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);

            #if UNITY_UV_STARTS_AT_TOP
                positionCS.y = -positionCS.y;
            #endif

                return positionCS;
            }

            float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
            {
                float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
                float4 hpositionWS = mul(invViewProjMatrix, positionCS);
                return hpositionWS.xyz / hpositionWS.w;
            }

            float3 DepthToWorld(float2 uv, float depth)
            {
                return ComputeWorldSpacePosition(uv, depth, ViewProjectionInverse);
            }

            float4 GetDepthAndPos(float2 uv)
            {
                float depth = tex2D(_CameraDepthTexture, uv).x;
                float3 pos = DepthToWorld(uv, depth);
                return float4(pos, depth);
            }

            float PositionToDepth(float3 pos)
            {
                float4 clipPos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                return (1.0 / clipPos.w - _ZBufferParams.w) / _ZBufferParams.z; //inverse of linearEyeDepth
            }

            float3 PositionToScreen(float3 pos)
            {
                float4 clipPos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                clipPos = ComputeScreenPos(clipPos); 

                return float3(clipPos.xy/clipPos.w, (1.0 / clipPos.w - _ZBufferParams.w) / _ZBufferParams.z); 
            }

            float3 BoxProjection(float3 rayOrigin, float3 rayDir, float3 cubemapPosition, float3 boxMin, float3 boxMax)
            {
                float3 tMin = (boxMin - rayOrigin) / rayDir;
                float3 tMax = (boxMax - rayOrigin) / rayDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tFar = min(min(t2.x, t2.y), t2.z);
                return normalize(rayOrigin + rayDir*tFar - cubemapPosition);
            };

#ifdef HDRP
            Texture2D<float2> _CameraExposureTexture;
#endif


            VSOut VSMain(VSIn input)
            {
                VSOut output;

                float2 vertexBuffer[4] = {
                    float2(0.0f, 0.0f),
                    float2(0.0f, 1.0f),
                    float2(1.0f, 0.0f),
                    float2(1.0f, 1.0f),
                };
                uint indexBuffer[6] = { 0, 1, 2, 2, 1, 3 };
                uint indexID = indexBuffer[input.vertexID];

                float2 uv = vertexBuffer[indexID];
                float2 flippedUV = GetFlippedUV(uv);

                output.position = float4(2 * flippedUV.x - 1, 1 - 2 * flippedUV.y, 0.5, 1.0);
                output.uv = uv;
                output.raydir = GetCameraRay(uv);
                
                return output;
            }

            float RayMarchResolutionDownscale;
            float4 MeshRenderData_TexelSize;

            PSOut PSMain(VSOut input)
            {
                PSOut output;

                float3 cameraPos = _WorldSpaceCameraPos;
                float3 cameraRay = normalize(input.raydir);
                int3 pixelCoord = int3(input.position.xy, 0);
#if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x > 0)
                {
                    pixelCoord.y = MeshRenderData_TexelSize.w - pixelCoord.y;
                }
#endif

                float sceneDepth = tex2D(_CameraDepthTexture, input.uv).x;
#if !defined(UNITY_REVERSED_Z)
                sceneDepth = 1.0 - sceneDepth;
#endif
               
                RayProperties prop = {float3(1.0, 1.0, 1.0), float3(0.0, 0.0, 0.0)};

                float4 scenePos = GetDepthAndPos(input.uv);
                int timeSeed = int(_Time.y * 44328);
                DitherValues = (BlueNoise.Load(uint3(pixelCoord + int3(timeSeed, timeSeed, 0)) % 1024u) - 0.5);

                TraceRay(cameraPos, cameraRay, distance(cameraPos, scenePos.xyz), prop);
                
#ifndef DOWNSCALE
                float simulationScale =
                    (1.0f / 3.0f) * (ContainerScale.x + ContainerScale.y + ContainerScale.z);
             
                float3 lightmapShadow = (IlluminationShadows == 0) ? 1.0 : GetLightmapShadow(scenePos.xyz);
                float3 primaryShadow = (PrimaryShadows == 0) ? 1.0 : exp(-ShadowColor * SampleShadowmapSmooth(scenePos.xyz, simulationScale));
                float3 shadow = lightmapShadow * primaryShadow;
                float3 background_color = lerp(1.0, shadow, FakeShadows) * tex2D(Background, GetFlippedUVBackground(input.uv)).xyz;
#endif

#ifdef HDRP
                prop.incoming *= _CameraExposureTexture[int2(0, 0)].x;
#endif

#ifdef DOWNSCALE
                output.color = float4(prop.incoming, 1.0 - Sum(prop.absorption)/3.0);
#else
                output.color = float4(background_color * prop.absorption + prop.incoming, 1.0);
#endif

                output.color.xyz += tex2D(ParticlesTex, input.uv).xyz;

                return output;
            }
            ENDHLSL
        }
    }
}
