using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.DataStructures
{
    [Serializable]
    public class ZibraSmokeAndFireSolverParameters : MonoBehaviour
    {
        public const float DEFAULT_MAX_VELOCITY = 10.0f;
        public const float DEFAULT_VISCOSITY = 0.392f;
        public const float DEFAULT_GRAVITY = -9.81f;

        public Vector3 Gravity = new Vector3(0.0f, DEFAULT_GRAVITY, 0.0f);

        [Range(-1.0f, 1.0f)]
        public float SmokeBuoyancy = 0.0f;

        [Range(-1.0f, 1.0f)]
        public float HeatBuoyancy = 0.5f;

        [Range(0.0f, 1.0f)]
        public float TempThreshold = 0.18f;

        [Range(0.0f, 1.0f)]
        public float HeatEmission = 0.078f;

        [Range(0.0f, 1.0f)]
        public float ReactionSpeed = 0.012f;

        [Tooltip("The velocity limit of the particles")]
        [Range(0.0f, 32.0f)]
        public float MaximumVelocity = DEFAULT_MAX_VELOCITY;

        [Range(0.0f, 16.0f)]
        public float MinimumVelocity = 0.0f;
        [Range(0.0f, 3.0f)]
        public float Sharpen = 0.2f;
        [Range(0.0f, 1.0f)]
        public float SharpenThreshold = 0.016f;
        [Range(0.0f, 0.25f)]
        public float ColorDecay = 0.01f;
        [Range(0.0f, 0.25f)]
        public float VelocityDecay = 0.005f;
        [Range(0.0f, 1.0f)]
        public float PressureReuse = 0.95f;
        [Range(0.0f, 2.0f)]
        public float PressureProjection = 1.6f;
        [Range(1, 8)]
        public int PressureSolveIterations = 1;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 100.0f)]
        public float PressureReuseClamp = 50.0f;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 100.0f)]
        public float PressureClamp = 50.0f;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(1, 20)]
        public int LOD0Iterations = 1;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0, 20)]
        public int LOD1Iterations = 2;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0, 20)]
        public int LOD2Iterations = 12;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0, 20)]
        public int PreIterations = 1;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 2.0f)]
        public float MainOverrelax = 1.4f;

#if !ZIBRA_SMOKE_AND_FIRE_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 2.0f)]
        public float EdgeOverrelax = 1.11f;

#if UNITY_EDITOR
        public void OnValidate()
        {
            ValidateParameters();
        }
#endif

        public void ValidateParameters()
        {

        }
    }
}