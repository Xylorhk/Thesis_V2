#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION && UNITY_EDITOR

using com.zibraai.smoke_and_fire.DataStructures;
using com.zibraai.smoke_and_fire.SDFObjects;
using com.zibraai.smoke_and_fire.Utilities;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace com.zibraai.smoke_and_fire.Editor.SDFObjects
{
    public abstract class NeuralSDFGenerator
    {
        // Limits for representation generation web requests
        protected const uint REQUEST_TRIANGLE_COUNT_LIMIT = 100000;
        protected const uint REQUEST_SIZE_LIMIT = 3 << 20; // 3mb

        protected Mesh meshToProcess;
        protected Bounds MeshBounds;
        protected UnityWebRequest CurrentRequest;

        public abstract void Start();

        protected bool CheckMeshSize()
        {
            if (meshToProcess.triangles.Length / 3 > REQUEST_TRIANGLE_COUNT_LIMIT)
            {
                string errorMessage =
                    $"Mesh is too large. Can't generate representation. Triangle count should not exceed {REQUEST_TRIANGLE_COUNT_LIMIT} triangles, but current mesh have {meshToProcess.triangles.Length / 3} triangles";
                EditorUtility.DisplayDialog("ZibraSmokeAndFire Error.", errorMessage, "OK");
                Debug.LogError(errorMessage);
                return true;
            }
            return false;
        }

        protected void SendRequest(string requestURL, string json)
        {
            if (CurrentRequest != null)
            {
                CurrentRequest.Dispose();
                CurrentRequest = null;
            }

            if (json.Length > REQUEST_SIZE_LIMIT)
            {
                string errorMessage =
                    $"Mesh is too large. Can't generate representation. Please decrease vertex/triangle count. Web request should not exceed {REQUEST_SIZE_LIMIT / (1 << 20):N2}mb, but for current mesh {(float)json.Length / (1 << 20):N2}mb is needed.";
                EditorUtility.DisplayDialog("ZibraSmokeAndFire Error.", errorMessage, "OK");
                Debug.LogError(errorMessage);
                return;
            }

            if (ZibraServerAuthenticationManager.GetInstance().GetStatus() == ZibraServerAuthenticationManager.Status.OK)
            {
                if (requestURL != "")
                {
#if UNITY_2022_2_OR_NEWER
                    CurrentRequest = UnityWebRequest.PostWwwForm(requestURL, json);
#else
                    CurrentRequest = UnityWebRequest.Post(requestURL, json);
#endif
                    CurrentRequest.SendWebRequest();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Zibra Smoke & Fire Error",
                                            ZibraServerAuthenticationManager.GetInstance().ErrorText, "Ok");
                Debug.LogError(ZibraServerAuthenticationManager.GetInstance().ErrorText);
            }
        }

        public void Abort()
        {
            CurrentRequest?.Dispose();
        }

        protected abstract void ProcessResult();

        public void Update()
        {
            if (CurrentRequest != null && CurrentRequest.isDone)
            {
#if UNITY_2020_2_OR_NEWER
                if (CurrentRequest.isDone && CurrentRequest.result == UnityWebRequest.Result.Success)
#else
                if (CurrentRequest.isDone && !CurrentRequest.isHttpError && !CurrentRequest.isNetworkError)
#endif
                {
                    ProcessResult();
                }
                else
                {
                    EditorUtility.DisplayDialog("Zibra Smoke & Fire Server Error", CurrentRequest.error, "Ok");
                    Debug.LogError(CurrentRequest.downloadHandler.text);
                }

                CurrentRequest.Dispose();
                CurrentRequest = null;

                // make sure to mark the scene as changed
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }

        public bool IsFinished()
        {
            return CurrentRequest == null;
        }
    }

    public class MeshNeuralSDFGenerator : NeuralSDFGenerator
    {
        private NeuralSDFRepresentation NeuralSDFInstance;

        public MeshNeuralSDFGenerator(NeuralSDFRepresentation NeuralSDF, Mesh mesh)
        {
            this.meshToProcess = mesh;
            this.NeuralSDFInstance = NeuralSDF;
        }

        public NeuralSDFRepresentation GetSDF()
        {
            return NeuralSDFInstance;
        }

        public void CreateMeshBBCube()
        {
            MeshBounds = meshToProcess.bounds;
            NeuralSDFInstance.BoundingBoxCenter = MeshBounds.center;
            NeuralSDFInstance.BoundingBoxSize = MeshBounds.size;
        }

        public override void Start()
        {
            if (CheckMeshSize()) return;

            var meshRepresentation = new MeshRepresentation
            {
                vertices = meshToProcess.vertices.Vector3ToString(),
                faces = meshToProcess.triangles.IntToString(),
                vox_dim = NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION,
                sdf_dim = NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION,
                cutoff_weight = 0.1f,
                static_quantization = true
            };

            var json = JsonUtility.ToJson(meshRepresentation);
            string requestURL = ZibraServerAuthenticationManager.GetInstance().GenerationURL;

            SendRequest(requestURL, json);
        }

        protected override void ProcessResult()
        {
            var json = CurrentRequest.downloadHandler.text;
            VoxelRepresentation newRepresentation = JsonUtility.FromJson<SkinnedVoxelRepresentation>(json).meshes_data[0];

            if (string.IsNullOrEmpty(newRepresentation.embeds) ||
                string.IsNullOrEmpty(newRepresentation.sd_grid))
            {
                EditorUtility.DisplayDialog("Zibra Smoke & Fire Server Error",
                                            "Server returned empty result. Contact Zibra Smoke & Fire support",
                                            "Ok");
                Debug.LogError("Server returned empty result. Contact Zibra Smoke & Fire support");

                return;
            }

            CreateMeshBBCube();

            NeuralSDFInstance.CurrentRepresentationV3 = newRepresentation;
            NeuralSDFInstance.CreateRepresentation(NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION, NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION);
        }
    }

    public class SkinnedNeuralSDFGenerator : NeuralSDFGenerator
    {
        private List<SDFObject> NeuralSDFInstances;
        private Transform[] BoneTransforms;
        private SkinnedMeshRenderer renderer;

        public SkinnedNeuralSDFGenerator(List<SDFObject> NeuralSDFs, Transform[] bones, SkinnedMeshRenderer r)
        {
            renderer = r;
            meshToProcess = MeshUtilities.GetMesh(r.gameObject);
            NeuralSDFInstances = NeuralSDFs;
            BoneTransforms = bones;
        }

        public override void Start()
        {
            if (CheckMeshSize()) return;

            int[] bone_ids = new int[meshToProcess.vertexCount * 4];
            float[] bone_weights = new float[meshToProcess.vertexCount * 4];

            Mesh sharedMesh = renderer.sharedMesh;

            for (int i = 0; i < sharedMesh.vertexCount; i++)
            {
                var weight = sharedMesh.boneWeights[i];
                bone_ids[i * 4 + 0] = weight.boneIndex0;
                bone_ids[i * 4 + 1] = (weight.weight1 == 0.0f) ? -1 : weight.boneIndex1;
                bone_ids[i * 4 + 2] = (weight.weight2 == 0.0f) ? -1 : weight.boneIndex2;
                bone_ids[i * 4 + 3] = (weight.weight3 == 0.0f) ? -1 : weight.boneIndex3;

                bone_weights[i * 4 + 0] = weight.weight0;
                bone_weights[i * 4 + 1] = weight.weight1;
                bone_weights[i * 4 + 2] = weight.weight2;
                bone_weights[i * 4 + 3] = weight.weight3;
            }

            var meshRepresentation = new SkinnedMeshRepresentation
            {
                vertices = meshToProcess.vertices.Vector3ToString(),
                faces = meshToProcess.triangles.IntToString(),
                bone_ids = bone_ids.IntToString(),
                bone_weights = bone_weights.FloatToString(),
                vox_dim = NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION,
                sdf_dim = NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION,
                cutoff_weight = 0.1f,
                static_quantization = true
            };

            var json = JsonUtility.ToJson(meshRepresentation);
            string requestURL = ZibraServerAuthenticationManager.GetInstance().GenerationURL;

            SendRequest(requestURL, json);
        }

        protected override void ProcessResult()
        {
            SkinnedVoxelRepresentation newRepresentation = null;

            var json = CurrentRequest.downloadHandler.text;
            newRepresentation = JsonUtility.FromJson<SkinnedVoxelRepresentation>(json);

            if (newRepresentation.meshes_data == null)
            {
                EditorUtility.DisplayDialog("Zibra Smoke & Fire Server Error",
                                            "Server returned empty result. Contact Zibra Smoke & Fire support",
                                            "Ok");
                Debug.LogError("Server returned empty result. Contact Zibra Smoke & Fire support");

                return;
            }

            int j = 0;
            for (int i = 0; i < newRepresentation.meshes_data.Length; i++)
            {
                var representation = newRepresentation.meshes_data[i];

                if (string.IsNullOrEmpty(representation.embeds) ||
                    string.IsNullOrEmpty(representation.sd_grid))
                {
                    continue;
                }

                var instance = NeuralSDFInstances[j];

                if (instance is NeuralSDF neuralSDF)
                {
                    neuralSDF.objectRepresentation.CurrentRepresentationV3 = representation;
                    neuralSDF.objectRepresentation.CreateRepresentation(NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION, NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION);
                    neuralSDF.transform.SetParent(BoneTransforms[i], true);
                    j++;
                }
            }
        }
    }



    static public class GenerationQueue
    {
        static Queue<NeuralSDFGenerator> SDFsToGenerate = new Queue<NeuralSDFGenerator>();
        static Dictionary<MeshNeuralSDFGenerator, NeuralSDF> Generators = new Dictionary<MeshNeuralSDFGenerator, NeuralSDF>();
        static Dictionary<SkinnedNeuralSDFGenerator, SkinnedMeshSDF> SkinnedGenerators = new Dictionary<SkinnedNeuralSDFGenerator, SkinnedMeshSDF>();

        static void Update()
        {
            if (SDFsToGenerate.Count == 0)
                Abort();

            SDFsToGenerate.Peek().Update();
            if (SDFsToGenerate.Peek().IsFinished())
            {
                RemoveFromQueue();
                if (SDFsToGenerate.Count > 0)
                {
                    SDFsToGenerate.Peek().Start();
                }
            }
        }

        static void RemoveFromQueue()
        {
            if (SDFsToGenerate.Peek() is MeshNeuralSDFGenerator)
                Generators.Remove(SDFsToGenerate.Peek() as MeshNeuralSDFGenerator);

            if (SDFsToGenerate.Peek() is SkinnedNeuralSDFGenerator)
                SkinnedGenerators.Remove(SDFsToGenerate.Peek() as SkinnedNeuralSDFGenerator);

            SDFsToGenerate.Dequeue();

            if (SDFsToGenerate.Count == 0)
            {
                EditorApplication.update -= Update;
            }
        }

        static public void AddToQueue(NeuralSDFGenerator generator)
        {
            if (!SDFsToGenerate.Contains(generator))
            {
                if (SDFsToGenerate.Count == 0)
                {
                    EditorApplication.update += Update;
                    generator.Start();
                }
                SDFsToGenerate.Enqueue(generator);
            }
        }

        static public void AddToQueue(NeuralSDF sdf)
        {
            if (Contains(sdf)) return;

            Mesh objectMesh = MeshUtilities.GetMesh(sdf.gameObject);
            if (objectMesh == null) return;

            MeshNeuralSDFGenerator gen = new MeshNeuralSDFGenerator(sdf.objectRepresentation, objectMesh);
            AddToQueue(gen);
            Generators[gen] = sdf;
        }

        static public void AddToQueue(SkinnedMeshSDF sdf)
        {
            if (Contains(sdf)) return;

            SkinnedMeshRenderer instanceSkinnedMeshRenderer = sdf.GetComponent<SkinnedMeshRenderer>();
            Transform[] bones = instanceSkinnedMeshRenderer.bones;
            if (instanceSkinnedMeshRenderer == null) return;

            SkinnedNeuralSDFGenerator gen = new SkinnedNeuralSDFGenerator(sdf.BoneSDFList, bones, instanceSkinnedMeshRenderer);
            AddToQueue(gen);
            SkinnedGenerators[gen] = sdf;
        }

        static public void Abort()
        {
            if (SDFsToGenerate.Count > 0)
            {
                SDFsToGenerate.Peek().Abort();
                SDFsToGenerate.Clear();
                Generators.Clear();
                SkinnedGenerators.Clear();
                EditorApplication.update -= Update;
            }
        }

        static public int GetQueueLength()
        {
            return SDFsToGenerate.Count;
        }

        static public bool Contains(NeuralSDF sdf)
        {
            return Generators.ContainsValue(sdf);
        }

        static public bool Contains(SkinnedMeshSDF sdf)
        {
            return SkinnedGenerators.ContainsValue(sdf);
        }
    }
}

#endif