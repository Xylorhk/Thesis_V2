#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
#endif


namespace com.zibraai.smoke_and_fire.SDFObjects
{
    public class SkinnedMeshSDF : SDFObject
    {
        [SerializeField]
        public List<SDFObject> BoneSDFList = new List<SDFObject>();

        public bool HasRepresentation()
        {
            foreach (var bone in BoneSDFList)
            {
                NeuralSDF neuralBone = bone as NeuralSDF;
                if (neuralBone != null)
                {
                    if (!neuralBone.HasRepresentation())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public override ulong GetMemoryFootrpint()
        {
            ulong totalFootprint = 0;
            foreach (var bone in BoneSDFList)
            {
                if (bone != null)
                    totalFootprint += bone.GetMemoryFootrpint();
            }
            return totalFootprint;
        }
        public override int GetSDFType()
        {
            return -2;
        }
        public override Vector3 GetBBoxSize()
        {
            return transform.localScale;
        }
    }
}

#endif