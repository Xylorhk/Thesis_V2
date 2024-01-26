using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    [AddComponentMenu("Zibra/Zibra Smoke & Fire Particle Emitter")]
    [DisallowMultipleComponent]
    public class ZibraParticleEmitter : Manipulator
    {
        public enum RenderingMode
        {
            Default,
            Sprite
        };

        [Min(0)]
        public float EmitedParticlesPerFrame = 1.0f;
        public RenderingMode RenderMode = RenderingMode.Default;
        public Texture2D ParticleSprite;
        public AnimationCurve SizeCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [GradientUsageAttribute(true)]
        public Gradient ParticleColor;
        [Range(0, 2f)]
        public float ParticleMotionBlur = 1.0f;
        [Range(0, 10f)]
        public float ParticleBrightness = 1.0f;
        [Range(0, 1f)]
        public float ParticleColorOscillationAmount = 0;
        [Range(0, 100f)]
        public float ParticleColorOscillationFrequency = 0;
        [Range(0, 1f)]
        public float ParticleSizeOscillationAmount = 0;
        [Range(0, 500f)]
        public float ParticleSizeOscillationFrequency = 0;

        [NonSerialized]
        public bool IsDirty = true;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.EffectParticleEmitter;
        }

        private void Update()
        {
            AdditionalData0.x = EmitedParticlesPerFrame;
            AdditionalData0.y = (int)RenderMode;
            AdditionalData0.z = ParticleMotionBlur;
            AdditionalData0.w = ParticleBrightness;

            AdditionalData1.x = ParticleColorOscillationAmount;
            AdditionalData1.y = ParticleColorOscillationFrequency;
            AdditionalData1.z = ParticleSizeOscillationAmount;
            AdditionalData1.w = ParticleSizeOscillationFrequency;
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