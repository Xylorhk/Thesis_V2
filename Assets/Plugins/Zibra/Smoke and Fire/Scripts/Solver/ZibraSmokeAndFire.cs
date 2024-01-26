using com.zibraai.smoke_and_fire.DataStructures;
using com.zibraai.smoke_and_fire.Manipulators;
using com.zibraai.smoke_and_fire.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif // UNITY_PIPELINE_HDRP

#if !ZIBRA_SMOKE_AND_FIRE_PAID_VERSION && !ZIBRA_SMOKE_AND_FIRE_FREE_VERSION
#error Missing plugin version definition
#endif

namespace com.zibraai.smoke_and_fire.Solver
{
    /// <summary>
    /// Main ZibraFluid solver component
    /// </summary>
    [AddComponentMenu("Zibra/Zibra Smoke & Fire")]
    [RequireComponent(typeof(ZibraSmokeAndFireMaterialParameters))]
    [RequireComponent(typeof(ZibraSmokeAndFireSolverParameters))]
    [RequireComponent(typeof(ZibraManipulatorManager))]
    [ExecuteInEditMode] // Careful! This makes script execute in edit mode.
    // Use "EditorApplication.isPlaying" for play mode only check.
    // Encase this check and "using UnityEditor" in "#if UNITY_EDITOR" preprocessor directive to prevent build errors
    public class ZibraSmokeAndFire : MonoBehaviour
    {
        #region STATIC
        /// <summary>
        /// A list of all instances of the ZibraFluid solver
        /// </summary>
        public static List<ZibraSmokeAndFire> AllInstances = new List<ZibraSmokeAndFire>();
        public static int ms_NextInstanceId = 0;

#if UNITY_PIPELINE_URP
        static int upscaleColorTextureID = Shader.PropertyToID("Zibra_DownscaledSmokeAndFireColor");
        static int upscaleDepthTextureID = Shader.PropertyToID("Zibra_DownscaledSmokeAndFireDepth");
#endif
        #endregion

        #region CONSTANTS

        public const string PluginVersion = "Early Access 19.01.2023";
        public const int STATISTICS_PER_MANIPULATOR = 8;
        public const int WORKGROUP_SIZE_X = 8;
        public const int WORKGROUP_SIZE_Y = 8;
        public const int WORKGROUP_SIZE_Z = 6;
        public const int PARTICLE_WORKGROUP = 256;
        public const int DEPTH_COPY_WORKGROUP = 16;
        public const int MAX_LIGHT_COUNT = 16;
        public const int RANDOM_TEX_SIZE = 64;
        public const int EMITTER_GRADIENT_TEX_WIDTH = 48;
        public const int EMITTER_SPRITE_TEX_SIZE = 64;
        public const float EMITTER_PARTICLE_SIZE_SCALE = .1f;

        #endregion

