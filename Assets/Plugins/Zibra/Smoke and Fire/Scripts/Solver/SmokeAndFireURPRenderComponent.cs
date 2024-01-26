#if UNITY_PIPELINE_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using com.zibraai.smoke_and_fire.Solver;

namespace com.zibraai.smoke_and_fire
{
    public class SmokeAndFireURPRenderComponent : ScriptableRendererFeature
    {
        [System.Serializable]
        public class SmokeAndFireURPRenderSettings
        {
            // we're free to put whatever we want here, public fields will be exposed in the inspector
            public bool IsEnabled = true;
            public RenderPassEvent InjectionPoint = RenderPassEvent.AfterRenderingTransparents;
        }
        // Must be called exactly "settings" so Unity shows this as render feature settings in editor
        public SmokeAndFireURPRenderSettings settings = new SmokeAndFireURPRenderSettings();

        public class CopyBackgroundURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            RenderTargetIdentifier cameraColorTexture;

            public CopyBackgroundURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

#if UNITY_PIPELINE_URP_9_0_OR_HIGHER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            }
#else
            public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                cameraColorTexture = renderer.cameraColorTarget;
            }
#endif

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;

                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                if (smokeAndFire.cameraResources.ContainsKey(camera))
                {
#if UNITY_PIPELINE_URP_9_0_OR_HIGHER
                    Blit(cmd, cameraColorTexture, smokeAndFire.cameraResources[camera].background);
#else
                    // For some reason old version of URP don't want to blit texture via correct API
                    cmd.Blit(cameraColorTexture, smokeAndFire.cameraResources[camera].background);
#endif
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        public class SmokeAndFireNativeRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            public SmokeAndFireNativeRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (smokeAndFire && smokeAndFire.IsRenderingEnabled())
                {
                    Camera camera = renderingData.cameraData.camera;
                    camera.depthTextureMode = DepthTextureMode.Depth;
                    CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                    smokeAndFire.RenderCallBack(renderingData.cameraData.camera, renderingData.cameraData.renderScale);

                    smokeAndFire.RenderFluid(cmd, renderingData.cameraData.camera);

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }

        public class SmokeAndFireURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            RenderTargetIdentifier cameraColorTexture;

            static int upscaleColorTextureID = Shader.PropertyToID("ZibraSmokeAndFire_SmokeAndFireTempColorTexture");
            RenderTargetIdentifier upscaleColorTexture;

            public SmokeAndFireURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

#if UNITY_PIPELINE_URP_9_0_OR_HIGHER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            }
#else
            public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                cameraColorTexture = renderer.cameraColorTarget;
            }
#endif

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (smokeAndFire.EnableDownscale)
                {
                    RenderTextureDescriptor descriptor = cameraTextureDescriptor;

                    Vector2Int dimensions = new Vector2Int(descriptor.width, descriptor.height);
                    dimensions = smokeAndFire.ApplyDownscaleFactor(dimensions);
                    descriptor.width = dimensions.x;
                    descriptor.height = dimensions.y;

                    descriptor.msaaSamples = 1;

                    descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
                    descriptor.depthBufferBits = 0;

                    cmd.GetTemporaryRT(upscaleColorTextureID, descriptor, FilterMode.Bilinear);

                    upscaleColorTexture = new RenderTargetIdentifier(upscaleColorTextureID);
                    ConfigureTarget(upscaleColorTexture);
                    ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));
                }
                else
                {
                    ConfigureTarget(cameraColorTexture);
                    // ConfigureClear seems to be persistent, so need to reset it
                    ConfigureClear(ClearFlag.None, new Color(0, 0, 0, 0));
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                camera.depthTextureMode = DepthTextureMode.Depth;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                if (!smokeAndFire.EnableDownscale)
                {
                    cmd.SetRenderTarget(cameraColorTexture);
                }

                smokeAndFire.RenderSmokeAndFireMain(cmd, camera);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

#if UNITY_PIPELINE_URP_9_0_OR_HIGHER
            public override void OnCameraCleanup(CommandBuffer cmd)
#else
            public override void FrameCleanup(CommandBuffer cmd)
#endif
            {
                if (smokeAndFire.EnableDownscale)
                {
                    cmd.ReleaseTemporaryRT(upscaleColorTextureID);
                }
            }
        }

        public class SmokeAndFireUpscaleURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            static int upscaleColorTextureID = Shader.PropertyToID("ZibraSmokeAndFire_SmokeAndFireTempColorTexture");
            RenderTargetIdentifier upscaleColorTexture;

            public SmokeAndFireUpscaleURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                camera.depthTextureMode = DepthTextureMode.Depth;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                upscaleColorTexture = new RenderTargetIdentifier(upscaleColorTextureID);
                smokeAndFire.UpscaleSmokeAndFireDirect(cmd, camera, upscaleColorTexture);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        // 1 pass per rendered simulation that requires background copy
        public CopyBackgroundURPRenderPass[] copyPasses;
        // 1 pass per rendered simulation
        public SmokeAndFireNativeRenderPass[] nativePasses;
        // 1 pass per rendered simulation
        public SmokeAndFireURPRenderPass[] smokeAndFireURPPasses;
        // 1 pass per rendered simulation that have downscale enabled
        public SmokeAndFireUpscaleURPRenderPass[] upscalePasses;

        public override void Create()
        {
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.IsEnabled)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            camera.depthTextureMode = DepthTextureMode.Depth;

            int simulationsToRenderCount = 0;
            int backgroundsToCopyCount = 0;
            int simulationsToUpscaleCount = 0;

            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null && instance.initialized)
                {
                    simulationsToRenderCount++;
                    if (instance.EnableDownscale)
                    {
                        simulationsToUpscaleCount++;
                    }
                    if (instance.IsBackgroundCopyNeeded(camera))
                    {
                        backgroundsToCopyCount++;
                    }
                }
            }

            if (copyPasses == null || copyPasses.Length != backgroundsToCopyCount)
            {
                copyPasses = new CopyBackgroundURPRenderPass[backgroundsToCopyCount];
                for (int i = 0; i < backgroundsToCopyCount; ++i)
                {
                    copyPasses[i] = new CopyBackgroundURPRenderPass(settings.InjectionPoint);
                }
            }

            if (nativePasses == null || nativePasses.Length != simulationsToRenderCount)
            {
                nativePasses = new SmokeAndFireNativeRenderPass[simulationsToRenderCount];
                for (int i = 0; i < simulationsToRenderCount; ++i)
                {
                    nativePasses[i] = new SmokeAndFireNativeRenderPass(settings.InjectionPoint);
                }
            }

            if (smokeAndFireURPPasses == null || smokeAndFireURPPasses.Length != simulationsToRenderCount)
            {
                smokeAndFireURPPasses = new SmokeAndFireURPRenderPass[simulationsToRenderCount];
                for (int i = 0; i < simulationsToRenderCount; ++i)
                {
                    smokeAndFireURPPasses[i] = new SmokeAndFireURPRenderPass(settings.InjectionPoint);
                }
            }

            if (upscalePasses == null || upscalePasses.Length != simulationsToUpscaleCount)
            {
                upscalePasses = new SmokeAndFireUpscaleURPRenderPass[simulationsToUpscaleCount];
                for (int i = 0; i < simulationsToUpscaleCount; ++i)
                {
                    upscalePasses[i] = new SmokeAndFireUpscaleURPRenderPass(settings.InjectionPoint);
                }
            }

            int currentCopyPass = 0;
            int currentSmokeAndFirePass = 0;
            int currentUpscalePass = 0;

            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null && instance.IsRenderingEnabled() &&
                    ((camera.cullingMask & (1 << instance.gameObject.layer)) != 0))
                {
                    if (instance.IsBackgroundCopyNeeded(camera))
                    {
                        copyPasses[currentCopyPass].smokeAndFire = instance;

#if UNITY_PIPELINE_URP_10_0_OR_HIGHER
                        copyPasses[currentCopyPass].ConfigureInput(ScriptableRenderPassInput.Color |
                                                                   ScriptableRenderPassInput.Depth);
#endif
                        copyPasses[currentCopyPass].renderPassEvent = settings.InjectionPoint;

                        renderer.EnqueuePass(copyPasses[currentCopyPass]);
                        currentCopyPass++;
                    }

                    nativePasses[currentSmokeAndFirePass].smokeAndFire = instance;
                    nativePasses[currentSmokeAndFirePass].renderPassEvent = settings.InjectionPoint;
                    renderer.EnqueuePass(nativePasses[currentSmokeAndFirePass]);

                    smokeAndFireURPPasses[currentSmokeAndFirePass].smokeAndFire = instance;
#if UNITY_PIPELINE_URP_10_0_OR_HIGHER
                    smokeAndFireURPPasses[currentSmokeAndFirePass].ConfigureInput(ScriptableRenderPassInput.Color |
                                                                      ScriptableRenderPassInput.Depth);
#endif

#if !UNITY_PIPELINE_URP_9_0_OR_HIGHER
                    smokeAndFireURPPasses[currentSmokeAndFirePass].Setup(renderer, ref renderingData);
#endif
                    smokeAndFireURPPasses[currentSmokeAndFirePass].renderPassEvent = settings.InjectionPoint;

                    renderer.EnqueuePass(smokeAndFireURPPasses[currentSmokeAndFirePass]);
                    currentSmokeAndFirePass++;
                    if (instance.EnableDownscale)
                    {
                        upscalePasses[currentUpscalePass].smokeAndFire = instance;

                        upscalePasses[currentUpscalePass].renderPassEvent = settings.InjectionPoint;

                        renderer.EnqueuePass(upscalePasses[currentUpscalePass]);
                        currentUpscalePass++;
                    }
                }
            }
        }
    }
}

#endif // UNITY_PIPELINE_HDRP