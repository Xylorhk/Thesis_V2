#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION

using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    [AddComponentMenu("Zibra/Zibra Smoke & Fire Force Field")]
    // Multiple components of this type are allowed
    public class ZibraSmokeAndFireForceField : Manipulator
    {
        public enum ForceFieldType
        {
            Directional,
            Swirl,
            Random
        }

        public enum ForceFieldShape
        {
            Sphere,
            Cube
        }

        public const float STRENGTH_DRAW_THRESHOLD = 0.001f;

        public ForceFieldType Type = ForceFieldType.Directional;

        [Tooltip("The strength of the force acting on the liquid")]
        [Range(0.0f, 15.0f)]
        public float Strength = 1.0f;
        [Tooltip("Velocity of the field")]
        [Range(-5.0f, 5.0f)]
        public float Speed = 1.0f;

        [Tooltip("Size of the random swirls")]
        [Range(0.0f, 64.0f)]
        public float RandomScale = 16.0f;

        [Tooltip("Distance where force field activates")]
        [Range(-10.0f, 10.0f)]
        public float DistanceOffset = 0.0f;

        [Tooltip("Disable applying forces inside the object")]
        public bool DisableForceInside = true;

        [Tooltip("Force vector of the directional force field")]
        public Vector3 ForceDirection = Vector3.up;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.ForceField;
        }

        private void Update()
        {
            AdditionalData0.x = (int)Type;
            AdditionalData0.y = Strength;
            AdditionalData0.z = Speed;
            AdditionalData0.w = DistanceOffset;

            if (Type == ForceFieldType.Random)
            {
                AdditionalData1.x = RandomScale;
            }
            else
            {
                AdditionalData1.x = ForceDirection.x;
                AdditionalData1.y = ForceDirection.y;
                AdditionalData1.z = ForceDirection.z;
            }

            AdditionalData1.w = DisableForceInside ? 1.0f : 0.0f;
        }
    }
}

#endif