        #region STRUCTURES
        [StructLayout(LayoutKind.Sequential)]
        private class UnityTextureBridge
        {
            public IntPtr texture;
            public ZibraSmokeAndFireBridge.TextureFormat format;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterBuffersBridgeParams
        {
            public IntPtr SimulationParams;
            public UnityTextureBridge RenderDensity;
            public UnityTextureBridge RenderColor;
            public UnityTextureBridge RenderIllumination;
            public UnityTextureBridge ColorTexture0;
            public UnityTextureBridge VelocityTexture0;
            public UnityTextureBridge ColorTexture1;
            public UnityTextureBridge VelocityTexture1;
            public UnityTextureBridge TmpSDFTexture;
            public UnityTextureBridge Divergence;
            public UnityTextureBridge ResidualLOD0;
            public UnityTextureBridge ResidualLOD1;
            public UnityTextureBridge ResidualLOD2;
            public UnityTextureBridge Pressure0LOD0;
            public UnityTextureBridge Pressure0LOD1;
            public UnityTextureBridge Pressure0LOD2;
            public UnityTextureBridge Pressure1LOD0;
            public UnityTextureBridge Pressure1LOD1;
            public UnityTextureBridge Pressure1LOD2;
            public IntPtr AtomicCounters;
            public UnityTextureBridge RandomTexture;
            public TextureUploadData RandomData;
            public IntPtr EffectParticleData0;
            public IntPtr EffectParticleData1;
            public UnityTextureBridge RenderDensityLOD;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterRenderResourcesBridgeParams
        {
            public UnityTextureBridge ParticleColors;
            public UnityTextureBridge ParticleSprites;
            public UnityTextureBridge Depth;
            public UnityTextureBridge ParticlesRT;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class InitializeGPUReadbackParams
        {
            public UInt32 readbackBufferSize;
            public Int32 maxFramesInFlight;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TextureUploadData
        {
            public IntPtr data;
            public Int32 dataSize;
            public Int32 rowPitch;
            public Int32 dimensionX;
            public Int32 dimensionY;
            public Int32 dimensionZ;
        };

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterManipulatorsBridgeParams
        {
            public Int32 ManipulatorNum;
            public IntPtr ManipulatorBufferDynamic;
            public IntPtr SDFObjectBuffer;
            public IntPtr ManipulatorBufferStatistics;
            public IntPtr ManipulatorParams;
            public Int32 SDFObjectCount;
            public IntPtr SDFObjectData;
            public IntPtr ManipIndices;
            public UnityTextureBridge EmbeddingsTexture;
            public UnityTextureBridge SDFGridTexture;
            public TextureUploadData EmbeddigsData;
            public TextureUploadData SDFGridData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SimulationParams
        {
            public Vector3 GridSize;
            public Int32 NodeCount;

            public Vector3 ContainerScale;
            public Single MinimumVelocity;

            public Vector3 ContainerPos;
            public Single MaximumVelocity;

            public Single TimeStep;
            public Single SimulationTime;
            public Int32 SimulationFrame;
            public Int32 JacobiIterations;

            public Single ColorDecay;
            public Single VelocityDecay;
            public Single PressureReuse;
            public Single PressureReuseClamp;

            public Single Sharpen;
            public Single SharpenThreshold;
            public Single PressureProjection;
            public Single PressureClamp;

            public Vector3 Gravity;
            public Single SmokeBuoyancy;

            public Int32 LOD0Iterations;
            public Int32 LOD1Iterations;
            public Int32 LOD2Iterations;
            public Int32 PreIterations;

            public Single MainOverrelax;
            public Single EdgeOverrelax;
            public Single VolumeEdgeFadeoff;
            public Int32 SimulationIterations;

            public Vector3 SimulationContainerPosition;
            public Int32 SimulationMode;

            public Vector3 PreviousContainerPosition;
            public Int32 FixVolumeWorldPosition;

            public Single TempThreshold;
            public Single HeatEmission;
            public Single ReactionSpeed;
            public Single HeatBuoyancy;

            public Single SmokeDensity;
            public Single FuelDensity;
            public Single TemperatureDensityDependence;
            public Single FireBrightness;

            public int MaxEffectParticleCount;
            public int ParticleLifetime;
            public int padding0;
            public int padding1;

            public Vector3 GridSizeLOD;
            public int GridDownscale;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RenderParams
        {
            public Matrix4x4 View;
            public Matrix4x4 Projection;
            public Matrix4x4 ProjectionInverse;
            public Matrix4x4 ViewProjection;
            public Matrix4x4 ViewProjectionInverse;
            public Matrix4x4 EyeRayCameraCoeficients;
            public Vector3 WorldSpaceCameraPos;
            public Int32 CameraID;
            public Vector4 ZBufferParams;
            public Vector2 CameraResolution;
            Single CameraParamsPadding1;
            Single CameraParamsPadding2;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct DebugTimestampItem
        {
            public uint EventType;
            public float ExecutionTime;
        }
        public struct MaterialPair
        {
            public Material currentMaterial;
            public Material sharedMaterial;

            // Returns true if dirty
            public bool SetMaterial(Material mat)
            {
                if (sharedMaterial != mat)
                {
                    currentMaterial = (mat != null ? Material.Instantiate(mat) : null);
                    sharedMaterial = mat;
                    return true;
                }
                return false;
            }
        }

        public class CameraResources
        {
            public RenderTexture background;
            public MaterialPair smokeAndFireMaterial;
            public MaterialPair upscaleMaterial;
            public bool isDirty = true;
        }
        #endregion

        #region PARAMETERS

        public enum SimulationMode
        {
            Smoke,
            ColoredSmoke,
            Fire,
        };

        public SimulationMode CurrentSimulationMode;
        public bool forceTextureUpdate = false;


        /// <summary>
        /// Current timestep
        /// </summary>
        public float timestep = 0.0f;

        /// <summary>
        /// Simulation time passed (in simulation time units)
        /// </summary>
        public float simulationInternalTime { get; private set; } = 0.0f;

        /// <summary>
        /// Number of simulation iterations done so far
        /// </summary>
        public int simulationInternalFrame { get; private set; } = 0;

        /// <summary>
        /// The grid size of the simulation
        /// </summary>
        public Vector3Int GridSize { get; private set; }
        public Vector3Int GridSizeLOD { get; private set; }

        private int GridDownscale = 1;
        private int NumNodes;

        [Tooltip("Main directional light")]
        public Light mainLight;

        [Tooltip("List of additional lights")]
        public List<Light> lights;

        [Tooltip("The maximum allowed simulation timestep")]
        [Range(0.0f, 3.0f)]
        public float timeStep = 1.00f;

        [Tooltip("Fallback max frame latency. Used when it isn't possible to retrieve Unity's max frame latency.")]
        [Range(2, 16)]
        public UInt32 maxFramesInFlight = 3;

        [Tooltip("The number of solver iterations per frame, in most cases one iteration is sufficient")]
        [Range(1, 10)]
        public int SimulationIterations = 3;

        public float CellSize { get; private set; }

        [Tooltip("Sets the resolution of the largest sid of the grids container equal to this value")]
        [Min(16)]
        public int gridResolution = 128;

        public bool runSimulation = true;
        public bool runRendering = true;
        public bool fixVolumeWorldPosition = true;

        [NonSerialized]
        public bool isEnabled = true;
        public Bounds bounds;

        // If set to false resolution is always 100%
        // If set to true DownscaleFactor is applied to simulation rendering
        public bool EnableDownscale = false;

        // Scale width/height of smoke & fire render target
        // Pixel count is decreased by factor of DownscaleFactor * DownscaleFactor
        // So DownscaleFactor of 0.7 result in about 50% less pixels in render target
        // Doesn't have any effect unless EnableDownscale is set to true
        [Range(0.2f, 0.99f)]
        public float DownscaleFactor = 0.5f;

        /// <summary>
        /// Solver container size
        /// </summary>
        public Vector3 containerSize = new Vector3(10, 10, 10);

        /// <summary>
        /// Solver container position
        /// </summary>
        public Vector3 containerPos;

        public Vector3 simulationContainerPosition;
        private bool isSimulationContainerPositionChanged;

        // Only used on SRP
        public CameraEvent CurrentInjectionPoint = CameraEvent.BeforeForwardAlpha;
        private CameraEvent ActiveInjectionPoint = CameraEvent.BeforeForwardAlpha;

        /// <summary>
        /// List of used manipulators
        /// </summary>
        [SerializeField]
        private List<Manipulator> manipulators = new List<Manipulator>();

        #endregion

        #region COMPONENTS
        /// <summary>
        /// Main parameters of the simulation
        /// </summary>
        public ZibraSmokeAndFireSolverParameters solverParameters;

        /// <summary>
        /// Main rendering parameters
        /// </summary>
        public ZibraSmokeAndFireMaterialParameters materialParameters;

        /// <summary>
        /// Manager for all objects interacting in some way with the simulation
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public ZibraManipulatorManager manipulatorManager;


#if UNITY_PIPELINE_HDRP
        private SmokeAndFireHDRPRenderComponent hdrpRenderer;
#endif // UNITY_PIPELINE_HDRP

        #endregion

        #region RESOURCES
        //Render resources
        public RenderTexture UpscaleColor { get; private set; }
        public RenderTexture Shadowmap { get; private set; }
        public RenderTexture Lightmap { get; private set; }
        public RenderTexture CameraOcclusion { get; private set; }

        //Simulation resources
        public RenderTexture RenderDensity { get; private set; }
        public RenderTexture RenderDensityLOD { get; private set; }
        public RenderTexture RenderColor { get; private set; }
        public RenderTexture RenderIllumination { get; private set; }
        public RenderTexture ColorTexture0 { get; private set; }
        public RenderTexture VelocityTexture0 { get; private set; }
        public RenderTexture ColorTexture1 { get; private set; }
        public RenderTexture VelocityTexture1 { get; private set; }
        public RenderTexture TmpSDFTexture { get; private set; }

        public RenderTexture Divergence { get; private set; }
        public RenderTexture ResidualLOD0 { get; private set; }
        public RenderTexture ResidualLOD1 { get; private set; }
        public RenderTexture ResidualLOD2 { get; private set; }
        public RenderTexture Pressure0LOD0 { get; private set; }
        public RenderTexture Pressure0LOD1 { get; private set; }
        public RenderTexture Pressure0LOD2 { get; private set; }
        public RenderTexture Pressure1LOD0 { get; private set; }
        public RenderTexture Pressure1LOD1 { get; private set; }
        public RenderTexture Pressure1LOD2 { get; private set; }

        public ComputeBuffer AtomicCounters { get; private set; }
        public ComputeBuffer EffectParticleData0 { get; private set; }
        public ComputeBuffer EffectParticleData1 { get; private set; }

        public Texture3D RandomTexture { get; private set; }
        public RenderTexture DepthTexture { get; private set; }
        public RenderTexture ParticlesRT { get; private set; }

        //Manipulator resources
        public ComputeBuffer DynamicManipulatorData { get; private set; }
        public ComputeBuffer SDFObjectData { get; private set; }
        public ComputeBuffer ManipulatorStatistics { get; private set; }
        public Texture3D SDFGridTexture { get; private set; }
        public Texture3D EmbeddingsTexture { get; private set; }
        public Texture2D EmittersColorsTexture { get; private set; }
        public Texture3D EmittersSpriteTexture { get; private set; }

        //Shaders
        private ComputeShader Renderer;
        private int ShadowmapID;
        private int LightmapID;
        private int IlluminationID;
        private int CopyDepthID;
        private Vector3Int WorkGroupsXYZ;
        private int MaxEffectParticleWorkgroups;
        private Vector3Int ShadowWorkGroupsXYZ;
        private Vector3Int LightWorkGroupsXYZ;
        private Vector3Int DownscaleXYZ;
        #endregion

        #region NATIVE RESOURCES

        private RenderParams cameraRenderParams;
        private SimulationParams simulationParams;
        // We don't know exact number of DebugTimestampsItems returned from native plugin
        // because several events (like UpdateRenderParams) can be triggered many times
        // per frame. For our current needs 100 should be enough
        [NonSerialized]
        public DebugTimestampItem[] DebugTimestampsItems = new DebugTimestampItem[100];
        [NonSerialized]
        public uint DebugTimestampsItemsCount = 0;

        private IntPtr NativeManipData;
        private IntPtr NativeSDFData;
        private IntPtr NativeSimulationData;

        private List<IntPtr> toFreeOnExit = new List<IntPtr>();

        [NonSerialized]
        private Vector2Int CurrentTextureResolution = new Vector2Int(0, 0);

        // List of all cameras we have added a command buffer to
        private readonly Dictionary<Camera, CommandBuffer> cameraCBs = new Dictionary<Camera, CommandBuffer>();

        // Each camera needs its own resources
        List<Camera> cameras = new List<Camera>();

        public Dictionary<Camera, CameraResources> cameraResources = new Dictionary<Camera, CameraResources>();

        public Dictionary<Camera, IntPtr> camNativeParams = new Dictionary<Camera, IntPtr>();
        Dictionary<Camera, IntPtr> camMeshRenderParams = new Dictionary<Camera, IntPtr>();
        Dictionary<Camera, Vector2Int> camRenderResolutions = new Dictionary<Camera, Vector2Int>();
        Dictionary<Camera, Vector2Int> camNativeResolutions = new Dictionary<Camera, Vector2Int>();
        #endregion

        #region SOLVER
        /// <summary>
        /// Native solver instance ID number
        /// </summary>
        [NonSerialized]
        public int CurrentInstanceID;

        private CommandBuffer solverCommandBuffer;

        /// <summary>
        /// Is solver initialized
        /// </summary>
        public bool initialized { get; private set; } = false;

        /// <summary>
        /// Is solver using fixed unity time steps
        /// </summary>
        public bool limitFramerate = false;
        [Min(0.0f)]
        public float maximumFramerate = 60.0f;
        private float timeAccumulation = 0.0f;

        #endregion

#if UNITY_EDITOR

        #region EDITOR
        private bool ForceRepaint = false;

        // Used to update editors
        public event Action onChanged;
        public void NotifyChange()
        {
            if (onChanged != null)
            {
                onChanged.Invoke();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, containerSize);
            Gizmos.color = Color.cyan;
            Vector3 voxelSize =
                new Vector3(containerSize.x / GridSize.x, containerSize.y / GridSize.y, containerSize.z / GridSize.z);
            const int GizmosVoxelCubeSize = 2;
            for (int i = -GizmosVoxelCubeSize; i <= GizmosVoxelCubeSize; i++)
                for (int j = -GizmosVoxelCubeSize; j <= GizmosVoxelCubeSize; j++)
                    for (int k = -GizmosVoxelCubeSize; k <= GizmosVoxelCubeSize; k++)
                        Gizmos.DrawWireCube(transform.position +
                                                new Vector3(i * voxelSize.x, j * voxelSize.y, k * voxelSize.z),
                                            voxelSize);
        }

        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }
        #endregion

#endif
        #region NATIVE UTILITIES

        IntPtr GetNativePtr(ComputeBuffer buffer)
        {
            return buffer == null ? IntPtr.Zero : buffer.GetNativeBufferPtr();
        }

        IntPtr GetNativePtr(GraphicsBuffer buffer)
        {
            return buffer == null ? IntPtr.Zero : buffer.GetNativeBufferPtr();
        }

        IntPtr GetNativePtr(RenderTexture texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        IntPtr GetNativePtr(Texture2D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        IntPtr GetNativePtr(Texture3D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        UnityTextureBridge MakeTextureNativeBridge(RenderTexture texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            if (texture != null)
            {
                unityTextureBridge.texture = GetNativePtr(texture);
                unityTextureBridge.format = ZibraSmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);
            }
            else
            {
                unityTextureBridge.texture = IntPtr.Zero;
                unityTextureBridge.format = ZibraSmokeAndFireBridge.TextureFormat.None;
            }

            return unityTextureBridge;
        }

        UnityTextureBridge MakeTextureNativeBridge(Texture3D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = ZibraSmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        UnityTextureBridge MakeTextureNativeBridge(Texture2D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = ZibraSmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        void SetInteropBuffer<T>(IntPtr NativeBuffer, List<T> list)
        {
            long LongPtr = NativeBuffer.ToInt64(); // Must work both on x86 and x64
            for (int I = 0; I < list.Count; I++)
            {
                IntPtr Ptr = new IntPtr(LongPtr);
                Marshal.StructureToPtr(list[I], Ptr, true);
                LongPtr += Marshal.SizeOf(typeof(T));
            }
        }
        #endregion

        #region SOLVER FUNCTIONS

        /// <summary>
        /// Activate the solver
        /// </summary>
        public void Run()
        {
            runSimulation = true;
        }

        /// <summary>
        /// Stop the solver
        /// </summary>
        public void Stop()
        {
            runSimulation = false;
        }

        void SetupScriptableRenderComponents()
        {
#if UNITY_PIPELINE_HDRP
#if UNITY_EDITOR
            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
                hdrpRenderer = gameObject.GetComponent<SmokeAndFireHDRPRenderComponent>();
                if (hdrpRenderer == null)
                {
                    hdrpRenderer = gameObject.AddComponent<SmokeAndFireHDRPRenderComponent>();
                    hdrpRenderer.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
                    hdrpRenderer.AddPassOfType(typeof(SmokeAndFireHDRPRenderComponent.FluidHDRPRender));
                    SmokeAndFireHDRPRenderComponent.FluidHDRPRender renderer =
                        hdrpRenderer.customPasses[0] as SmokeAndFireHDRPRenderComponent.FluidHDRPRender;
                    renderer.name = "ZibraSmokeAndFireRenderer";
                    renderer.smokeAndFire = this;
                }
            }
#endif
#endif // UNITY_PIPELINE_HDRP
        }

        void ForceCloseCommandEncoder(CommandBuffer cmdList)
        {
#if UNITY_EDITOR_OSX || (!UNITY_EDITOR && UNITY_STANDALONE_OSX)
            // Unity bug workaround
            // For whatever reason, Unity sometimes doesn't close command encoder when we request it from native plugin
            // So when we try to start our command encoder with active encoder already present it leads to crash
            // This happens when scene have Terrain (I still have no idea why)
            // So we force change command encoder like that, and this one closes gracefuly
            //if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            //{
            //    cmdList.SetRenderTarget(color0);
            //    cmdList.DrawProcedural(new Matrix4x4(), materialParameters.NoOpMaterial, 0, MeshTopology.Triangles, 3);
            //}
#endif
        }


        void Start()
        {
            Application.targetFrameRate = 512;
            materialParameters = gameObject.GetComponent<ZibraSmokeAndFireMaterialParameters>();
            solverParameters = gameObject.GetComponent<ZibraSmokeAndFireSolverParameters>();
            manipulatorManager = gameObject.GetComponent<ZibraManipulatorManager>();
        }

        protected void OnEnable()
        {
            SetupScriptableRenderComponents();

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            if (!ZibraSmokeAndFireBridge.IsPaidVersion())
            {
                Debug.LogError(
                    "Free version of native plugin used with paid version of C# plugin. If you just replaced your Zibra Smoke & Fire version you need to restart Unity Editor.");
            }
#else
            if (ZibraSmokeAndFireBridge.IsPaidVersion())
            {
                Debug.LogError(
                    "Paid version of native plugin used with free version of C# plugin. If you just replaced your Zibra Smoke & Fire version you need to restart Unity Editor.");
            }
#endif

            AllInstances?.Add(this);

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            Init();
        }

        public void UpdateGridSize()
        {
            CellSize = Math.Max(containerSize.x, Math.Max(containerSize.y, containerSize.z)) / gridResolution;
            GridSize = 8 * Vector3Int.CeilToInt(containerSize / (8.0f * CellSize));
            NumNodes = GridSize[0] * GridSize[1] * GridSize[2];
            GridDownscale = (int)Mathf.Ceil(1.0f / Mathf.Max(materialParameters.ShadowResolution, materialParameters.IlluminationResolution));
            GridSizeLOD = LODGridSize(GridSize, GridDownscale);
        }
        void InitVolumeTexture(ref RenderTexture volume, Vector3Int resolution, string name, GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat)
        {
            if (volume)
                return;

            format = SystemInfo.IsFormatSupported(format, FormatUsage.LoadStore)
                      ? format
                      : GraphicsFormat.R32G32B32A32_SFloat;

            volume = new RenderTexture(resolution.x, resolution.y, 0, format);
            volume.volumeDepth = resolution.z;
            volume.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            volume.enableRandomWrite = true;
            volume.filterMode = FilterMode.Trilinear;
            volume.name = name;
            volume.Create();
            if (!volume.IsCreated())
            {
                volume = null;
                throw new NotSupportedException("Failed to create 3D texture.");
            }
        }

        RenderTexture InitVolumeTexture(Vector3Int resolution, string name, GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat)
        {
            format = SystemInfo.IsFormatSupported(format, FormatUsage.LoadStore)
                      ? format
                      : GraphicsFormat.R32G32B32A32_SFloat;

            var volume = new RenderTexture(resolution.x, resolution.y, 0, format);
            volume.volumeDepth = resolution.z;
            volume.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            volume.enableRandomWrite = true;
            volume.filterMode = FilterMode.Trilinear;
            volume.name = name;
            volume.Create();
            if (!volume.IsCreated())
            {
                volume = null;
                throw new NotSupportedException("Failed to create 3D texture.");
            }

            return volume;
        }


        private Vector3Int LODGridSize(Vector3Int size, int downscale)
        {
            return new Vector3Int(size.x / downscale, size.y / downscale, size.z / downscale);
        }

        private Vector3Int PressureGridSize(Vector3Int size, int downscale)
        {
            return new Vector3Int(size.x / downscale + 1, size.y / downscale + 1, size.z / downscale + 1);
        }

        private int IntDivCeil(int a, int b)
        {
            return (a + b - 1) / b;
        }

        private void InitializeSolver()
        {
            simulationInternalTime = 0.0f;
            simulationInternalFrame = 0;
            simulationParams = new SimulationParams();
            cameraRenderParams = new RenderParams();

            UpdateGridSize();
            SetSimulationParameters();

            NativeSimulationData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SimulationParams)));

            isEnabled = true;

            if (manipulatorManager != null)
            {
                manipulatorManager.UpdateConst(manipulators);
                manipulatorManager.UpdateDynamic(this);

                if (manipulatorManager.TextureCount > 0)
                {
                    EmbeddingsTexture = new Texture3D(
                        manipulatorManager.EmbeddingTextureDimension,
                        manipulatorManager.EmbeddingTextureDimension,
                        manipulatorManager.EmbeddingTextureDimension,
                        TextureFormat.RGBA32, false);

                    SDFGridTexture = new Texture3D(
                        manipulatorManager.SDFTextureDimension,
                        manipulatorManager.SDFTextureDimension,
                        manipulatorManager.SDFTextureDimension,
                        TextureFormat.RHalf, false);

                    EmbeddingsTexture.filterMode = FilterMode.Trilinear;
                    SDFGridTexture.filterMode = FilterMode.Trilinear;
                }
                else
                {
                    EmbeddingsTexture = new Texture3D(1, 1, 1, TextureFormat.RGBA32, 0);
                    SDFGridTexture = new Texture3D(1, 1, 1, TextureFormat.RHalf, 0);
                }

                int ManipSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.ManipulatorParam));
                int SDFSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams));
                // Need to create at least some buffer to bind to shaders
                NativeManipData = Marshal.AllocHGlobal(manipulatorManager.Elements * ManipSize);
                NativeSDFData = Marshal.AllocHGlobal(manipulatorManager.SDFObjectList.Count * SDFSize);
                DynamicManipulatorData = new ComputeBuffer(Math.Max(manipulatorManager.Elements, 1), ManipSize);

                AtomicCounters = new ComputeBuffer(8, sizeof(int));
                EffectParticleData0 = new ComputeBuffer(3 * materialParameters.MaxEffectParticles, sizeof(uint));
                EffectParticleData1 = new ComputeBuffer(3 * materialParameters.MaxEffectParticles, sizeof(uint));

                SDFObjectData = new ComputeBuffer(Math.Max(manipulatorManager.SDFObjectList.Count, 1),
                                                  Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams)));
                ManipulatorStatistics = new ComputeBuffer(
                    Math.Max(STATISTICS_PER_MANIPULATOR * manipulatorManager.Elements, 1), sizeof(int));

#if ZIBRA_SMOKE_AND_FIRE_DEBUG
                DynamicManipulatorData.name = "DynamicManipulatorData";
                SDFObjectData.name = "SDFObjectData";
                ManipulatorStatistics.name = "ManipulatorStatistics";
#endif
                var gcparamBuffer2 = GCHandle.Alloc(manipulatorManager.indices, GCHandleType.Pinned);

                UpdateInteropBuffers();

                var registerManipulatorsBridgeParams = new RegisterManipulatorsBridgeParams();
                registerManipulatorsBridgeParams.ManipulatorNum = manipulatorManager.Elements;
                registerManipulatorsBridgeParams.ManipulatorBufferDynamic = GetNativePtr(DynamicManipulatorData);
                registerManipulatorsBridgeParams.SDFObjectBuffer = GetNativePtr(SDFObjectData);
                registerManipulatorsBridgeParams.ManipulatorBufferStatistics =
                    ManipulatorStatistics.GetNativeBufferPtr();
                registerManipulatorsBridgeParams.ManipulatorParams = NativeManipData;
                registerManipulatorsBridgeParams.SDFObjectCount = manipulatorManager.SDFObjectList.Count;
                registerManipulatorsBridgeParams.SDFObjectData = NativeSDFData;
                registerManipulatorsBridgeParams.ManipIndices = gcparamBuffer2.AddrOfPinnedObject();
                registerManipulatorsBridgeParams.EmbeddingsTexture = MakeTextureNativeBridge(EmbeddingsTexture);
                registerManipulatorsBridgeParams.SDFGridTexture = MakeTextureNativeBridge(SDFGridTexture);

                GCHandle embeddingDataHandle = default(GCHandle);
                if (manipulatorManager.Embeddings.Length > 0)
                {
                    embeddingDataHandle = GCHandle.Alloc(manipulatorManager.Embeddings, GCHandleType.Pinned);
                    registerManipulatorsBridgeParams.EmbeddigsData.dataSize =
                        Marshal.SizeOf(new Color32()) * manipulatorManager.Embeddings.Length;
                    registerManipulatorsBridgeParams.EmbeddigsData.data = embeddingDataHandle.AddrOfPinnedObject();
                    registerManipulatorsBridgeParams.EmbeddigsData.rowPitch =
                        Marshal.SizeOf(new Color32()) * EmbeddingsTexture.width;
                    registerManipulatorsBridgeParams.EmbeddigsData.dimensionX = EmbeddingsTexture.width;
                    registerManipulatorsBridgeParams.EmbeddigsData.dimensionY = EmbeddingsTexture.height;
                    registerManipulatorsBridgeParams.EmbeddigsData.dimensionZ = EmbeddingsTexture.depth;
                }

                GCHandle sdfGridHandle = default(GCHandle);
                if (manipulatorManager.SDFGrid.Length > 0)
                {
                    sdfGridHandle = GCHandle.Alloc(manipulatorManager.SDFGrid, GCHandleType.Pinned);
                    registerManipulatorsBridgeParams.SDFGridData.dataSize =
                        Marshal.SizeOf(new byte()) * manipulatorManager.SDFGrid.Length;
                    registerManipulatorsBridgeParams.SDFGridData.data = sdfGridHandle.AddrOfPinnedObject();
                    registerManipulatorsBridgeParams.SDFGridData.rowPitch =
                        Marshal.SizeOf(new byte()) * 2 * SDFGridTexture.width;
                    registerManipulatorsBridgeParams.SDFGridData.dimensionX = SDFGridTexture.width;
                    registerManipulatorsBridgeParams.SDFGridData.dimensionY = SDFGridTexture.height;
                    registerManipulatorsBridgeParams.SDFGridData.dimensionZ = SDFGridTexture.depth;
                }

                IntPtr nativeRegisterManipulatorsBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(registerManipulatorsBridgeParams));
                Marshal.StructureToPtr(registerManipulatorsBridgeParams, nativeRegisterManipulatorsBridgeParams, true);
                solverCommandBuffer.Clear();
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.RegisterManipulators,
                                                      nativeRegisterManipulatorsBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);

