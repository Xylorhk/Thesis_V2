using System;

namespace com.zibraai.smoke_and_fire.DataStructures
{
    [Serializable]
    public class ObjectTransform
    {
        public string Q;
        public string T;
        public string S;
    }

    [Serializable]
    public class VoxelRepresentation
    {
        public string embeds;
        public string sd_grid;
        public ObjectTransform transform;
    }

    [Serializable]
    public class SkinnedVoxelRepresentation
    {
        public VoxelRepresentation[] meshes_data;
    }
}