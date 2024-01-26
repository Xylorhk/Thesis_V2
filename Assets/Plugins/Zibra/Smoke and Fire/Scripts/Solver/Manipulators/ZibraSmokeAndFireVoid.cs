#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION

using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    [AddComponentMenu("Zibra/Zibra Smoke & Fire Void")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireVoid : Manipulator
    {
        public float ColorDecay = 0.95f;
        public float VelocityDecay = 0.95f;
        public float Pressure = -0.05f;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Void;
        }
    }
}

#endif