                gcparamBuffer2.Free();
            }
            else
            {
                Debug.LogWarning("No manipulator manipulatorManager has been set");
            }

            SetSimulationParameters();
            UpdateInteropBuffers();

            RenderDensity = InitVolumeTexture(GridSize, nameof(RenderDensity), GraphicsFormat.R16_SFloat);

            if (GridDownscale > 1)
            {
                DownscaleXYZ = new Vector3Int(IntDivCeil((int)GridSizeLOD.x, WORKGROUP_SIZE_X), IntDivCeil((int)GridSizeLOD.y, WORKGROUP_SIZE_Y), IntDivCeil((int)GridSizeLOD.z, WORKGROUP_SIZE_Z));
                RenderDensityLOD = InitVolumeTexture(GridSizeLOD, nameof(RenderDensityLOD), GraphicsFormat.R16_SFloat);
            }

            RenderColor = InitVolumeTexture(GridSize, nameof(RenderColor), GraphicsFormat.R16G16_SFloat);
            RenderIllumination = InitVolumeTexture(GridSize, nameof(RenderIllumination), GraphicsFormat.B10G11R11_UFloatPack32);
            // TODO replace with 32 or 64 bit texture format
            ColorTexture0 = InitVolumeTexture(GridSize, nameof(ColorTexture0), GraphicsFormat.R16G16B16_SFloat);
            VelocityTexture0 = InitVolumeTexture(GridSize, nameof(VelocityTexture0), GraphicsFormat.R16G16B16A16_SFloat);
            ColorTexture1 = InitVolumeTexture(GridSize, nameof(ColorTexture1), GraphicsFormat.R16G16_SFloat);
            VelocityTexture1 = InitVolumeTexture(GridSize, nameof(VelocityTexture1), GraphicsFormat.R16G16B16A16_SFloat);
            //TODO we probably can use single-channel texture here. Need to be further verified
            TmpSDFTexture = InitVolumeTexture(GridSize, nameof(TmpSDFTexture), GraphicsFormat.R16G16_SFloat);

