#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION

using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    [AddComponentMenu("Zibra/Zibra Smoke & Fire Detector")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireDetector : Manipulator
    {
        public enum DetectorModes
        {
            Illumination,
            Velocity,
            SmokeDensity,
        }

        public DetectorModes CurrentDetectorMode = DetectorModes.Illumination;

        [NonSerialized]
        public Vector3 CurrentIllumination = Vector3.zero;

        [NonSerialized]
        public Vector3 CurrentIlluminationCenter = Vector3.zero;

        public Light LightToControl;

        [Range(0.0f, 10.0f)]
        public float RelativeBrightness = 1.0f;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.matrix = GetTransform();
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        }

        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Detector;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (CurrentDetectorMode == DetectorModes.Illumination)
            {
                if (LightToControl != null && LightToControl.type == LightType.Point)
                {
                    Vector3 normalized = CurrentIllumination.normalized;
                    float lenght = CurrentIllumination.magnitude;
                    Color color = new Color(normalized.x, normalized.y, normalized.z);
                    LightToControl.color = color;
                    LightToControl.intensity = RelativeBrightness * lenght;
                    Vector3 delta = new Vector3(
                        transform.transform.lossyScale.x * CurrentIlluminationCenter.x,
                        transform.transform.lossyScale.y * CurrentIlluminationCenter.y,
                        transform.transform.lossyScale.z * CurrentIlluminationCenter.z
                        );
                    LightToControl.transform.position = transform.position + 2.0f * delta;
                }
            }
        }
    }
}

#endif