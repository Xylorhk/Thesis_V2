using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    public class ZibraSmokeAndFireCollider : Manipulator
    {
        [Tooltip(
            "0.0 fluid flows without friction, 1.0 fluid sticks to the surface (0 is hydrophobic, 1 is hydrophilic)")]
        [Range(0.0f, 1.0f)]
        public float FluidFriction = 0.0f;


        override public ManipulatorType GetManipulatorType()
        {
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            if (GetComponent<SDFObjects.NeuralSDF>() != null)
                return ManipulatorType.NeuralCollider;
            else if (GetComponent<SDFObjects.SkinnedMeshSDF>() != null)
                return ManipulatorType.GroupCollider;
            else
#endif
                return ManipulatorType.AnalyticCollider;
        }

        private void Update()
        {
            AdditionalData0.w = FluidFriction;
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        public virtual ulong GetMemoryFootrpint()
        {
            return 0;
        }
    }
}