            Divergence = InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Divergence), GraphicsFormat.R16_SFloat);
            ResidualLOD0 = InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(ResidualLOD0), GraphicsFormat.R16_SFloat);
            ResidualLOD1 = InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(ResidualLOD1), GraphicsFormat.R16_SFloat);
            ResidualLOD2 = InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(ResidualLOD2), GraphicsFormat.R16_SFloat);
            Pressure0LOD0 = InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Pressure0LOD0), GraphicsFormat.R16_SFloat);
            Pressure0LOD1 = InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(Pressure0LOD1), GraphicsFormat.R16_SFloat);
            Pressure0LOD2 = InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(Pressure0LOD2), GraphicsFormat.R16_SFloat);
            Pressure1LOD0 = InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Pressure1LOD0), GraphicsFormat.R16_SFloat);
            Pressure1LOD1 = InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(Pressure1LOD1), GraphicsFormat.R16_SFloat);
            Pressure1LOD2 = InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(Pressure1LOD2), GraphicsFormat.R16_SFloat);

            Vector3 ShadowGridSize = new Vector3(GridSize.x, GridSize.y, GridSize.z) * materialParameters.ShadowResolution;
            Shadowmap = InitVolumeTexture(new Vector3Int((int)ShadowGridSize.x, (int)ShadowGridSize.y, (int)ShadowGridSize.z), nameof(Shadowmap), GraphicsFormat.R16_SFloat);
            ShadowWorkGroupsXYZ = new Vector3Int(IntDivCeil((int)ShadowGridSize.x, WORKGROUP_SIZE_X), IntDivCeil((int)ShadowGridSize.y, WORKGROUP_SIZE_Y), IntDivCeil((int)ShadowGridSize.z, WORKGROUP_SIZE_Z));

            Vector3 LightGridSize = new Vector3(GridSize.x, GridSize.y, GridSize.z) * materialParameters.IlluminationResolution;
            Lightmap = InitVolumeTexture(new Vector3Int((int)LightGridSize.x, (int)LightGridSize.y, (int)LightGridSize.z), nameof(Lightmap), GraphicsFormat.R16G16B16A16_SFloat);
            LightWorkGroupsXYZ = new Vector3Int(IntDivCeil((int)LightGridSize.x, WORKGROUP_SIZE_X), IntDivCeil((int)LightGridSize.y, WORKGROUP_SIZE_Y), IntDivCeil((int)LightGridSize.z, WORKGROUP_SIZE_Z));

            WorkGroupsXYZ = new Vector3Int(IntDivCeil((int)GridSize.x, WORKGROUP_SIZE_X), IntDivCeil((int)GridSize.y, WORKGROUP_SIZE_Y), IntDivCeil((int)GridSize.z, WORKGROUP_SIZE_Z));
            MaxEffectParticleWorkgroups = IntDivCeil(materialParameters.MaxEffectParticles, PARTICLE_WORKGROUP);

            Renderer = Resources.Load("Renderer") as ComputeShader;
            ShadowmapID = Renderer.FindKernel("CS_Shadowmap");
            LightmapID = Renderer.FindKernel("CS_Lightmap");
            IlluminationID = Renderer.FindKernel("CS_Illumination");
            CopyDepthID = Renderer.FindKernel("CS_CopyDepth");
            var registerBuffersParams = new RegisterBuffersBridgeParams();
            registerBuffersParams.SimulationParams = NativeSimulationData;

            registerBuffersParams.RenderDensity = MakeTextureNativeBridge(RenderDensity);
            registerBuffersParams.RenderDensityLOD = MakeTextureNativeBridge(RenderDensityLOD);
            registerBuffersParams.RenderColor = MakeTextureNativeBridge(RenderColor);
            registerBuffersParams.RenderIllumination = MakeTextureNativeBridge(RenderIllumination);
            registerBuffersParams.ColorTexture0 = MakeTextureNativeBridge(ColorTexture0);
            registerBuffersParams.VelocityTexture0 = MakeTextureNativeBridge(VelocityTexture0);
            registerBuffersParams.ColorTexture1 = MakeTextureNativeBridge(ColorTexture1);
            registerBuffersParams.VelocityTexture1 = MakeTextureNativeBridge(VelocityTexture1);
            registerBuffersParams.TmpSDFTexture = MakeTextureNativeBridge(TmpSDFTexture);

            registerBuffersParams.Divergence = MakeTextureNativeBridge(Divergence);
            registerBuffersParams.ResidualLOD0 = MakeTextureNativeBridge(ResidualLOD0);
            registerBuffersParams.ResidualLOD1 = MakeTextureNativeBridge(ResidualLOD1);
            registerBuffersParams.ResidualLOD2 = MakeTextureNativeBridge(ResidualLOD2);
            registerBuffersParams.Pressure0LOD0 = MakeTextureNativeBridge(Pressure0LOD0);
            registerBuffersParams.Pressure0LOD1 = MakeTextureNativeBridge(Pressure0LOD1);
            registerBuffersParams.Pressure0LOD2 = MakeTextureNativeBridge(Pressure0LOD2);
            registerBuffersParams.Pressure1LOD0 = MakeTextureNativeBridge(Pressure1LOD0);
            registerBuffersParams.Pressure1LOD1 = MakeTextureNativeBridge(Pressure1LOD1);
            registerBuffersParams.Pressure1LOD2 = MakeTextureNativeBridge(Pressure1LOD2);
            registerBuffersParams.AtomicCounters = GetNativePtr(AtomicCounters);
            registerBuffersParams.EffectParticleData0 = GetNativePtr(EffectParticleData0);
            registerBuffersParams.EffectParticleData1 = GetNativePtr(EffectParticleData1);

            RandomTexture = new Texture3D(RANDOM_TEX_SIZE, RANDOM_TEX_SIZE, RANDOM_TEX_SIZE, TextureFormat.RGBA32, false);
            RandomTexture.filterMode = FilterMode.Trilinear;
            registerBuffersParams.RandomTexture = MakeTextureNativeBridge(RandomTexture);

            GCHandle randomDataHandle = default(GCHandle);
            System.Random rand = new System.Random();
            int RandomTextureSize = RANDOM_TEX_SIZE * RANDOM_TEX_SIZE * RANDOM_TEX_SIZE;
            Color32[] RandomTextureData = new Color32[RandomTextureSize];
            for (int i = 0; i < RandomTextureSize; i++)
            {
                RandomTextureData[i] = new Color32((byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255));
            }

            randomDataHandle = GCHandle.Alloc(RandomTextureData, GCHandleType.Pinned);
            registerBuffersParams.RandomData.dataSize =
                Marshal.SizeOf(new Color32()) * RandomTextureData.Length;
            registerBuffersParams.RandomData.data = randomDataHandle.AddrOfPinnedObject();
            registerBuffersParams.RandomData.rowPitch =
                Marshal.SizeOf(new Color32()) * RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionX = RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionY = RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionZ = RANDOM_TEX_SIZE;

            IntPtr nativeRegisterBuffersParams =
                Marshal.AllocHGlobal(Marshal.SizeOf(registerBuffersParams));
            Marshal.StructureToPtr(registerBuffersParams, nativeRegisterBuffersParams, true);
            solverCommandBuffer.Clear();
            ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                  ZibraSmokeAndFireBridge.EventID.RegisterSolverBuffers,
                                                  nativeRegisterBuffersParams);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            solverCommandBuffer.Clear();
            toFreeOnExit.Add(nativeRegisterBuffersParams);
        }

        /// <summary>
        /// Initializes a new instance of ZibraFluid
        /// </summary>
        public void Init()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            try
            {
#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    bool missingRequiredParameter = false;

                    if (mainLight == null)
                    {
                        Debug.LogError("No Custom Light set in Zibra Smoke & Fire.");
                        missingRequiredParameter = true;
                    }

                    if (missingRequiredParameter)
                    {
                        throw new Exception("Smoke & Fire creation failed due to missing parameter.");
                    }
                }
