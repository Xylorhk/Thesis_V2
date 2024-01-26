using com.zibra.smoke_and_fire.Utilities;
using com.zibraai.smoke_and_fire.SDFObjects;
using com.zibraai.smoke_and_fire.Solver;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators
{
    public class ZibraManipulatorManager : MonoBehaviour
    {
        [HideInInspector]
        [StructLayout(LayoutKind.Sequential)]
        public struct ManipulatorParam
        {
            public Int32 Enabled;
            public Int32 SDFObjectID;
            public Int32 ParticleSpecies;
            public Int32 IntParameter;

            public Vector4 AdditionalData0;
            public Vector4 AdditionalData1;
        }

        [HideInInspector]
        [StructLayout(LayoutKind.Sequential)]
        public struct SDFObjectParams
        {
            public Vector3 Position;
            public Single NormalSmooth;

            public Vector3 PrevPosition;
            public Single SurfaceValue;

            public Vector3 Scale;
            public Single DistanceScale;

            public Vector3 PrevScale;
            public Int32 Type;

            public Quaternion Rotation;

            public Quaternion PrevRotation;

            public Vector3 BBoxSize;
            public Single BBoxVolume;

            public Int32 EmbeddingTextureBlocks;
            public Int32 SDFTextureBlocks;
            public Int32 ObjectID;
            public Single TotalGroupVolume;
        };

        [HideInInspector]
        [StructLayout(LayoutKind.Sequential)]
        public struct ManipulatorIndices
        {
            public Int32 EmitterIndexBegin;
            public Int32 EmitterIndexEnd;
            public Int32 VoidIndexBegin;
            public Int32 VoidIndexEnd;

            public Int32 ForceFieldIndexBegin;
            public Int32 ForceFieldIndexEnd;
            public Int32 AnalyticColliderIndexBegin;
            public Int32 AnalyticColliderIndexEnd;

            public Int32 NeuralColliderIndexBegin;
            public Int32 NeuralColliderIndexEnd;
            public Int32 GroupColliderIndexBegin;
            public Int32 GroupColliderIndexEnd;

            public Int32 DetectorIndexBegin;
            public Int32 DetectorIndexEnd;
            public Int32 SpeciesModifierIndexBegin;
            public Int32 SpeciesModifierIndexEnd;

            public Int32 EffectParticleEmitterBegin;
            public Int32 EffectParticleEmitterEnd;
            public Int32 AnalyticForceFieldIndexBegin;
            public Int32 AnalyticForceFieldIndexEnd;

            public Int32 NeuralForceFieldIndexBegin;
            public Int32 NeuralForceFieldIndexEnd;
            public Int32 GroupForceFieldIndexBegin;
            public Int32 GroupForceFieldIndexEnd;
        }

        public ManipulatorIndices indices = new ManipulatorIndices();

        // All data together
        [HideInInspector]
        public int Elements = 0;
        [HideInInspector]
        public List<ManipulatorParam> ManipulatorParams = new List<ManipulatorParam>();
        [HideInInspector]
        public List<SDFObjectParams> SDFObjectList = new List<SDFObjectParams>();
        [HideInInspector]
        public Color32[] Embeddings;
        [HideInInspector]
        public byte[] SDFGrid;
        [HideInInspector]
        public List<int> ConstDataID = new List<int>();

        [HideInInspector]
        public int TextureCount = 0;
        [HideInInspector]
        public int SDFTextureSize = 0;
        [HideInInspector]
        public int EmbeddingTextureSize = 0;

        [HideInInspector]
        public int SDFTextureBlocks = 0;
        [HideInInspector]
        public int EmbeddingTextureBlocks = 0;

        [HideInInspector]
        public int SDFTextureDimension = 0;
        [HideInInspector]
        public int EmbeddingTextureDimension = 0;

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
        [HideInInspector]
        public Dictionary<ZibraHash128, NeuralSDF> neuralSDFs = new Dictionary<ZibraHash128, NeuralSDF>();
        [HideInInspector]
        public Dictionary<ZibraHash128, int> textureHashMap = new Dictionary<ZibraHash128, int>();
#endif

        private Vector3 VectorClamp(Vector3 x, Vector3 min, Vector3 max)
        {
            return Vector3.Max(Vector3.Min(x, max), min);
        }

        private List<Manipulator> manipulators;

        private Vector3 Abs(Vector3 x)
        {
            return new Vector3(
                Mathf.Abs(x.x),
                Mathf.Abs(x.y),
                Mathf.Abs(x.z)
                );
        }

        protected SDFObjectParams GetSDF(SDFObjects.SDFObject obj, Manipulator manipulator)
        {
            SDFObjectParams sdf = new SDFObjectParams();

            if (obj != null)
            {
                sdf.Rotation = obj.transform.rotation;
                sdf.Scale = obj.transform.lossyScale;
                sdf.Position = obj.transform.position;
            }
            else
            {
                sdf.Rotation = manipulator.GetRotation();
                sdf.Scale = manipulator.GetScale();
                sdf.Position = manipulator.GetPosition();
                sdf.BBoxSize = 2.0f * sdf.Scale;
            }

            sdf.NormalSmooth = 0.01f;
            sdf.SurfaceValue = 0.0f;
            SDFObjects.SDFObject main = manipulator.GetComponent<SDFObjects.SDFObject>();
            if (main != null)
            {
                sdf.SurfaceValue += main.SurfaceDistance;
            }
            if (obj != null)
            {
                sdf.SurfaceValue += obj.SurfaceDistance;
            }
            sdf.DistanceScale = 1.0f;
            sdf.Type = 0;
            sdf.TotalGroupVolume = 0.0f;
            sdf.BBoxSize = 0.5f * manipulator.transform.lossyScale;

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            if (manipulator is ZibraSmokeAndFireEmitter || manipulator is ZibraSmokeAndFireVoid)
#else
            if (manipulator is ZibraSmokeAndFireEmitter)
#endif
            {
                //use Box as default
                sdf.Type = 1;
            }

            if (obj is SDFObjects.AnalyticSDF)
            {
                SDFObjects.AnalyticSDF analyticSDF = obj as SDFObjects.AnalyticSDF;
                sdf.Type = (int)analyticSDF.chosenSDFType;
                sdf.DistanceScale = analyticSDF.InvertSDF ? -1.0f : 1.0f;
                sdf.BBoxSize = analyticSDF.GetBBoxSize();
            }
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            if (obj is SDFObjects.NeuralSDF)
            {
                SDFObjects.NeuralSDF neuralSDF = obj as SDFObjects.NeuralSDF;
                Matrix4x4 transf = obj.transform.localToWorldMatrix * neuralSDF.objectRepresentation.ObjectTransform;

                sdf.Rotation = transf.rotation;
                sdf.Scale = Abs(transf.lossyScale) * (1.0f + 0.1f);
                sdf.Position = transf.MultiplyPoint(Vector3.zero);
                sdf.Type = -1;
                sdf.ObjectID = textureHashMap[neuralSDF.objectRepresentation.GetHash()];
                sdf.EmbeddingTextureBlocks = EmbeddingTextureBlocks;
                sdf.SDFTextureBlocks = SDFTextureBlocks;
                sdf.DistanceScale = neuralSDF.InvertSDF ? -1.0f : 1.0f;
                sdf.BBoxSize = sdf.Scale;
            }
#endif
            sdf.BBoxVolume = sdf.BBoxSize.x * sdf.BBoxSize.y * sdf.BBoxSize.z;
            return sdf;
        }

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
        protected void AddTexture(NeuralSDF neuralSDF)
        {
            ZibraHash128 curHash = neuralSDF.objectRepresentation.GetHash();

            if (textureHashMap.ContainsKey(curHash)) return;

            SDFTextureSize += neuralSDF.objectRepresentation.GridResolution / NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION;
            EmbeddingTextureSize += NeuralSDFRepresentation.EMBEDDING_SIZE * neuralSDF.objectRepresentation.EmbeddingResolution / NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION;
            neuralSDFs[curHash] = neuralSDF;

            int sdfID = TextureCount;
            textureHashMap[curHash] = sdfID;

            TextureCount++;
        }

        protected void AddTextureData(NeuralSDF neuralSDF)
        {
            ZibraHash128 curHash = neuralSDF.objectRepresentation.GetHash();
            int sdfID = textureHashMap[curHash];

            //Embedding texture
            for (int t = 0; t < NeuralSDFRepresentation.EMBEDDING_SIZE; t++)
            {
                int block = sdfID * NeuralSDFRepresentation.EMBEDDING_SIZE + t;
                Vector3Int blockPos = NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION * new Vector3Int(
                    block % EmbeddingTextureBlocks,
                    (block / EmbeddingTextureBlocks) % EmbeddingTextureBlocks,
                    block / (EmbeddingTextureBlocks * EmbeddingTextureBlocks)
                );
                int Size = neuralSDF.objectRepresentation.EmbeddingResolution;

                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < Size; j++)
                    {
                        for (int k = 0; k < Size; k++)
                        {
                            Vector3Int pos = blockPos + new Vector3Int(i, j, k);
                            int id = pos.x + EmbeddingTextureDimension * (pos.y + EmbeddingTextureDimension * pos.z);
                            if (id >= EmbeddingTextureSize)
                            {
                                Debug.LogError(pos);
                            }
                            Embeddings[id] = neuralSDF.objectRepresentation.GetEmbedding(i, j, k, t);
                        }
                    }
                }
            }

            //SDF approximation texture
            {
                int block = sdfID;
                Vector3Int blockPos = NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION * new Vector3Int(
                    block % SDFTextureBlocks,
                    (block / SDFTextureBlocks) % SDFTextureBlocks,
                    block / (SDFTextureBlocks * SDFTextureBlocks)
                );
                int Size = neuralSDF.objectRepresentation.GridResolution;
                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < Size; j++)
                    {
                        for (int k = 0; k < Size; k++)
                        {
                            Vector3Int pos = blockPos + new Vector3Int(i, j, k);
                            int id = pos.x + SDFTextureDimension * (pos.y + SDFTextureDimension * pos.z);
                            for (int t = 0; t < 2; t++)
                                SDFGrid[2 * id + t] = neuralSDF.objectRepresentation.GetSDGrid(i, j, k, t);
                        }
                    }
                }
            }
        }

        protected void CalculateTextureData()
        {
            SDFTextureBlocks = (int)Mathf.Ceil(Mathf.Pow(SDFTextureSize, (1.0f / 3.0f)));
            EmbeddingTextureBlocks = (int)Mathf.Ceil(Mathf.Pow(EmbeddingTextureSize, (1.0f / 3.0f)));

            SDFTextureDimension = NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION * SDFTextureBlocks;
            EmbeddingTextureDimension = NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION * EmbeddingTextureBlocks;

            SDFTextureSize = SDFTextureDimension * SDFTextureDimension * SDFTextureDimension;
            EmbeddingTextureSize = EmbeddingTextureDimension * EmbeddingTextureDimension * EmbeddingTextureDimension;

            Array.Resize<Color32>(ref Embeddings, EmbeddingTextureSize);
            Array.Resize<byte>(ref SDFGrid, 2 * SDFTextureSize);

            foreach (var sdf in neuralSDFs.Values)
            {
                AddTextureData(sdf);
            }
        }

