using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
#endif

namespace com.zibraai.smoke_and_fire.DataStructures
{
    [ExecuteInEditMode]
    public class ZibraSmokeAndFireMaterialParameters : MonoBehaviour
    {

#if UNITY_EDITOR
        private static string DEFAULT_UPSCALE_MATERIAL_GUID = "5db2c81e302e40efb0419ec664a50f01";
        private static string DEFAULT_SMOKE_MATERIAL_GUID = "7246813b959848a28c439cc0e41ae98f";
        private static string NO_OP_MATERIAL_GUID = "08f2a0310a8448dd828b0c1e6b46a11f";
#endif
        [Tooltip("Custom mesh fluid material.")]
        public Material SmokeMaterial;

        [Tooltip("Custom upscale material. Not used if you don't enable downscale in Smoke & Fire instance.")]
        public Material UpscaleMaterial;

        [HideInInspector]
        public Material NoOpMaterial;

        [Tooltip("Density of rendered smoke.")]
        [Range(0.0f, 1000.0f)]
        public float SmokeDensity = 50.0f;

        [Tooltip("Density of rendered fuel.")]
        [Range(0.0f, 1000.0f)]
        public float FuelDensity = 50.0f;

        [ColorUsage(true, true)]
        [Tooltip("The absorption coefficients")]
        public Color AbsorptionColor = new Color(0.95f, 0.96f, 1.0f, 1.0f);
        [ColorUsage(true, true)]
        [Tooltip("The scattering coefficients")]
        public Color ScatteringColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        [ColorUsage(true, true)]
        [Tooltip("The shadow absorption coefficients")]
        public Color ShadowColor = new Color(0.95f, 0.96f, 1.0f, 1.0f);

        [Range(0.0f, 1.0f)]
        public float ScatteringAttenuation = 0.2f;
        [Range(0.0f, 1.0f)]
        public float ScatteringContribution = 0.2f;

        public bool ObjectPrimaryShadows = true;
        public bool ObjectIlluminationShadows = true;

        [Min(0.0f)]
        public float IlluminationBrightness = 1.0f;

        [Range(0.0f, 1.0f)]
        public float IlluminationSoftness = 0.6f;

        [Min(0.0f)]
        public float BlackBodyBrightness = 1.0f;

        [Min(0.0f)]
        public float FireBrightness = 1.0f;

        [ColorUsage(true, true)]
        [Tooltip("The shadow absorption coefficients")]
        public Color FireColor = new Color(0.7f, 0.8f, 1.0f, 1.0f);

        [Range(-1.0f, 15.0f)]
        public float TemperatureDensityDependence = 0.0f;

        [Range(0.0f, 1.0f)]
        public float ObjectShadowIntensity = 0.75f;
        [Range(0.0f, 10.0f)]
        public float ShadowDistanceDecay = 2.0f;
        [Range(0.0f, 1.0f)]
        public float ShadowIntensity = 0.5f;
        [Range(0.0f, 1.0f)]
        public float VolumeEdgeFadeoff = 0.008f;


        [Range(0.5f, 10.0f)]
        public float RayMarchingStepSize = 2.5f;

        [Range(0.05f, 1.0f)]
        public float ShadowResolution = 0.25f;

        [Range(1.0f, 10.0f)]
        public float ShadowStepSize = 1.5f;

        [Range(8, 512)]
        public int ShadowMaxSteps = 256;

        [Range(0.05f, 1.0f)]
        public float IlluminationResolution = 0.25f;

        [Range(1.0f, 10.0f)]
        public float IlluminationStepSize = 1.5f;

        [Range(0, 512)]
        public int IlluminationMaxSteps = 64;

        [Range(0, 8388608)]
        public int MaxEffectParticles = 32768;

        // Must fit in 12 bits
        [Range(0, 4095)]
        public int ParticleLifetime = 150;

        [Range(0.05f, 1.0f)]
        public float ParticleOcclusionResolution = 0.25f;

        //[HideInInspector]
        //[SerializeField]
        //private int ObjectVersion = 1;

#if UNITY_EDITOR
        void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            Debug.Log("Zibra Smoke & Fire Material Parameters format was updated. Please resave scene.");
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif

        [ExecuteInEditMode]
        public void Awake()
        {
            // If Material Parameters is in old format we need to parse old parameters and come up with equivalent new
            // ones
#if UNITY_EDITOR
            bool updated = false;
#endif

#if UNITY_EDITOR
            if (updated)
            {
                // Can't mark object dirty in Awake, since scene is not fully loaded yet
                UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            }
#endif
        }

#if UNITY_EDITOR
        public void OnDestroy()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        void Reset()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
            UpscaleMaterial = AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            string DefaultSmokeMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SMOKE_MATERIAL_GUID);
            SmokeMaterial =
                AssetDatabase.LoadAssetAtPath(DefaultSmokeMaterialPath, typeof(Material)) as Material;
            string NoOpMaterialPath = AssetDatabase.GUIDToAssetPath(NO_OP_MATERIAL_GUID);
            NoOpMaterial = AssetDatabase.LoadAssetAtPath(NoOpMaterialPath, typeof(Material)) as Material;
        }

        void OnValidate()
        {
            if (UpscaleMaterial == null)
            {
                string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
                UpscaleMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            }
            if (SmokeMaterial == null)
            {
                string DefaultSmokeMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SMOKE_MATERIAL_GUID);
                SmokeMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultSmokeMaterialPath, typeof(Material)) as Material;
            }
            string NoOpMaterialPath = AssetDatabase.GUIDToAssetPath(NO_OP_MATERIAL_GUID);
            NoOpMaterial = AssetDatabase.LoadAssetAtPath(NoOpMaterialPath, typeof(Material)) as Material;
        }
#endif
    }
}