#endif


                bool haveEmitter = false;
                foreach (var manipulator in manipulators)
                {
                    if (manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter)
                    {
                        haveEmitter = true;
                        break;
                    }
                }

                if (!haveEmitter)
                {
                    throw new Exception("Smoke & Fire creation failed. Simulation has neither initial state nor emitters.");
                }

                Camera.onPreRender += RenderCallBackWrapper;

                solverCommandBuffer = new CommandBuffer { name = "ZibraSmokeAndFire.Solver" };

                CurrentInstanceID = ms_NextInstanceId++;

                ForceCloseCommandEncoder(solverCommandBuffer);
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.CreateFluidInstance);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
                solverCommandBuffer.Clear();

                InitializeSolver();

                var initializeGPUReadbackParamsBridgeParams = new InitializeGPUReadbackParams();
                UInt32 manipSize = (UInt32)manipulatorManager.Elements * STATISTICS_PER_MANIPULATOR * sizeof(Int32);

                initializeGPUReadbackParamsBridgeParams.readbackBufferSize = manipSize;
                switch (SystemInfo.graphicsDeviceType)
                {
                    case GraphicsDeviceType.Direct3D11:
                    case GraphicsDeviceType.XboxOne:
                    case GraphicsDeviceType.Switch:
#if UNITY_2020_3_OR_NEWER
                    case GraphicsDeviceType.Direct3D12:
                    case GraphicsDeviceType.XboxOneD3D12:
#endif
                        initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = QualitySettings.maxQueuedFrames + 1;
                        break;
                    default:
                        initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = (int)this.maxFramesInFlight;
                        break;
                }

                IntPtr nativeCreateInstanceBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(initializeGPUReadbackParamsBridgeParams));
                Marshal.StructureToPtr(initializeGPUReadbackParamsBridgeParams, nativeCreateInstanceBridgeParams, true);

                solverCommandBuffer.Clear();
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.InitializeGpuReadback,
                                                      nativeCreateInstanceBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
                solverCommandBuffer.Clear();
                toFreeOnExit.Add(nativeCreateInstanceBridgeParams);

                initialized = true;
                // hack to make editor -> play mode transition work when the simulation is initialized
                forceTextureUpdate = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ClearRendering();
                ClearSolver();

                initialized = false;
            }
        }

        protected void Update()
        {
            if (!initialized)
            {
                return;
            }

            ZibraSmokeAndFireGPUGarbageCollector.GCUpdateWrapper();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (limitFramerate)
            {
                //TODO separate simulation into frames equally to avoid frame jitter
                if (maximumFramerate > 0.0f)
                {
                    timeAccumulation += Time.deltaTime;

                    if (timeAccumulation > 1.0f / maximumFramerate)
                    {
                        UpdateSimulation();
                        timeAccumulation = 0;
                    }
                }
            }
            else
            {
                UpdateSimulation();
            }

            UpdateReadback();
            RefreshEmitterColorsTexture();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                UpdateDebugTimestamps();
            }
        }

        public bool IsSimulationEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            // So we need to always simulate first 2 frames
            return initialized && (runSimulation || (simulationInternalFrame <= 2));
        }

        public void UpdateDebugTimestamps()
        {
            if (!IsSimulationEnabled())
            {
                return;
            }
            DebugTimestampsItemsCount = ZibraSmokeAndFireBridge.GetDebugTimestamps(CurrentInstanceID, DebugTimestampsItems);
        }

        public void UpdateReadback()
        {
            solverCommandBuffer.Clear();

            // This must be called at most ONCE PER FRAME
            // Otherwise you'll get deadlock
            ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                  ZibraSmokeAndFireBridge.EventID.UpdateReadback);

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            UpdateManipulatorStatistics();
#endif
        }

        private Vector3 GetLightColor(Light light)
        {
            Vector3 lightColor = new Vector3(light.color.r, light.color.g, light.color.b);
#if UNITY_PIPELINE_HDRP
            var lightData = light.GetComponent<HDAdditionalLightData>();
            if (lightData != null)
            {
                float intensityHDRP = lightData.intensity;
                return 0.03f * lightColor * intensityHDRP;
            }
#endif
            float intensity = light.intensity;
            return lightColor * intensity;
        }

        private int GetLights(ref Vector4[] lightColors, ref Vector4[] lightPositions, float brightness = 1.0f)
        {
            int lightCount = 0;
            for (int i = 0; i < lights.Count; i++)
            {
                if (!lights[i].enabled) continue;
                Vector3 color = GetLightColor(lights[i]);
                Vector3 pos = lights[i].transform.position;
                lightColors[lightCount] = brightness * new Vector4(color.x, color.y, color.z, 0.0f);
                lightPositions[lightCount] = new Vector4(pos.x, pos.y, pos.z, 1.0f / Mathf.Max(lights[i].range * lights[i].range, 0.00001f));
                lightCount++;
                if (lightCount == MAX_LIGHT_COUNT)
                {
                    Debug.Log("Zibra Flames instance: Max light count reached.");
                    break;
                }
            }
            return lightCount;
        }

        private void Illumination()
        {
            //TODO Add native Unity volumetric render mode

            solverCommandBuffer.Clear();

            solverCommandBuffer.SetComputeVectorParam(Renderer, "ContainerScale", containerSize);
            solverCommandBuffer.SetComputeVectorParam(Renderer, "ContainerPosition", simulationContainerPosition);
            solverCommandBuffer.SetComputeVectorParam(Renderer, "GridSize", (Vector3)GridSize);
            solverCommandBuffer.SetComputeVectorParam(Renderer, "ShadowColor", materialParameters.ShadowColor);
            solverCommandBuffer.SetComputeVectorParam(Renderer, "ScatteringColor", materialParameters.ScatteringColor);
            solverCommandBuffer.SetComputeVectorParam(Renderer, "LightColor", GetLightColor(mainLight));
            solverCommandBuffer.SetComputeVectorParam(Renderer, "LightDirWorld", mainLight.transform.rotation * new Vector3(0, 0, -1));

            int mainLightMode = mainLight.enabled ? 1 : 0;
            Vector4[] lightColors = new Vector4[MAX_LIGHT_COUNT];
            Vector4[] lightPositions = new Vector4[MAX_LIGHT_COUNT];
            int lightCount = GetLights(ref lightColors, ref lightPositions, materialParameters.IlluminationBrightness);

            solverCommandBuffer.SetComputeVectorArrayParam(Renderer, "LightColorArray", lightColors);
            solverCommandBuffer.SetComputeVectorArrayParam(Renderer, "LightPositionArray", lightPositions);
            solverCommandBuffer.SetComputeIntParam(Renderer, "LightCount", lightCount);
            solverCommandBuffer.SetComputeIntParam(Renderer, "MainLightMode", mainLightMode);
            solverCommandBuffer.SetComputeIntParam(Renderer, "SimulationMode", (int)CurrentSimulationMode);

            solverCommandBuffer.SetComputeFloatParam(Renderer, "IlluminationSoftness", materialParameters.IlluminationSoftness);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "SmokeDensity", materialParameters.SmokeDensity);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "FuelDensity", materialParameters.FuelDensity);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "ShadowIntensity", materialParameters.ShadowIntensity);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "FireBrightness", materialParameters.FireBrightness);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "BlackBodyBrightness", materialParameters.BlackBodyBrightness);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "ReactionSpeed", solverParameters.ReactionSpeed);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "TempThreshold", solverParameters.TempThreshold);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "TemperatureDensityDependence", materialParameters.TemperatureDensityDependence);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "ScatteringAttenuation", materialParameters.ScatteringAttenuation);
            solverCommandBuffer.SetComputeFloatParam(Renderer, "ScatteringContribution", materialParameters.ScatteringContribution);
            solverCommandBuffer.SetComputeVectorParam(Renderer, "FireColor", materialParameters.FireColor);

            if (mainLight.enabled)
            {
                solverCommandBuffer.SetComputeFloatParam(Renderer, "ShadowStepSize", materialParameters.ShadowStepSize);
                solverCommandBuffer.SetComputeIntParam(Renderer, "ShadowMaxSteps", materialParameters.ShadowMaxSteps);

                if (GridDownscale > 1)
                {
                    solverCommandBuffer.SetComputeTextureParam(Renderer, ShadowmapID, "Density", RenderDensityLOD);
                }
                else
                {
                    solverCommandBuffer.SetComputeTextureParam(Renderer, ShadowmapID, "Density", RenderDensity);
                }

                solverCommandBuffer.SetComputeIntParam(Renderer, "DensityDownscale", GridDownscale);
                solverCommandBuffer.SetComputeTextureParam(Renderer, ShadowmapID, "Color", RenderColor);
                solverCommandBuffer.SetComputeTextureParam(Renderer, ShadowmapID, "BlueNoise", Resources.Load("Textures/bluenoise") as Texture);
                solverCommandBuffer.SetComputeTextureParam(Renderer, ShadowmapID, "ShadowmapOUT", Shadowmap);
                solverCommandBuffer.DispatchCompute(Renderer, ShadowmapID, ShadowWorkGroupsXYZ.x, ShadowWorkGroupsXYZ.y, ShadowWorkGroupsXYZ.z);
            }

            if (lights.Count > 0)
            {
                solverCommandBuffer.SetComputeFloatParam(Renderer, "ShadowStepSize", materialParameters.IlluminationStepSize);
                solverCommandBuffer.SetComputeIntParam(Renderer, "ShadowMaxSteps", materialParameters.IlluminationMaxSteps);

                if (GridDownscale > 1)
                {
                    solverCommandBuffer.SetComputeTextureParam(Renderer, LightmapID, "Density", RenderDensityLOD);
                }
                else
                {
                    solverCommandBuffer.SetComputeTextureParam(Renderer, LightmapID, "Density", RenderDensity);
                }

                solverCommandBuffer.SetComputeIntParam(Renderer, "DensityDownscale", GridDownscale);
                solverCommandBuffer.SetComputeTextureParam(Renderer, LightmapID, "Color", RenderColor);
                solverCommandBuffer.SetComputeTextureParam(Renderer, LightmapID, "BlueNoise", Resources.Load("Textures/bluenoise") as Texture);
                solverCommandBuffer.SetComputeTextureParam(Renderer, LightmapID, "LightmapOUT", Lightmap);

                solverCommandBuffer.DispatchCompute(Renderer, LightmapID, LightWorkGroupsXYZ.x, LightWorkGroupsXYZ.y, LightWorkGroupsXYZ.z);
            }

            solverCommandBuffer.SetComputeTextureParam(Renderer, IlluminationID, "Density", RenderDensity);
            solverCommandBuffer.SetComputeIntParam(Renderer, "DensityDownscale", 1);
            solverCommandBuffer.SetComputeTextureParam(Renderer, IlluminationID, "Shadowmap", Shadowmap);
            solverCommandBuffer.SetComputeTextureParam(Renderer, IlluminationID, "Lightmap", Lightmap);
            solverCommandBuffer.SetComputeTextureParam(Renderer, IlluminationID, "Color", RenderColor);
            solverCommandBuffer.SetComputeTextureParam(Renderer, IlluminationID, "IlluminationOUT", RenderIllumination);
            solverCommandBuffer.DispatchCompute(Renderer, IlluminationID, WorkGroupsXYZ.x, WorkGroupsXYZ.y, WorkGroupsXYZ.z);

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            solverCommandBuffer.Clear();
        }

        public void UpdateSimulation()
        {
            if (!initialized)
                return;

            timestep = timeStep;

            if (runSimulation)
                StepPhysics();

            Illumination();

#if UNITY_EDITOR
            NotifyChange();
#endif
        }


        void UpdateInteropBuffers()
        {
            Marshal.StructureToPtr(simulationParams, NativeSimulationData, true);

            if (manipulatorManager.Elements > 0)
            {
                SetInteropBuffer(NativeManipData, manipulatorManager.ManipulatorParams);
            }

            if (manipulatorManager.SDFObjectList.Count > 0)
            {
                SetInteropBuffer(NativeSDFData, manipulatorManager.SDFObjectList);
            }
        }

        void UpdateSolverParameters()
        {
            // Update solver parameters
            ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                  ZibraSmokeAndFireBridge.EventID.UpdateSolverParameters, NativeSimulationData);

            if (manipulatorManager.Elements > 0)
            {
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.UpdateManipulatorParameters,
                                                      NativeManipData);
            }

            if (manipulatorManager.SDFObjectList.Count > 0)
            {
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.UpdateSDFObjects, NativeSDFData);
            }
        }

        private void StepPhysics()
        {
            solverCommandBuffer.Clear();

            ForceCloseCommandEncoder(solverCommandBuffer);

            SetSimulationParameters();

            manipulatorManager.UpdateDynamic(this, timestep);

            UpdateInteropBuffers();
            UpdateSolverParameters();

            // execute simulation
            ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                  ZibraSmokeAndFireBridge.EventID.StepPhysics);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            //the actual position of the container
            Vector3 prevPosition = simulationContainerPosition;
            simulationContainerPosition = ZibraSmokeAndFireBridge.GetSimulationContainerPosition(CurrentInstanceID);
            isSimulationContainerPositionChanged = prevPosition != simulationContainerPosition;

            // update internal time
            simulationInternalTime += timestep;
            simulationInternalFrame++;
        }

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
        void UpdateManipulatorStatistics()
        {
            /// ManipulatorStatistics GPUReadback
            if (manipulatorManager.Elements > 0)
            {
                UInt32 size = (UInt32)manipulatorManager.Elements * STATISTICS_PER_MANIPULATOR;
                IntPtr readbackData = ZibraSmokeAndFireBridge.GPUReadbackGetData(CurrentInstanceID, size * sizeof(Int32));
                if (readbackData != IntPtr.Zero)
                {
                    Int32[] Stats = new Int32[size];
                    Marshal.Copy(readbackData, Stats, 0, (Int32)size);
                    manipulatorManager.UpdateStatistics(Stats, manipulators, solverParameters, materialParameters);
                }
            }
        }