#endif

        /// <summary>
        /// Update all arrays and lists with manipulator object data
        /// Should be executed every simulation frame
        /// </summary>
        /// 
        public void UpdateDynamic(ZibraSmokeAndFire parent, float deltaTime = 0.0f)
        {
            Vector3 containerPos = parent.containerPos;
            Vector3 containerSize = parent.containerSize;

            int ID = 0;
            ManipulatorParams.Clear();
            List<SDFObjectParams> SDFObjectOld = new List<SDFObjectParams>(SDFObjectList);
            SDFObjectList.Clear();
            // fill arrays

            foreach (var manipulator in manipulators)
            {
                if (manipulator == null)
                    continue;

                ManipulatorParam manip = new ManipulatorParam();

                manip.Enabled = (manipulator.isActiveAndEnabled && manipulator.gameObject.activeInHierarchy) ? 1 : 0;
                manip.AdditionalData0 = manipulator.AdditionalData0;
                manip.AdditionalData1 = manipulator.AdditionalData1;

                SDFObjectParams sdf = GetSDF(manipulator.GetComponent<SDFObjects.SDFObject>(), manipulator);

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
                //TODO replace with SDF group
                if (manipulator.GetComponent<SDFObjects.SkinnedMeshSDF>() != null)
                {
                    float TotalVolume = 0.0f;
                    Vector3 averageScale = Vector3.zero;
                    Vector3 averagePosition = Vector3.zero;
                    SDFObjects.SkinnedMeshSDF skinnedMeshSDF = manipulator.GetComponent<SDFObjects.SkinnedMeshSDF>();

                    sdf.Type = -2;
                    sdf.ObjectID = SDFObjectList.Count;
                    sdf.SDFTextureBlocks = skinnedMeshSDF.BoneSDFList.Count;

                    foreach (var bone in skinnedMeshSDF.BoneSDFList)
                    {
                        SDFObjectParams boneSDF = GetSDF(bone, manipulator);
                        TotalVolume += boneSDF.BBoxVolume;
                        averageScale += boneSDF.Scale;
                        averagePosition += boneSDF.Position;

                        if (SDFObjectOld.Count > 0)
                        {
                            boneSDF.PrevPosition = SDFObjectOld[SDFObjectList.Count].Position;
                            boneSDF.PrevScale = SDFObjectOld[SDFObjectList.Count].Scale;
                            boneSDF.PrevRotation = SDFObjectOld[SDFObjectList.Count].Rotation;
                        }
                        else
                        {
                            boneSDF.PrevPosition = boneSDF.Position;
                            boneSDF.PrevScale = boneSDF.Scale;
                            boneSDF.PrevRotation = boneSDF.Rotation;
                        }

                        SDFObjectList.Add(boneSDF);
                    }

                    sdf.Position = averagePosition / skinnedMeshSDF.BoneSDFList.Count;
                    sdf.Scale = averageScale / skinnedMeshSDF.BoneSDFList.Count;
                    sdf.TotalGroupVolume = TotalVolume;
                }
#endif

                if (manipulator is ZibraSmokeAndFireEmitter)
                {
                    ZibraSmokeAndFireEmitter emitter = manipulator as ZibraSmokeAndFireEmitter;

                    manip.AdditionalData0.x = 0.0f;

                    Vector3 normalizedColor = new Vector3(emitter.SmokeColor.r, emitter.SmokeColor.g, emitter.SmokeColor.b);
                    if (normalizedColor.sqrMagnitude < 0.001f)
                    {
                        normalizedColor = new Vector3(1.0f, 1.0f, 1.0f);
                    }
                    normalizedColor.Normalize();
                    Vector3 effectiveColor = normalizedColor * emitter.SmokeDensity;
                    float grayscaleColor = Vector3.Dot(effectiveColor, new Vector3(0.2126f, 0.7152f, 0.0722f));

                    if (parent.CurrentSimulationMode == ZibraSmokeAndFire.SimulationMode.Fire)
                    {
                        manip.AdditionalData1 = new Vector4(grayscaleColor, emitter.EmitterFuel, emitter.EmitterTemperature, emitter.UseObjectVelocity ? 1.0f : 0.0f);
                    }
                    else
                    {
                        manip.AdditionalData1 = new Vector4(effectiveColor[0], effectiveColor[1], effectiveColor[2], emitter.UseObjectVelocity ? 1.0f : 0.0f);
                    }
                }

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
                if (manipulator is ZibraSmokeAndFireVoid)
                {
                    ZibraSmokeAndFireVoid voidmanip = manipulator as ZibraSmokeAndFireVoid;
                    manip.AdditionalData0.x = voidmanip.ColorDecay;
                    manip.AdditionalData0.y = voidmanip.VelocityDecay;
                    manip.AdditionalData0.z = voidmanip.Pressure;
                }
#endif

                manip.SDFObjectID = SDFObjectList.Count;

                if (SDFObjectOld.Count > 0)
                {
                    sdf.PrevPosition = SDFObjectOld[manip.SDFObjectID].Position;
                    sdf.PrevScale = SDFObjectOld[manip.SDFObjectID].Scale;
                    sdf.PrevRotation = SDFObjectOld[manip.SDFObjectID].Rotation;
                }
                else
                {
                    sdf.PrevPosition = sdf.Position;
                    sdf.PrevScale = sdf.Scale;
                    sdf.PrevRotation = sdf.Rotation;
                }
                SDFObjectList.Add(sdf);
                ManipulatorParams.Add(manip);
                ID++;
            }

            Elements = manipulators.Count;
        }

        private static float INT2Float(int a)
        {
            const float MAX_INT = 2147483647.0f;
            const float F2I_MAX_VALUE = 5000.0f;
            const float F2I_SCALE = (MAX_INT / F2I_MAX_VALUE);

            return a / F2I_SCALE;
        }

        private int GetStatIndex(int id, int offset)
        {
            return id * Solver.ZibraSmokeAndFire.STATISTICS_PER_MANIPULATOR + offset;
        }

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
        /// <summary>
        /// Update manipulator statistics
        /// </summary>
        public void UpdateStatistics(Int32[] data, List<Manipulator> curManipulators,
                                     DataStructures.ZibraSmokeAndFireSolverParameters solverParameters, DataStructures.ZibraSmokeAndFireMaterialParameters materialParameters)
        {
            int id = 0;
            foreach (var manipulator in manipulators)
            {
                if (manipulator == null)
                    continue;

                switch (manipulator.GetManipulatorType())
                {
                    default:
                        break;
                    case Manipulator.ManipulatorType.Detector:
                        ZibraSmokeAndFireDetector zibradetector = manipulator as ZibraSmokeAndFireDetector;
                        zibradetector.CurrentIllumination =
                                new Vector3(INT2Float(data[GetStatIndex(id, 0)]), INT2Float(data[GetStatIndex(id, 1)]), INT2Float(data[GetStatIndex(id, 2)]));
                        zibradetector.CurrentIllumination *= materialParameters.FireBrightness + materialParameters.BlackBodyBrightness;
                        zibradetector.CurrentIlluminationCenter =
                                new Vector3(INT2Float(data[GetStatIndex(id, 3)]) - 10.0f, INT2Float(data[GetStatIndex(id, 4)]) - 10.0f, INT2Float(data[GetStatIndex(id, 5)]) - 10.0f);

                        break;
                }
#if UNITY_EDITOR
                manipulator.NotifyChange();
#endif

                id++;
            }
        }
