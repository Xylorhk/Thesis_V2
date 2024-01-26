using System.Collections.Generic;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Solver
{
    class ZibraSmokeAndFireGPUGarbageCollector : MonoBehaviour
    {
        static bool GarbageCollectorEnabled = false;
        static List<ComputeBuffer> buffersToClear = new List<ComputeBuffer>();
        static List<RenderTexture> texturesToClear = new List<RenderTexture>();
        static List<GraphicsBuffer> graphicsBuffersToClear = new List<GraphicsBuffer>();
        static List<Texture3D> textures3DToClear = new List<Texture3D>();
        static List<Texture2D> textures2DToClear = new List<Texture2D>();
        private static void SafeReleaseImmediate(ComputeBuffer obj)
        {
            if (obj != null)
            {
                obj.Release();
            }
        }

        private static void SafeReleaseImmediate(GraphicsBuffer obj)
        {
            if (obj != null)
            {
                obj.Release();
            }
        }

        private static void SafeReleaseImmediate(RenderTexture obj)
        {
            if (obj != null)
            {
                obj.Release();
            }
        }

        private static void SafeReleaseImmediate(Texture3D obj)
        {
            if (obj != null)
            {
                DestroyImmediate(obj, true);
            }
        }

        private static void SafeReleaseImmediate(Texture2D obj)
        {
            if (obj != null)
            {
                DestroyImmediate(obj, true);
            }
        }

        public static void SafeRelease(ComputeBuffer obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                buffersToClear.Add(obj);
            }
        }

        public static void SafeRelease(GraphicsBuffer obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                graphicsBuffersToClear.Add(obj);
            }
        }

        public static void SafeRelease(RenderTexture obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                texturesToClear.Add(obj);
            }
        }

        public static void SafeRelease(Texture3D obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                textures3DToClear.Add(obj);
            }
        }

        public static void SafeRelease(Texture2D obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                textures2DToClear.Add(obj);
            }
        }
        static void GCUpdate()
        {
            int isEmpty = ZibraSmokeAndFireBridge.GarbageCollect();
            if (isEmpty == 1)
            {
                for (int i = 0; i < buffersToClear.Count; i++)
                {
                    SafeReleaseImmediate(buffersToClear[i]);
                }
                for (int i = 0; i < texturesToClear.Count; i++)
                {
                    SafeReleaseImmediate(texturesToClear[i]);
                }
                for (int i = 0; i < graphicsBuffersToClear.Count; i++)
                {
                    SafeReleaseImmediate(graphicsBuffersToClear[i]);
                }
                for (int i = 0; i < textures3DToClear.Count; i++)
                {
                    SafeReleaseImmediate(textures3DToClear[i]);
                }
                for (int i = 0; i < textures2DToClear.Count; i++)
                {
                    SafeReleaseImmediate(textures2DToClear[i]);
                }
                buffersToClear.Clear();
                texturesToClear.Clear();
                graphicsBuffersToClear.Clear();
                textures3DToClear.Clear();
                textures2DToClear.Clear();

                if (GarbageCollectorEnabled)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.update -= GCUpdate;
#endif
                    GarbageCollectorEnabled = false;
                }
            }
        }

#if !UNITY_EDITOR
        private void Update()
        {
            GCUpdate();
            if (!GarbageCollectorEnabled)
            {
                Destroy(this.gameObject);
            }
        }
#endif

        static public void GCUpdateWrapper()
        {
            if (ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                GCUpdate();
            }
        }

        public static void CreateGarbageCollector()
        {
            if (!ZibraSmokeAndFireBridge.NeedGarbageCollect())
            {
                return;
            }

            if (GarbageCollectorEnabled)
            {
                // Garbage collector already exists
                // No need to create another one
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += GCUpdate;
#else

            var garbageCollector = new GameObject("ZibraSmokeAndFire GPU Garbage Collector");
            garbageCollector.AddComponent<ZibraSmokeAndFireGPUGarbageCollector>();
            DontDestroyOnLoad(garbageCollector);
#endif
            GarbageCollectorEnabled = true;
        }
    }
}
