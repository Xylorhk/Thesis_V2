using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.zibraai.smoke_and_fire.SDFObjects
{
    /// <summary>
    ///     An analytical Zibra Smoke & Fire SDF
    /// </summary>
    [AddComponentMenu("Zibra/Zibra Analytic SDF")]
    public class AnalyticSDF : SDFObject
    {
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            switch (chosenSDFType)
            {
                case SDFType.Sphere:
                    Gizmos.DrawWireSphere(new Vector3(0, 0, 0), 0.5f);
                    break;
                case SDFType.Box:
                    Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
                    break;
                case SDFType.Capsule:
                    Utilities.GizmosHelper.DrawWireCapsule(transform.position, transform.rotation,
                                                           0.5f * transform.lossyScale.x, 0.5f * transform.lossyScale.y,
                                                           Color.cyan);
                    break;
                case SDFType.Torus:
                    Utilities.GizmosHelper.DrawWireTorus(transform.position, transform.rotation,
                                                         0.5f * transform.lossyScale.x, transform.lossyScale.y, Color.cyan);
                    break;
                case SDFType.Cylinder:
                    Utilities.GizmosHelper.DrawWireCylinder(transform.position, transform.rotation,
                                                            0.5f * transform.lossyScale.x, transform.lossyScale.y,
                                                            Color.cyan);
                    break;
            }
        }

        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }
#endif

        public override ulong GetMemoryFootrpint()
        {
            return 0;
        }
        public override int GetSDFType()
        {
            return 0;
        }
        public override Vector3 GetBBoxSize()
        {
            Vector3 scale = transform.lossyScale;
            switch (chosenSDFType)
            {
                default:
                    return 0.5f * scale;
                case SDFType.Capsule:
                    return new Vector3(scale.x, scale.y, scale.x);
                case SDFType.Torus:
                    return new Vector3(scale.x, scale.y, scale.x);
                case SDFType.Cylinder:
                    return new Vector3(scale.x, scale.y, scale.x);
            }
        }
    }
}