#endif

        /// <summary>
        /// Update constant object data and generate and sort the current manipulator list
        /// Should be executed once
        /// </summary>
        public void UpdateConst(List<Manipulator> curManipulators)
        {
            manipulators = new List<Manipulator>(curManipulators);

            // first sort the manipulators
            manipulators.Sort(new ManipulatorCompare());

            int[] TypeIndex = new int[(int)Manipulator.ManipulatorType.TypeNum * 2];
            foreach (Manipulator.ManipulatorType curManipulatorType in Enum.GetValues(typeof(Manipulator.ManipulatorType)))
            {
                if (curManipulatorType == Manipulator.ManipulatorType.None ||
                    curManipulatorType == Manipulator.ManipulatorType.TypeNum)
                {
                    continue;
                }
                Predicate<Manipulator> FindEnabled = delegate (Manipulator curManipulator) 
                { 
                    return curManipulator.enabled && curManipulator.GetManipulatorType() == curManipulatorType; 
                };
                int firstIndex = manipulators.FindIndex(FindEnabled);
                int quantity = manipulators.FindAll(FindEnabled).Count;

                int curTypeIndex = (int)curManipulatorType * 2;
                TypeIndex[curTypeIndex] = firstIndex;
                TypeIndex[curTypeIndex + 1] = firstIndex + quantity;
            }

            indices.EmitterIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.Emitter];
            indices.EmitterIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.Emitter + 1];
            indices.VoidIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.Void];
            indices.VoidIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.Void + 1];
            indices.ForceFieldIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.ForceField];
            indices.ForceFieldIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.ForceField + 1];
            indices.AnalyticColliderIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.AnalyticCollider];
            indices.AnalyticColliderIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.AnalyticCollider + 1];
            indices.NeuralColliderIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.NeuralCollider];
            indices.NeuralColliderIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.NeuralCollider + 1];
            indices.GroupColliderIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.GroupCollider];
            indices.GroupColliderIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.GroupCollider + 1];
            indices.DetectorIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.Detector];
            indices.DetectorIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.Detector + 1];
            indices.SpeciesModifierIndexBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.SpeciesModifier];
            indices.SpeciesModifierIndexEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.SpeciesModifier + 1];
            indices.EffectParticleEmitterBegin = TypeIndex[2 * (int)Manipulator.ManipulatorType.EffectParticleEmitter];
            indices.EffectParticleEmitterEnd = TypeIndex[2 * (int)Manipulator.ManipulatorType.EffectParticleEmitter + 1];

            int enabledForceFieldsCount = indices.ForceFieldIndexEnd - indices.ForceFieldIndexBegin;
            if (enabledForceFieldsCount > 0)
            {
                var forceFieldsList = manipulators.GetRange(indices.ForceFieldIndexBegin, enabledForceFieldsCount);
                {   // Analytic force fields
                    Predicate<Manipulator> FindAnalyticSDF = delegate (Manipulator curManipulator) {
                        return curManipulator.GetComponent<SDFObjects.SDFObject>().GetSDFType() == 0;
                    };
                    int firstAnalytycFFIndex = forceFieldsList.FindIndex(FindAnalyticSDF);
                    int ffQuantity = forceFieldsList.FindAll(FindAnalyticSDF).Count;

                    indices.AnalyticForceFieldIndexBegin = indices.ForceFieldIndexBegin + firstAnalytycFFIndex;
                    indices.AnalyticForceFieldIndexEnd = indices.ForceFieldIndexBegin + firstAnalytycFFIndex + ffQuantity;
                }

                {   // Neural force fields
                    Predicate<Manipulator> FindNeuralSDF = delegate (Manipulator curManipulator) {
                        return curManipulator.GetComponent<SDFObjects.SDFObject>().GetSDFType() == -1;
                    };
                    int firstNeuralFFIndex = forceFieldsList.FindIndex(FindNeuralSDF);
                    int ffQuantity = forceFieldsList.FindAll(FindNeuralSDF).Count;

                    indices.NeuralForceFieldIndexBegin = indices.ForceFieldIndexBegin + firstNeuralFFIndex;
                    indices.NeuralForceFieldIndexEnd = indices.ForceFieldIndexBegin + firstNeuralFFIndex + ffQuantity;
                }

                {   // Group force fields
                    Predicate<Manipulator> FindGroupSDF = delegate (Manipulator curManipulator) {
                        return curManipulator.GetComponent<SDFObjects.SDFObject>().GetSDFType() == -2;
                    };
                    int firstGroupFFIndex = forceFieldsList.FindIndex(FindGroupSDF);
                    int ffQuantity = forceFieldsList.FindAll(FindGroupSDF).Count;

                    indices.GroupForceFieldIndexBegin = indices.ForceFieldIndexBegin + firstGroupFFIndex;
                    indices.GroupForceFieldIndexEnd = indices.ForceFieldIndexBegin + firstGroupFFIndex + ffQuantity;
                }
            }
            else
            {
                indices.AnalyticForceFieldIndexBegin = 0;
                indices.AnalyticForceFieldIndexEnd = 0;
                indices.NeuralForceFieldIndexBegin = 0;
                indices.NeuralForceFieldIndexEnd = 0;
                indices.GroupForceFieldIndexBegin = 0;
                indices.GroupForceFieldIndexEnd = 0;
            }

            if (ConstDataID.Count != 0)
            {
                ConstDataID.Clear();
            }

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            SDFTextureSize = 0;
            EmbeddingTextureSize = 0;
            TextureCount = 0;
            foreach (var manipulator in manipulators)
            {
                if (manipulator == null)
                    continue;

                if (manipulator.GetComponent<SDFObjects.NeuralSDF>() != null)
                {
                    AddTexture(manipulator.GetComponent<SDFObjects.NeuralSDF>());
                }

                if (manipulator.GetComponent<SDFObjects.SkinnedMeshSDF>() != null)
                {
                    SDFObjects.SkinnedMeshSDF skinnedMeshSDF = manipulator.GetComponent<SDFObjects.SkinnedMeshSDF>();

                    foreach (var bone in skinnedMeshSDF.BoneSDFList)
                    {
                        if (bone is SDFObjects.NeuralSDF neuralBone)
                            AddTexture(neuralBone);
                    }
                }
            }

            CalculateTextureData();
#endif
        }
    }
}
