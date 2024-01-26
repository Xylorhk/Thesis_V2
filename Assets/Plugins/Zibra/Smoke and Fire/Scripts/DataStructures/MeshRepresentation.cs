using System;

namespace com.zibraai.smoke_and_fire.DataStructures
{
    [Serializable]
    public class MeshRepresentation
    {
        public string faces;
        public string vertices;
        public int vox_dim;
        public int sdf_dim;
        public float cutoff_weight;
        public bool static_quantization;
    }

    [Serializable]
    public class SkinnedMeshRepresentation
    {
        public string faces;
        public string vertices;
        public string bone_ids;
        public string bone_weights;
        public int vox_dim;
        public int sdf_dim;
        public float cutoff_weight;
        public bool static_quantization;
    }
}