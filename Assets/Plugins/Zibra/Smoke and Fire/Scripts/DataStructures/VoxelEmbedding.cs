using System;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.DataStructures
{
    [Serializable]
    public struct VoxelEmbedding
    {
        public Color32[] embeds;
        public byte[] grid;
    }
}