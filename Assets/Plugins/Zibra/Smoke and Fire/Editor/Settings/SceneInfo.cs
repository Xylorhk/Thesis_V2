using System;
using UnityEngine.SceneManagement;

namespace com.zibraai.smoke_and_fire
{
    [Serializable]
    internal class SceneStateInfo
    {
        public string Path;
        public bool WasLoaded;

        public SceneStateInfo(Scene scene)
        {
            Path = scene.path;
            WasLoaded = scene.isLoaded;
        }
    }
}