#endif

        // stability calibration curve fit
        private float DivergenceDecayCurve(float x)
        {
            float a = (0.177f - 0.85f * x + 9.0f * x * x) / 1.8f;
            return 1.8f * a / (a + 1);
        }

        private void SetSimulationParameters()
        {
            solverParameters.ValidateParameters();

            containerPos = transform.position;

            simulationParams.GridSize = GridSize;
            simulationParams.NodeCount = NumNodes;

            simulationParams.ContainerScale = containerSize;
            simulationParams.MinimumVelocity = solverParameters.MinimumVelocity;

            simulationParams.ContainerPos = containerPos;
            simulationParams.MaximumVelocity = solverParameters.MaximumVelocity;

            simulationParams.TimeStep = timestep;
            simulationParams.SimulationTime = simulationInternalTime;
            simulationParams.SimulationFrame = simulationInternalFrame;
            simulationParams.Sharpen = solverParameters.Sharpen;
            simulationParams.SharpenThreshold = solverParameters.SharpenThreshold;

            simulationParams.JacobiIterations = solverParameters.PressureSolveIterations;
            simulationParams.ColorDecay = solverParameters.ColorDecay;
            simulationParams.VelocityDecay = solverParameters.VelocityDecay;
            simulationParams.PressureReuse = solverParameters.PressureReuse;
            simulationParams.PressureReuseClamp = solverParameters.PressureReuseClamp;
            simulationParams.PressureProjection = solverParameters.PressureProjection;
            simulationParams.PressureClamp = solverParameters.PressureClamp;

            simulationParams.Gravity = solverParameters.Gravity;
            simulationParams.SmokeBuoyancy = solverParameters.SmokeBuoyancy;

            simulationParams.LOD0Iterations = solverParameters.LOD0Iterations;
            simulationParams.LOD1Iterations = solverParameters.LOD1Iterations;
            simulationParams.LOD2Iterations = solverParameters.LOD2Iterations;
            simulationParams.PreIterations = solverParameters.PreIterations;

            simulationParams.MainOverrelax = solverParameters.MainOverrelax;
            simulationParams.EdgeOverrelax = solverParameters.EdgeOverrelax;
            simulationParams.VolumeEdgeFadeoff = materialParameters.VolumeEdgeFadeoff;
            simulationParams.SimulationIterations = SimulationIterations;

            simulationParams.SimulationMode = (int)CurrentSimulationMode;
            simulationParams.FixVolumeWorldPosition = fixVolumeWorldPosition ? 1 : 0;

            simulationParams.FuelDensity = materialParameters.FuelDensity;
            simulationParams.SmokeDensity = materialParameters.SmokeDensity;
            simulationParams.TemperatureDensityDependence = materialParameters.TemperatureDensityDependence;
            simulationParams.FireBrightness = materialParameters.FireBrightness + materialParameters.BlackBodyBrightness;

            simulationParams.TempThreshold = solverParameters.TempThreshold;
            simulationParams.HeatEmission = solverParameters.HeatEmission;
            simulationParams.ReactionSpeed = solverParameters.ReactionSpeed;
            simulationParams.HeatBuoyancy = solverParameters.HeatBuoyancy;

            simulationParams.MaxEffectParticleCount = materialParameters.MaxEffectParticles;
            simulationParams.ParticleLifetime = materialParameters.ParticleLifetime;

            simulationParams.GridSizeLOD = GridSizeLOD;
            simulationParams.GridDownscale = GridDownscale;
        }

        public bool HasEmitter()
        {
            foreach (var manipulator in manipulators)
            {
                if (manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter)
                {
                    return true;
                }
            }

            return false;
        }

        public ReadOnlyCollection<Manipulator> GetManipulatorList()
        {
            return manipulators.AsReadOnly();
        }

        public bool HasManipulator(Manipulator manipulator)
        {
            return manipulators.Contains(manipulator);
        }

        public void AddManipulator(Manipulator manipulator)
        {
            if (initialized)
            {
                Debug.LogWarning("We don't yet support changing number of manipulators/colliders at runtime.");
                return;
            }

            if (!manipulators.Contains(manipulator))
            {
                manipulators.RemoveAll(item => item == null);
                manipulators.Add(manipulator);
                manipulators.Sort(new ManipulatorCompare());
            }
        }

        private void RefreshEmitterColorsTexture()
        {
            var emitters = manipulators.FindAll(manip => manip is ZibraParticleEmitter);

            var textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var textureFlags = TextureCreationFlags.None;

            emitters.Sort(new ManipulatorCompare());
            if (EmittersColorsTexture == null || emitters.Count != EmittersColorsTexture.height)
            {
                EmittersColorsTexture = new Texture2D(EMITTER_GRADIENT_TEX_WIDTH, Mathf.Max(emitters.Count, 1), textureFormat, textureFlags);
            }
            else if (emitters.Count == 0)
            {
                EmittersColorsTexture = new Texture2D(1, 1, textureFormat, textureFlags);
            }

            if (EmittersSpriteTexture == null)
            {
                int[] dimensions = new int[] { 1, 1, Mathf.Max(1, emitters.Count) };
                if (emitters.Find(emitter => (emitter as ZibraParticleEmitter).RenderMode == ZibraParticleEmitter.RenderingMode.Sprite))
                {
                    dimensions[0] = dimensions[1] = EMITTER_SPRITE_TEX_SIZE;
                }
                EmittersSpriteTexture = new Texture3D(dimensions[0], dimensions[1], dimensions[2], textureFormat, textureFlags);
            }

            if (emitters.Find(emitter => (emitter as ZibraParticleEmitter).IsDirty))
            {
                float inv = 1f / (EMITTER_GRADIENT_TEX_WIDTH - 1);
                for (int y = 0; y < emitters.Count; y++)
                {
                    var curEmitter = emitters[y] as ZibraParticleEmitter;
                    for (int x = 0; x < EMITTER_GRADIENT_TEX_WIDTH; x++)
                    {
                        var t = x * inv;
                        Color col = curEmitter.ParticleColor.Evaluate(t);
                        col.a = curEmitter.SizeCurve.Evaluate(t) * EMITTER_PARTICLE_SIZE_SCALE;
                        EmittersColorsTexture.SetPixel(x, y, col);
                    }

                    if (curEmitter.RenderMode == ZibraParticleEmitter.RenderingMode.Sprite)
                    {
                        RenderTexture rt = new RenderTexture(EMITTER_SPRITE_TEX_SIZE, EMITTER_SPRITE_TEX_SIZE, 0, textureFormat);
                        Graphics.Blit(curEmitter.ParticleSprite, rt);
                        int slice = y;
                        Graphics.CopyTexture(rt, 0, EmittersSpriteTexture, slice);
                    }
                }
                EmittersColorsTexture.Apply();
                emitters.ForEach(emitter => (emitter as ZibraParticleEmitter).IsDirty = false);
            }
        }

        public void RemoveManipulator(Manipulator manipulator)
        {
            if (initialized)
            {
                Debug.LogWarning("We don't yet support changing number of manipulators/colliders at runtime.");
                return;
            }

            if (manipulators.Contains(manipulator))
            {
                manipulators.RemoveAll(item => item == null);
                manipulators.Remove(manipulator);
                manipulators.Sort(new ManipulatorCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            containerSize[0] = Math.Max(containerSize[0], 1e-3f);
            containerSize[1] = Math.Max(containerSize[1], 1e-3f);
            containerSize[2] = Math.Max(containerSize[2], 1e-3f);

            CellSize = Math.Max(containerSize.x, Math.Max(containerSize.y, containerSize.z)) / gridResolution;

            if (GetComponent<ZibraSmokeAndFireMaterialParameters>() == null)
            {
                gameObject.AddComponent<ZibraSmokeAndFireMaterialParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraSmokeAndFireSolverParameters>() == null)
            {
                gameObject.AddComponent<ZibraSmokeAndFireSolverParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraManipulatorManager>() == null)
            {
                gameObject.AddComponent<ZibraManipulatorManager>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (manipulators != null)
            {
                int removed = manipulators.RemoveAll(item => item == null);
                if (removed > 0)
                {
                    manipulators.Sort(new ManipulatorCompare());
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }

#if !ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            // Limit manipulator count to 1
            if (manipulators.Count > 1)
                manipulators.RemoveRange(1, manipulators.Count - 1);
#endif
        }
#endif

        protected void OnApplicationQuit()
        {
            // On quit we need to destroy simulation before destroying any colliders/manipulators
            OnDisable();
        }

        public void StopSolver()
        {
            if (!initialized)
            {
                return;
            }

            initialized = false;
            ClearRendering();
            ClearSolver();
            isEnabled = false;

            // If ZibraSmokeAndFire object gets disabled/destroyed
            // We still may need to do cleanup few frames later
            // So we create new gameobject which allows us to run cleanup code
            ZibraSmokeAndFireGPUGarbageCollector.CreateGarbageCollector();
        }

        // dispose the objects
        protected void OnDisable()
        {
            StopSolver();
        }
        #endregion

        #region RENDER FUNCTIONS
        /// <summary>
        /// Update render parameters for a given camera
        /// </summary>
        /// <param name="cam">Camera</param>
        public void InitializeNativeCameraParams(Camera cam)
        {
            if (!camNativeParams.ContainsKey(cam))
            {
                // allocate memory for camera parameters
                camNativeParams[cam] = Marshal.AllocHGlobal(Marshal.SizeOf(cameraRenderParams));
            }
        }

        /// <summary>
        /// Update the material parameters
        /// </summary>
        public bool SetMaterialParams(Camera cam)
        {
            bool isDirty = false;

            CameraResources camRes = cameraResources[cam];
            Material usedUpscaleMaterial = EnableDownscale ? materialParameters.UpscaleMaterial : null;

            isDirty = camRes.upscaleMaterial.SetMaterial(usedUpscaleMaterial) || isDirty;

            Material CurrentSharedMaterial = materialParameters.SmokeMaterial;

            isDirty = camRes.smokeAndFireMaterial.SetMaterial(CurrentSharedMaterial) || isDirty;

            Material CurrentMaterial = camRes.smokeAndFireMaterial.currentMaterial;

            CurrentMaterial.SetFloat("SmokeDensity", materialParameters.SmokeDensity);
            CurrentMaterial.SetFloat("FuelDensity", materialParameters.FuelDensity);

            CurrentMaterial.SetVector("ShadowColor", materialParameters.ShadowColor);
            CurrentMaterial.SetVector("AbsorptionColor", materialParameters.AbsorptionColor);
            CurrentMaterial.SetVector("ScatteringColor", materialParameters.ScatteringColor);
            CurrentMaterial.SetFloat("ScatteringAttenuation", materialParameters.ScatteringAttenuation);
            CurrentMaterial.SetFloat("ScatteringContribution", materialParameters.ScatteringContribution);
            CurrentMaterial.SetFloat("FakeShadows", materialParameters.ObjectShadowIntensity);
            CurrentMaterial.SetFloat("ShadowDistanceDecay", materialParameters.ShadowDistanceDecay);
            CurrentMaterial.SetFloat("ShadowIntensity", materialParameters.ShadowIntensity);
            CurrentMaterial.SetFloat("StepSize", materialParameters.RayMarchingStepSize);

            CurrentMaterial.SetInt("PrimaryShadows", (materialParameters.ObjectPrimaryShadows && mainLight.enabled) ? 1 : 0);
            CurrentMaterial.SetInt("IlluminationShadows", materialParameters.ObjectIlluminationShadows ? 1 : 0);

            CurrentMaterial.SetVector("ContainerScale", containerSize);
            CurrentMaterial.SetVector("ContainerPosition", simulationContainerPosition);
            CurrentMaterial.SetVector("GridSize", (Vector3)GridSize);

            if (EnableDownscale)
            {
                CurrentMaterial.EnableKeyword("DOWNSCALE");
            }
            else
            {
                CurrentMaterial.DisableKeyword("DOWNSCALE");
            }

            if (mainLight == null)
                Debug.LogError("No main light source set in the Zibra Flames instance.");
            else
            {
                CurrentMaterial.SetVector("LightColor", GetLightColor(mainLight));
                CurrentMaterial.SetVector("LightDirWorld", mainLight.transform.rotation * new Vector3(0, 0, -1));
            }


            CurrentMaterial.SetTexture("BlueNoise", Resources.Load("Textures/bluenoise") as Texture);
            CurrentMaterial.SetTexture("Color", RenderColor);
            CurrentMaterial.SetTexture("Illumination", RenderIllumination);
            CurrentMaterial.SetTexture("Density", RenderDensity);
            CurrentMaterial.SetInt("DensityDownscale", 1);

            CurrentMaterial.SetTexture("Shadowmap", Shadowmap);
            CurrentMaterial.SetTexture("Lightmap", Lightmap);

            int mainLightMode = mainLight.enabled ? 1 : 0;
            Vector4[] lightColors = new Vector4[MAX_LIGHT_COUNT];
            Vector4[] lightPositions = new Vector4[MAX_LIGHT_COUNT];
            int lightCount = GetLights(ref lightColors, ref lightPositions);

            CurrentMaterial.SetVectorArray("LightColorArray", lightColors);
            CurrentMaterial.SetVectorArray("LightPositionArray", lightPositions);
            CurrentMaterial.SetInt("LightCount", lightCount);
            CurrentMaterial.SetInt("MainLightMode", mainLightMode);

#if UNITY_IOS && !UNITY_EDITOR
            if (EnableDownscale && IsBackgroundCopyNeeded(cam))
            {
                CurrentMaterial.EnableKeyword("FLIP_BACKGROUND");
            }
            else
            {
                CurrentMaterial.DisableKeyword("FLIP_BACKGROUND");
            }
#endif
            CurrentMaterial.SetTexture("Background", GetBackgroundToBind(cam));

            return isDirty;
        }

        public Vector2Int ApplyDownscaleFactor(Vector2Int val)
        {
            if (!EnableDownscale)
                return val;
            return new Vector2Int((int)(val.x * DownscaleFactor), (int)(val.y * DownscaleFactor));
        }

        public Vector2Int ApplyRenderPipelineRenderScale(Vector2Int val, float renderPipelineRenderScale)
        {
            return new Vector2Int((int)(val.x * renderPipelineRenderScale), (int)(val.y * renderPipelineRenderScale));
        }

        private RenderTexture CreateTexture(RenderTexture texture, Vector2Int resolution, bool applyDownscaleFactor,
                                   FilterMode filterMode, int depth, RenderTextureFormat format,
                                   bool enableRandomWrite, ref bool hasBeenUpdated)
        {
            if (texture == null || texture.width != resolution.x || texture.height != resolution.y ||
                forceTextureUpdate)
            {
                ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(texture);

                var newTexture = new RenderTexture(resolution.x, resolution.y, depth, format);
                newTexture.enableRandomWrite = enableRandomWrite;
                newTexture.filterMode = filterMode;
                newTexture.Create();
                hasBeenUpdated = true;
                return newTexture;
            }

            return texture;
        }

        // Returns resolution that is enough for all cameras
        private Vector2Int GetRequiredTextureResolution()
        {
            if (camRenderResolutions.Count == 0)
                Debug.Log("camRenderResolutions dictionary was empty when GetRequiredTextureResolution was called.");

            Vector2Int result = new Vector2Int(0, 0);
            foreach (var item in camRenderResolutions)
            {
                result = Vector2Int.Max(result, item.Value);
            }

            return result;
        }

        public bool IsBackgroundCopyNeeded(Camera cam)
        {
            return !EnableDownscale || (cam.activeTexture == null);
        }

        private RenderTexture GetBackgroundToBind(Camera cam)
        {
            if (!IsBackgroundCopyNeeded(cam))
                return cam.activeTexture;
            return cameraResources[cam].background;
        }

        /// <summary>
        /// Removes disabled/inactive cameras from cameraResources
        /// </summary>
        private void UpdateCameraList()
        {
            List<Camera> toRemove = new List<Camera>();
            foreach (var camResource in cameraResources)
            {
                if (camResource.Key == null ||
                    (!camResource.Key.isActiveAndEnabled && camResource.Key.cameraType != CameraType.SceneView))
                {
                    toRemove.Add(camResource.Key);
                    continue;
                }
            }

            foreach (var cam in toRemove)
            {
                if (cameraResources[cam].background)
                {
                    cameraResources[cam].background.Release();
                    cameraResources[cam].background = null;
                }

                cameraResources.Remove(cam);
            }
        }

        public bool IsRenderingEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            return initialized && runRendering && (simulationInternalFrame > 1);
        }
        void UpdateCameraResolution(Camera cam, float renderPipelineRenderScale)
        {
            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);
            camNativeResolutions[cam] = cameraResolution;
            Vector2Int cameraResolutionDownscaled = ApplyDownscaleFactor(cameraResolution);
            camRenderResolutions[cam] = cameraResolutionDownscaled;
        }

        /// <summary>
        /// Update Native textures for a given camera
        /// </summary>
        /// <param name="cam">Camera</param>
        public bool UpdateNativeTextures(Camera cam, float renderPipelineRenderScale)
        {
            RefreshEmitterColorsTexture();
            UpdateCameraList();

            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);

            Vector2Int textureResolution = GetRequiredTextureResolution();
            int pixelCount = textureResolution.x * textureResolution.y;

            if (!cameras.Contains(cam))
            {
                // add camera to list
                cameras.Add(cam);
            }

            int CameraID = cameras.IndexOf(cam);

            bool isGlobalTexturesDirty = false;
            bool isCameraDirty = cameraResources[cam].isDirty;

            FilterMode defaultFilter = EnableDownscale ? FilterMode.Bilinear : FilterMode.Point;

            if (IsBackgroundCopyNeeded(cam))
            {
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
#if UNITY_PIPELINE_HDRP
                    bool cameraBackgrounHasBeenUpdated = false;
                    cameraResources[cam].background = CreateTexture(cameraResources[cam].background, cameraResolution, false,
                                                  FilterMode.Point, 0, RenderTextureFormat.ARGBHalf, false, ref cameraBackgrounHasBeenUpdated);
                    isCameraDirty = cameraBackgrounHasBeenUpdated || isCameraDirty;
#endif
                }
                else
                {
                    var format = SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.LoadStore)
                                  ? RenderTextureFormat.RGB111110Float
                                  : RenderTextureFormat.ARGB32; // 8 bits per component

                    bool cameraBackgrounHasBeenUpdated = false;
                    cameraResources[cam].background = CreateTexture(cameraResources[cam].background, cameraResolution, false,
                                                  FilterMode.Point, 0, format, false, ref cameraBackgrounHasBeenUpdated);
                    isCameraDirty = cameraBackgrounHasBeenUpdated || isCameraDirty;
                }
            }
            else
            {
                if (cameraResources[cam].background != null)
                {
                    isCameraDirty = true;
                    cameraResources[cam].background.Release();
                    cameraResources[cam].background = null;
                }
            }

            bool updationFlag = false;
            UpscaleColor = CreateTexture(UpscaleColor, textureResolution, true, FilterMode.Bilinear, 0, RenderTextureFormat.ARGBHalf, true, ref updationFlag);
            ParticlesRT = CreateTexture(ParticlesRT, textureResolution, true, FilterMode.Point, 0, RenderTextureFormat.ARGB32, true, ref updationFlag);
            DepthTexture = CreateTexture(DepthTexture, textureResolution, true, defaultFilter, 32, RenderTextureFormat.RFloat, true, ref updationFlag);
            isGlobalTexturesDirty = updationFlag || isGlobalTexturesDirty;

            if (isGlobalTexturesDirty || isCameraDirty || forceTextureUpdate)
            {
                if (isGlobalTexturesDirty || forceTextureUpdate)
                {
                    foreach (var camera in cameraResources)
                    {
                        camera.Value.isDirty = true;
                    }

                    CurrentTextureResolution = textureResolution;
                }

                cameraResources[cam].isDirty = false;

                var registerRenderResourcesBridgeParams = new RegisterRenderResourcesBridgeParams();
                registerRenderResourcesBridgeParams.ParticleColors = MakeTextureNativeBridge(EmittersColorsTexture);
                registerRenderResourcesBridgeParams.ParticleSprites = MakeTextureNativeBridge(EmittersSpriteTexture);
                registerRenderResourcesBridgeParams.Depth = MakeTextureNativeBridge(DepthTexture);
                registerRenderResourcesBridgeParams.ParticlesRT = MakeTextureNativeBridge(ParticlesRT);

                IntPtr nativeRegisterRenderResourcesBridgeParams = Marshal.AllocHGlobal(Marshal.SizeOf(registerRenderResourcesBridgeParams));
                Marshal.StructureToPtr(registerRenderResourcesBridgeParams, nativeRegisterRenderResourcesBridgeParams, true);
                solverCommandBuffer.Clear();
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.RegisterRenderResources,
                                                      nativeRegisterRenderResourcesBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);

                toFreeOnExit.Add(nativeRegisterRenderResourcesBridgeParams);
                forceTextureUpdate = false;
            }

            return isGlobalTexturesDirty || isCameraDirty;
        }

        public void RenderSmokeAndFireMain(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            RenderSmokeAndFire(cmdBuffer, cam, viewport);
        }

        /// <summary>
        /// Upscale the simulation surface to currently bound render target
        /// Used for URP where we can't change render targets
        /// Used for URP where we can't change render targets
        /// </summary>
        public void UpscaleSmokeAndFireDirect(CommandBuffer cmdBuffer, Camera cam,
                                        RenderTargetIdentifier? sourceColorTexture = null,
                                        RenderTargetIdentifier? sourceDepthTexture = null, Rect? viewport = null)
        {
            Material CurrentUpscaleMaterial = cameraResources[cam].upscaleMaterial.currentMaterial;
            Vector2Int cameraNativeResolution = camNativeResolutions[cam];

            cmdBuffer.SetViewport(new Rect(0, 0, cameraNativeResolution.x, cameraNativeResolution.y));
            if (sourceColorTexture == null)
            {
                cmdBuffer.SetGlobalTexture("RenderedVolume", UpscaleColor);
            }
            else
            {
                cmdBuffer.SetGlobalTexture("RenderedVolume", sourceColorTexture.Value);
            }

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentUpscaleMaterial, 0, MeshTopology.Triangles,
                                     6);
        }

        /// <summary>
        /// Render the simulation surface
        /// Camera's targetTexture must be copied to cameraResources[cam].background
        /// using corresponding Render Pipeline before calling this method
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        public void RenderFluid(CommandBuffer cmdBuffer, Camera cam, RenderTargetIdentifier? renderTargetParam = null,
                                RenderTargetIdentifier? depthTargetParam = null, Rect? viewport = null)
        {
            RenderTargetIdentifier renderTarget =
                renderTargetParam ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetRenderTarget(UpscaleColor);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            }
            else
            {
                if (depthTargetParam != null)
                {
                    RenderTargetIdentifier depthTarget = depthTargetParam.Value;
                    cmdBuffer.SetRenderTarget(renderTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                                              depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
                }
                else
                {
                    cmdBuffer.SetRenderTarget(renderTarget);
                }
            }

            RenderSmokeAndFireMain(cmdBuffer, cam, viewport);

            // If downscale enabled then we need to blend it on top of final RenderTexture
            if (EnableDownscale)
            {
                if (depthTargetParam != null)
                {
                    RenderTargetIdentifier depthTarget = depthTargetParam.Value;
                    cmdBuffer.SetRenderTarget(renderTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                                              depthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
                }
                else
                {
                    cmdBuffer.SetRenderTarget(renderTarget);
                }
                //depth in upscale
                UpscaleSmokeAndFireDirect(cmdBuffer, cam, null, null, viewport);
            }
        }


        public void RenderParticlesNative(CommandBuffer cmdBuffer, Camera cam)
        {
            ForceCloseCommandEncoder(cmdBuffer);

            cmdBuffer.SetGlobalTexture("_DepthTexture", BuiltinRenderTextureType.Depth, RenderTextureSubElement.Depth);
            cmdBuffer.SetComputeTextureParam(Renderer, CopyDepthID, "_DepthOUT", DepthTexture);
            cmdBuffer.DispatchCompute(Renderer, CopyDepthID, IntDivCeil(cam.pixelWidth, DEPTH_COPY_WORKGROUP), IntDivCeil(cam.pixelHeight, DEPTH_COPY_WORKGROUP), 1);

            UpdateNativeRenderParams(cmdBuffer, cam);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                cmdBuffer.SetRenderTarget(ParticlesRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            }
            ZibraSmokeAndFireBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, ZibraSmokeAndFireBridge.EventID.Draw);
        }

        /// <summary>
        /// Render the simulation surface
        /// Camera's targetTexture must be copied to cameraResources[cam].background
        /// using corresponding Render Pipeline before calling this method
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        public void RenderSmokeAndFire(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            Vector2Int cameraRenderResolution = camRenderResolutions[cam];

            Material CurrentMaterial = cameraResources[cam].smokeAndFireMaterial.currentMaterial;

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetViewport(new Rect(0, 0, cameraRenderResolution.x, cameraRenderResolution.y));
            }
            else
            {
                if (viewport != null)
                {
                    cmdBuffer.SetViewport(viewport.Value);
                }
            }

