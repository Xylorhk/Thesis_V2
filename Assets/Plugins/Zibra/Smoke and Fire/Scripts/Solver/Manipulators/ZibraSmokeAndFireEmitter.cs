using System;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace com.zibraai.smoke_and_fire.Manipulators
{
    [AddComponentMenu("Zibra/Zibra Smoke & Fire Emitter")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireEmitter : Manipulator
    {
        [Tooltip("Initial velocity of newly created particles")]
        // Rotated with object
        // Used velocity will be equal to GetRotatedInitialVelocity
        public Vector3 InitialVelocity = new Vector3(0, 1, 0);

        [ColorUsage(false, false)]
        public Color SmokeColor = Color.white;

        [Tooltip("Initial smoke density")]
        [Range(0.0f, 1.0f)]
        public float SmokeDensity = 0.1f;

        [Tooltip("Initial temperature of smoke")]
        [Range(0f, 4.0f)]
        public float EmitterTemperature = 0.0f;

        [Tooltip("Initial combustible fuel density")]
        [Range(0f, 1.0f)]
        public float EmitterFuel = 0.0f;

        [Tooltip("Use the object velocity when emitting smoke")]
        public bool UseObjectVelocity = true;

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (InitialVelocity.sqrMagnitude > Vector3.kEpsilon)
            {
                Utilities.GizmosHelper.DrawArrow(transform.position, GetRotatedInitialVelocity(), Color.blue, 0.5f);
            }

            Gizmos.color = Color.blue;
            Gizmos.matrix = GetTransform();
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        }

        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }
#endif
        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Emitter;
        }

        public Vector3 GetRotatedInitialVelocity()
        {
            return transform.rotation * InitialVelocity;
        }

        private void Update()
        {
            Vector3 rotatedInitialVelocity = GetRotatedInitialVelocity();
            AdditionalData0.y = rotatedInitialVelocity.x;
            AdditionalData0.z = rotatedInitialVelocity.y;
            AdditionalData0.w = rotatedInitialVelocity.z;
        }

        override public Matrix4x4 GetTransform()
        {
            return transform.localToWorldMatrix;
        }

        override public Quaternion GetRotation()
        {
            return transform.rotation;
        }

        override public Vector3 GetPosition()
        {
            return transform.position;
        }
        override public Vector3 GetScale()
        {
            return transform.lossyScale;
        }
    }
}