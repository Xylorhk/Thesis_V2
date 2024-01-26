#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION

using com.zibraai.smoke_and_fire.SDFObjects;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Editor.SDFObjects
{
    [CustomEditor(typeof(SkinnedMeshSDF))]
    [CanEditMultipleObjects]
    public class SkinnedMeshSDFEditor : UnityEditor.Editor
    {
        static SkinnedMeshSDFEditor EditorInstance;

        private SkinnedMeshSDF[] SkinnedSDFs;

        SerializedProperty BoneSDFList;
        SerializedProperty InvertSDF;
        SerializedProperty SurfaceDistance;


        [MenuItem("Zibra AI/Zibra AI - Smoke And Fire/Generate all Skinned Mesh SDFs in the Scene", false, 20)]
        static void GenerateAllSDFs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Neural colliders can only be generated in edit mode.");
                return;
            }

            if (ZibraServerAuthenticationManager.GetInstance().GetStatus() != ZibraServerAuthenticationManager.Status.OK)
            {
                Debug.LogWarning("Licence key validation in process");
                return;
            }

            // Find all neural colliders in the scene
            SkinnedMeshSDF[] skinnedMeshSDFs = FindObjectsOfType<SkinnedMeshSDF>();

            if (skinnedMeshSDFs.Length == 0)
            {
                Debug.LogWarning("No skinned mesh colliders found in the scene.");
                return;
            }

            // Find all corresponding game objects
            GameObject[] skinnedMeshSDFsGameObjects = new GameObject[skinnedMeshSDFs.Length];
            for (int i = 0; i < skinnedMeshSDFs.Length; i++)
            {
                skinnedMeshSDFsGameObjects[i] = skinnedMeshSDFs[i].gameObject;
            }
            // Set selection to that game objects so user can see generation progress
            Selection.objects = skinnedMeshSDFsGameObjects;

            // Add all colliders to the generation queue
            foreach (var skinnedMeshSDF in skinnedMeshSDFs)
            {
                if (!GenerationQueue.Contains(skinnedMeshSDF) && !skinnedMeshSDF.HasRepresentation())
                {
                    GenerationQueue.AddToQueue(skinnedMeshSDF);
                }
            }
        }

        protected void OnEnable()
        {
            EditorInstance = this;

            SkinnedSDFs = new SkinnedMeshSDF[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                SkinnedSDFs[i] = targets[i] as SkinnedMeshSDF;
            }

            serializedObject.Update();
            BoneSDFList = serializedObject.FindProperty("BoneSDFList");
            InvertSDF = serializedObject.FindProperty("InvertSDF");
            SurfaceDistance = serializedObject.FindProperty("SurfaceDistance");
            serializedObject.ApplyModifiedProperties();
        }

        protected void OnDisable()
        {
            if (EditorInstance == this)
            {
                EditorInstance = null;
            }
        }
        private void GenerateSDFs(bool regenerate = false)
        {
            foreach (var instance in SkinnedSDFs)
            {
                instance.BoneSDFList.Clear();

                SkinnedMeshRenderer instanceSkinnedMeshRenderer = instance.GetComponent<SkinnedMeshRenderer>();
                Transform[] bones = instanceSkinnedMeshRenderer.bones;

                List<Mesh> boneMeshes = MeshUtilities.GetSkinnedMeshBoneMeshes(instance.gameObject);

                for (int i = 0; i < bones.Length; i++)
                {
                    Transform bone = bones[i];

                    GameObject boneObject;

                    Transform bonetransform = bone.Find("BoneNeuralSDF");
                    if (bonetransform != null)
                    {
                        DestroyImmediate(bonetransform.gameObject);
                    }

                    if (boneMeshes[i].vertexCount == 0)
                    {
                        continue;
                    }

                    boneObject = new GameObject();
                    boneObject.name = "BoneNeuralSDF";
                    boneObject.transform.SetParent(instance.transform, false);
                    boneObject.AddComponent<NeuralSDF>();
                    NeuralSDF boneSDF = boneObject.GetComponent<NeuralSDF>();
                    instance.BoneSDFList.Add(boneSDF);
                }

                if (!GenerationQueue.Contains(instance) || regenerate)
                {
                    GenerationQueue.AddToQueue(instance);
                }
            }
        }

        public void Update()
        {
            if (GenerationQueue.GetQueueLength() > 0)
                EditorInstance.Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (EditorApplication.isPlaying)
            {
                // Don't allow generation in playmode
            }
            else if (ZibraServerAuthenticationManager.GetInstance().GetStatus() != ZibraServerAuthenticationManager.Status.OK)
            {
                GUILayout.Label("Licence key validation in progress");

                GUILayout.Space(20);
            }
            else
            {
                int toGenerateCount = 0;
                int toRegenerateCount = 0;

                foreach (var instance in SkinnedSDFs)
                {
                    if (instance.HasRepresentation())
                    {
                        toRegenerateCount++;
                    }
                    else
                    {
                        if (!GenerationQueue.Contains(instance))
                            toGenerateCount++;
                    }
                }

                int inQueueCount = SkinnedSDFs.Length - toGenerateCount - toRegenerateCount;
                int fullQueueLength = GenerationQueue.GetQueueLength();
                if (fullQueueLength > 0)
                {
                    if (fullQueueLength != inQueueCount)
                    {
                        if (inQueueCount == 0)
                        {
                            GUILayout.Label($"Generating other SDFs. {fullQueueLength} left in total.");
                        }
                        else
                        {
                            GUILayout.Label(
                                $"Generating SDFs. {inQueueCount} left out of selected SDFs. {fullQueueLength} SDFs left in total.");
                        }
                    }
                    else
                    {
                        GUILayout.Label(SkinnedSDFs.Length > 1 ? $"Generating SDFs. {inQueueCount} left."
                                                                  : "Generating SDF.");
                    }
                    if (GUILayout.Button("Abort"))
                    {
                        GenerationQueue.Abort();
                    }
                }

                if (toGenerateCount > 0)
                {
                    GUILayout.Label(SkinnedSDFs.Length > 1
                                        ? $"{toGenerateCount} skinned mesh SDFs don't have a representation."
                                        : "Skinned mesh SDF doesn't have a representation.");
                    if (GUILayout.Button("Generate skinned mesh SDF"))
                    {
                        GenerateSDFs();
                    }
                }

                if (toRegenerateCount > 0)
                {
                    GUILayout.Label(SkinnedSDFs.Length > 1 ? $"{toRegenerateCount} skinned mesh SDFs already generated."
                                                              : "Skinned mesh SDFs already generated.");
                    if (GUILayout.Button(SkinnedSDFs.Length > 1 ? "Regenerate all selected skinned mesh SDFs"
                                                                   : "Regenerate skinned mesh SDFs"))
                    {
                        GenerateSDFs(true);
                    }
                }
            }

            ulong totalMemoryFootprint = 0;
            foreach (var instance in SkinnedSDFs)
            {
                totalMemoryFootprint += instance.GetMemoryFootrpint();
            }

            if (totalMemoryFootprint != 0)
            {
                GUILayout.Space(10);

                if (SkinnedSDFs.Length > 1)
                {
                    GUILayout.Label("Multiple skinned meshes SDFs selected. Showing sum of all selected instances.");
                }
                GUILayout.Label($"Approximate VRAM footprint:{(float)totalMemoryFootprint / (1 << 20):N2}MB");
            }

            if (SkinnedSDFs.Length == 1)
                EditorGUILayout.PropertyField(BoneSDFList);
            EditorGUILayout.PropertyField(InvertSDF);
            EditorGUILayout.PropertyField(SurfaceDistance);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