#if UNITY_IOS && !UNITY_EDITOR
            if (EnableDownscale && IsBackgroundCopyNeeded(cam))
            {
                CurrentMaterial.EnableKeyword("FLIP_BACKGROUND");
            }
            else
            {
                CurrentMaterial.DisableKeyword("FLIP_BACKGROUND");
            }
#endif
            cmdBuffer.SetGlobalTexture("ParticlesTex", ParticlesRT);
            cmdBuffer.SetGlobalTexture("Background", GetBackgroundToBind(cam));

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentMaterial, 0, MeshTopology.Triangles, 6);
        }

        /// <summary>
        /// Update the camera parameters for the particle renderer
        /// </summary>
        /// <param name="cam">Camera</param>
        ///
        public void UpdateCamera(Camera cam)
        {
            Vector2Int resolution = camRenderResolutions[cam];

            Material CurrentMaterial = cameraResources[cam].smokeAndFireMaterial.currentMaterial;
            Material CurrentUpscaleMaterial = cameraResources[cam].upscaleMaterial.currentMaterial;

            Matrix4x4 Projection = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 ProjectionInverse = Projection.inverse;
            Matrix4x4 View = cam.worldToCameraMatrix;
            Matrix4x4 ViewProjection = Projection * View;
            Matrix4x4 ViewProjectionInverse = ViewProjection.inverse;

            cameraRenderParams.View = cam.worldToCameraMatrix;
            cameraRenderParams.Projection = Projection;
            cameraRenderParams.ProjectionInverse = ProjectionInverse;
            cameraRenderParams.ViewProjection = ViewProjection;
            cameraRenderParams.ViewProjectionInverse = ViewProjectionInverse;
            cameraRenderParams.EyeRayCameraCoeficients = CalculateEyeRayCameraCoeficients(cam);
            cameraRenderParams.WorldSpaceCameraPos = cam.transform.position;
            cameraRenderParams.CameraResolution = new Vector2(resolution.x, resolution.y);
            { // Same as Unity's built-in _ZBufferParams
                float y = cam.farClipPlane / cam.nearClipPlane;
                float x = 1 - y;
                cameraRenderParams.ZBufferParams = new Vector4(x, y, x / cam.farClipPlane, y / cam.farClipPlane);
            }
            cameraRenderParams.CameraID = cameras.IndexOf(cam);

            CurrentMaterial.SetVector("Resolution", cameraRenderParams.CameraResolution);
            CurrentMaterial.SetMatrix("Projection", cameraRenderParams.Projection);
            CurrentMaterial.SetMatrix("ViewProjection", cameraRenderParams.ViewProjection);
            CurrentMaterial.SetMatrix("ProjectionInverse", cameraRenderParams.ProjectionInverse);
            CurrentMaterial.SetMatrix("ViewProjectionInverse", cameraRenderParams.ViewProjectionInverse);
            CurrentMaterial.SetMatrix("EyeRayCameraCoeficients", cameraRenderParams.EyeRayCameraCoeficients);

            Renderer.SetVector("Resolution", cameraRenderParams.CameraResolution);
            Renderer.SetMatrix("Projection", cameraRenderParams.Projection);
            Renderer.SetMatrix("ViewProjection", cameraRenderParams.ViewProjection);
            Renderer.SetMatrix("ProjectionInverse", cameraRenderParams.ProjectionInverse);
            Renderer.SetMatrix("ViewProjectionInverse", cameraRenderParams.ViewProjectionInverse);
            Renderer.SetMatrix("EyeRayCameraCoeficients", cameraRenderParams.EyeRayCameraCoeficients);

            // update the data at the pointer
            Marshal.StructureToPtr(cameraRenderParams, camNativeParams[cam], true);

            Vector2 textureScale = new Vector2((float)resolution.x / resolution.x, (float)resolution.y / resolution.y);

            CurrentMaterial.SetVector("TextureScale", textureScale);

            if (EnableDownscale)
            {
                CurrentUpscaleMaterial.SetVector("TextureScale", textureScale);
            }
        }



        private void ClearCameraCommandBuffers()
        {
            // clear all rendering command buffers if not rendering
            foreach (KeyValuePair<Camera, CommandBuffer> entry in cameraCBs)
            {
                if (entry.Key != null)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                }
            }
            cameraCBs.Clear();
            cameras.Clear();
        }

        private void UpdateNativeRenderParams(CommandBuffer cmdBuffer, Camera cam)
        {
            ZibraSmokeAndFireBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID,
                                                  ZibraSmokeAndFireBridge.EventID.SetRenderParameters,
                                                  camNativeParams[cam]);

        }

        /// <summary>
        /// Rendering callback which is called by every camera in the scene
        /// </summary>
        /// <param name="cam">Camera</param>
        public void RenderCallBack(Camera cam, float renderPipelineRenderScale = 1.0f)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection ||
                cam.cameraType == CameraType.VR)
            {
                ClearCameraCommandBuffers();
                return;
            }

            UpdateCameraResolution(cam, renderPipelineRenderScale);

            if (!cameraResources.ContainsKey(cam))
            {
                cameraResources[cam] = new CameraResources();
            }

            // Re-add command buffers to cameras with new injection points
            if (CurrentInjectionPoint != ActiveInjectionPoint)
            {
                foreach (KeyValuePair<Camera, CommandBuffer> entry in cameraCBs)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                    entry.Key.AddCommandBuffer(CurrentInjectionPoint, entry.Value);
                }
                ActiveInjectionPoint = CurrentInjectionPoint;
            }

            bool visibleInCamera =
                (RenderPipelineDetector.GetRenderPipelineType() != RenderPipelineDetector.RenderPipeline.BuiltInRP) ||
                ((cam.cullingMask & (1 << this.gameObject.layer)) != 0);

            if (!isEnabled || !IsRenderingEnabled() || !visibleInCamera || materialParameters.SmokeMaterial == null ||
                (EnableDownscale && materialParameters.UpscaleMaterial == null))
            {
                if (cameraCBs.ContainsKey(cam))
                {
                    CameraEvent cameraEvent = (cam.actualRenderingPath == RenderingPath.Forward)
                                                  ? CameraEvent.BeforeForwardAlpha
                                                  : CameraEvent.AfterLighting;
                    cam.RemoveCommandBuffer(cameraEvent, cameraCBs[cam]);
                    cameraCBs[cam].Clear();
                    cameraCBs.Remove(cam);
                }

                return;
            }

            bool isDirty = SetMaterialParams(cam);
            isDirty = UpdateNativeTextures(cam, renderPipelineRenderScale) || isDirty;
            isDirty = !cameraCBs.ContainsKey(cam) || isDirty;
#if UNITY_EDITOR
            isDirty = isDirty || ForceRepaint;
#endif

            isDirty = isDirty || isSimulationContainerPositionChanged;
            InitializeNativeCameraParams(cam);
            UpdateCamera(cam);

            if (RenderPipelineDetector.GetRenderPipelineType() != RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
#if UNITY_PIPELINE_HDRP || UNITY_PIPELINE_URP
                // upload camera parameters
                //solverCommandBuffer.Clear();
                //ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                //                                      ZibraSmokeAndFireBridge.EventID.SetCameraParameters,
                //                                      camNativeParams[cam]);
                //Graphics.ExecuteCommandBuffer(solverCommandBuffer);
#endif
            }
            else
            {
                if (!cameraCBs.ContainsKey(cam) || isDirty)
                {
                    CommandBuffer renderCommandBuffer;
                    if (isDirty && cameraCBs.ContainsKey(cam))
                    {
                        renderCommandBuffer = cameraCBs[cam];
                        renderCommandBuffer.Clear();
                    }
                    else
                    {
                        // Create render command buffer
                        renderCommandBuffer = new CommandBuffer { name = "ZibraSmokeAndFire.Render" };
                        // add command buffer to camera
                        cam.AddCommandBuffer(ActiveInjectionPoint, renderCommandBuffer);
                        // add camera to the list
                        cameraCBs[cam] = renderCommandBuffer;
                    }

                    // enable depth texture
                    cam.depthTextureMode = DepthTextureMode.Depth;

                    // update native camera parameters

                    if (IsBackgroundCopyNeeded(cam))
                    {
                        renderCommandBuffer.Blit(BuiltinRenderTextureType.CurrentActive,
                                                 cameraResources[cam].background);
                    }

                    RenderParticlesNative(renderCommandBuffer, cam);
                    RenderFluid(renderCommandBuffer, cam);
                }
            }
        }

        public void RenderCallBackWrapper(Camera cam)
        {
            RenderCallBack(cam);
        }


        protected void ClearSolver()
        {
            if (solverCommandBuffer != null)
            {
                ZibraSmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                      ZibraSmokeAndFireBridge.EventID.ReleaseResources);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            }

            if (solverCommandBuffer != null)
            {
                solverCommandBuffer.Release();
                solverCommandBuffer = null;
            }

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(SDFObjectData);
            SDFObjectData = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ManipulatorStatistics);
            ManipulatorStatistics = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(DynamicManipulatorData);
            ManipulatorStatistics = null;
            Marshal.FreeHGlobal(NativeManipData);
            NativeManipData = IntPtr.Zero;
            Marshal.FreeHGlobal(NativeSimulationData);
            NativeSimulationData = IntPtr.Zero;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(VelocityTexture0); VelocityTexture0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(VelocityTexture1); VelocityTexture1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(TmpSDFTexture); TmpSDFTexture = null;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderColor); RenderColor = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderDensity); RenderDensity = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderDensityLOD); RenderDensityLOD = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderIllumination); RenderIllumination = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ColorTexture0); ColorTexture0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ColorTexture1); ColorTexture1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Divergence); Divergence = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD0); ResidualLOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD1); ResidualLOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD2); ResidualLOD2 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD0); Pressure0LOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD1); Pressure0LOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD2); Pressure0LOD2 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD0); Pressure1LOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD1); Pressure1LOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD2); Pressure1LOD2 = null;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(AtomicCounters); AtomicCounters = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleData0); EffectParticleData0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleData1); EffectParticleData1 = null;

            CurrentTextureResolution = new Vector2Int(0, 0);
            GridSize = new Vector3Int(0, 0, 0);
            NumNodes = 0;
            simulationInternalFrame = 0;
            simulationInternalTime = 0.0f;
            timestep = 0.0f;
            camRenderResolutions.Clear();
            camNativeResolutions.Clear();

            initialized = false;

            // DO NOT USE AllInstances.Remove(this)
            // This will not result in equivalent code
            // ZibraSmokeAndFire::Equals is overriden and don't have correct implementation

            if (AllInstances != null)
            {
                for (int i = 0; i < AllInstances.Count; i++)
                {
                    var fluid = AllInstances[i];
                    if (ReferenceEquals(fluid, this))
                    {
                        AllInstances.RemoveAt(i);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Disable fluid render for a given camera
        /// </summary>
        public void DisableForCamera(Camera cam)
        {
            CameraEvent cameraEvent =
                cam.actualRenderingPath == RenderingPath.Forward ? CameraEvent.AfterSkybox : CameraEvent.AfterLighting;
            cam.RemoveCommandBuffer(cameraEvent, cameraCBs[cam]);
            cameraCBs[cam].Dispose();
            cameraCBs.Remove(cam);
        }

        protected void ClearRendering()
        {
            Camera.onPreRender -= RenderCallBackWrapper;

            ClearCameraCommandBuffers();

            // free allocated memory
            foreach (var data in camNativeParams)
            {
                Marshal.FreeHGlobal(data.Value);
            }

            // TODO
            // Fix memory cleanup
            // Can't currently release this data, since it may be used on render thread
            // Unity doesn't allow us to execute C# code on render thread
            // foreach (var data in camMeshRenderParams)
            //{
            //    Marshal.FreeHGlobal(data.Value);
            //}
            // foreach (var data in toFreeOnExit)
            //{
            //    Marshal.FreeHGlobal(data);
            //}

            foreach (var resource in cameraResources)
            {
                if (resource.Value.background != null)
                {
                    resource.Value.background.Release();
                    resource.Value.background = null;
                }
            }

            cameraResources.Clear();

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(SDFGridTexture);
            SDFGridTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmbeddingsTexture);
            EmbeddingsTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmittersColorsTexture);
            EmittersColorsTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmittersSpriteTexture);
            EmittersSpriteTexture = null;
            camNativeParams.Clear();
        }

        private Matrix4x4 CalculateEyeRayCameraCoeficients(Camera cam)
        {
            float fovTan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            if (cam.orthographic)
            {
                fovTan = 0.0f;
            }
            Vector3 r = cam.transform.right * cam.aspect * fovTan;
            Vector3 u = -cam.transform.up * fovTan;
            Vector3 v = cam.transform.forward;

            return new Matrix4x4(new Vector4(r.x, r.y, r.z, 0.0f), new Vector4(u.x, u.y, u.z, 0.0f),
                                 new Vector4(v.x, v.y, v.z, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f))
                .transpose;
        }

        #endregion
    }
}