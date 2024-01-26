using com.zibraai.smoke_and_fire.Manipulators;
using com.zibraai.smoke_and_fire.SDFObjects;
using com.zibraai.smoke_and_fire.Solver;
using com.zibraai.smoke_and_fire.Utilities;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if UNITY_PIPELINE_URP
using System.Reflection;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
#endif

namespace com.zibraai.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFire), true)]
    [CanEditMultipleObjects]
    public class ZibraSmokeAndFireEditor : UnityEditor.Editor
    {
        public static int instanceCount = 0;

        [MenuItem("GameObject/Zibra Smoke And Fire/Simulation", false, 10)]
        private static void CreateZibraSmokeAndFire(MenuCommand menuCommand)
        {
            instanceCount++;
            // Create a custom game object
            var go = new GameObject("Zibra Smoke & Fire " + instanceCount);
            ZibraSmokeAndFire instance = go.AddComponent<ZibraSmokeAndFire>();
            // Moving component up the list, so important parameters are at the top
            for (int i = 0; i < 4; i++)
                UnityEditorInternal.ComponentUtility.MoveComponentUp(instance);
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Create emitter for new simulation instance
            String emitterName = "Zibra Smoke & Fire Emitter " + (Manipulator.AllManipulators.Count + 1);
            var emitterGameObject = new GameObject(emitterName);
            var emitter = emitterGameObject.AddComponent<ZibraSmokeAndFireEmitter>();
            // Add emitter as child to simulation instance and add it to manipulators list
            GameObjectUtility.SetParentAndAlign(emitterGameObject, go);
            instance.AddManipulator(emitter);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Zibra Smoke And Fire/Emitter", false, 10)]
        private static void CreateZibraEmitter(MenuCommand menuCommand)
        {
#if !ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            if (Manipulator.AllManipulators.Count >= 1)
                return;
#endif

            // Create a custom game object
            String name = "Zibra Smoke & Fire Emitter " + (Manipulator.AllManipulators.Count + 1);
            var go = new GameObject(name);
            var newEmitter = go.AddComponent<ZibraSmokeAndFireEmitter>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newEmitter);
            Selection.activeObject = go;
        }

#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
        [MenuItem("GameObject/Zibra Smoke And Fire/Void", false, 10)]
        private static void CreateZibraVoid(MenuCommand menuCommand)
        {
            String name = "Zibra Smoke & Fire Void " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newVoid = go.AddComponent<ZibraSmokeAndFireVoid>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newVoid);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Zibra Smoke And Fire/Detector", false, 10)]
        private static void CreateZibraDetector(MenuCommand menuCommand)
        {
            String name = "Zibra Smoke & Fire Detector " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newDetector = go.AddComponent<ZibraSmokeAndFireDetector>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newDetector);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Zibra Smoke And Fire/Force Field", false, 10)]
        private static void CreateZibraForceField(MenuCommand menuCommand)
        {
            String name = "Zibra Smoke & Fire Force Field " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newForceField = go.AddComponent<ZibraSmokeAndFireForceField>();
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.chosenSDFType = SDFObject.SDFType.Box;
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newForceField);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Zibra Smoke And Fire/Particle Emitter", false, 10)]
        private static void CreateZibraParticleEmitter(MenuCommand menuCommand)
        {
            String name = "Zibra Smoke & Fire Particle Emitter " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newParticleEmitter = go.AddComponent<ZibraParticleEmitter>();
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.chosenSDFType = SDFObject.SDFType.Box;
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newParticleEmitter);
            Selection.activeObject = go;
        }
#endif

        private const int GRID_NODE_COUNT_WARNING_THRESHOLD = 16000000;

        private enum EditMode
        {
            None,
            Container,
            Emitter
        }

        private static readonly Color containerColor = new Color(1f, 0.8f, 0.4f);

        private ZibraSmokeAndFire[] ZibraSmokeAndFireInstances;

        private SerializedProperty ContainerSize;
        private SerializedProperty TimeStep;
        private SerializedProperty SimTimePerSec;

        private SerializedProperty SimulationIterations;
        private SerializedProperty GridResolution;
        private SerializedProperty RunSimulation;
        private SerializedProperty RunRendering;
        private SerializedProperty fixVolumeWorldPosition;
        private SerializedProperty maximumFramerate;
        private SerializedProperty manipulators;
        private SerializedProperty mainLight;
        private SerializedProperty CurrentSimulationMode;
        private SerializedProperty lights;
        private SerializedProperty limitFramerate;
        private SerializedProperty EnableDownscale;
        private SerializedProperty DownscaleFactor;
        private SerializedProperty CurrentInjectionPoint;
        private bool manipulatorDropdownToggle = true;
        private bool statsDropdownToggle = true;
        private EditMode editMode;
        private readonly BoxBoundsHandle boxBoundsHandleContainer = new BoxBoundsHandle();

        private GUIStyle containerText;

        protected void TriggerRepaint()
        {
            Repaint();
        }

        protected void OnEnable()
        {
            // Only need to add callback to one of instances
            ZibraSmokeAndFire anyInstance = target as ZibraSmokeAndFire;
            // Disabled, we don't want to repaint smoke & fire editor each frame
            // anyInstance.onChanged += TriggerRepaint;

            ZibraSmokeAndFireInstances = new ZibraSmokeAndFire[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ZibraSmokeAndFireInstances[i] = targets[i] as ZibraSmokeAndFire;
            }

            serializedObject.Update();

            mainLight = serializedObject.FindProperty("mainLight");
            CurrentSimulationMode = serializedObject.FindProperty("CurrentSimulationMode");
            lights = serializedObject.FindProperty("lights");
            ContainerSize = serializedObject.FindProperty("containerSize");
            TimeStep = serializedObject.FindProperty("timeStep");
            SimTimePerSec = serializedObject.FindProperty("simTimePerSec");

            SimulationIterations = serializedObject.FindProperty("SimulationIterations");
            GridResolution = serializedObject.FindProperty("gridResolution");

            RunSimulation = serializedObject.FindProperty("runSimulation");
            RunRendering = serializedObject.FindProperty("runRendering");
            fixVolumeWorldPosition = serializedObject.FindProperty("fixVolumeWorldPosition");
            maximumFramerate = serializedObject.FindProperty("maximumFramerate");

            manipulators = serializedObject.FindProperty("manipulators");

            limitFramerate = serializedObject.FindProperty("limitFramerate");

            EnableDownscale = serializedObject.FindProperty("EnableDownscale");
            DownscaleFactor = serializedObject.FindProperty("DownscaleFactor");

            CurrentInjectionPoint = serializedObject.FindProperty("CurrentInjectionPoint");

            serializedObject.ApplyModifiedProperties();

            containerText = new GUIStyle { alignment = TextAnchor.MiddleLeft, normal = { textColor = containerColor } };
        }

        protected void OnDisable()
        {
            ZibraSmokeAndFire anyInstance = target as ZibraSmokeAndFire;
            anyInstance.onChanged -= TriggerRepaint;
        }

        // Toggled with "Edit Container Area" button
        protected void OnSceneGUI()
        {
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.initialized)
                {
                    continue;
                }

                var localToWorld = Matrix4x4.TRS(instance.transform.position, instance.transform.rotation, Vector3.one);

                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                using (new Handles.DrawingScope(containerColor, localToWorld))
                {
                    if (editMode == EditMode.Container)
                    {
                        Handles.Label(Vector3.zero, "Container Area", containerText);

                        boxBoundsHandleContainer.center = Vector3.zero;
                        boxBoundsHandleContainer.size = instance.containerSize;

                        EditorGUI.BeginChangeCheck();
                        boxBoundsHandleContainer.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            // record the target object before setting new values so changes can be undone/redone
                            Undo.RecordObject(instance, "Change Container");

                            instance.containerSize = boxBoundsHandleContainer.size;
                            instance.OnValidate();
                            EditorUtility.SetDirty(instance);
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (ZibraSmokeAndFireInstances == null || ZibraSmokeAndFireInstances.Length == 0)
            {
                Debug.LogError("ZibraSmokeAndFireEditor not attached to ZibraSmokeAndFire component.");
                return;
            }

            serializedObject.Update();

#if ZIBRA_SMOKE_AND_FIRE_DEBUG
            EditorGUILayout.HelpBox("DEBUG VERSION", MessageType.Info);
            var currentLogLevel =
                (ZibraSmokeAndFireBridge.LogLevel)EditorGUILayout.EnumPopup("Log level:", ZibraSmokeAndFireDebug.CurrentLogLevel);
            if (currentLogLevel != ZibraSmokeAndFireDebug.CurrentLogLevel)
            {
                ZibraSmokeAndFireDebug.SetLogLevel(currentLogLevel);
            }
#endif

            if (RenderPipelineDetector.IsURPMissingRenderComponent())
            {
                EditorGUILayout.HelpBox(
                    "URP Smoke And Fire Rendering Component is not added. Smoke And Fire will not be rendered, but will still be simulated.",
                    MessageType.Error);
            }

            if (RenderPipelineDetector.IsURPMissingDepthBuffer())
            {
                EditorGUILayout.HelpBox(
                    "Depth buffer is not enabled in URP options. Smoke And Fire will not be rendered properly.",
                    MessageType.Error);
            }

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.URP)
            {
                EditorGUILayout.HelpBox(
                    "On URP simulation will only be rendered in Game view, and will not be rendered in Scene view.",
                    MessageType.Info);
            }

            bool instanceCanSpawn = true;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                bool haveEmitter = instance.HasEmitter();
                if (!haveEmitter)
                {
                    instanceCanSpawn = false;
                    break;
                }
            }
            if (!instanceCanSpawn)
            {
                EditorGUILayout.HelpBox(
                    "No emitters or initial state added" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " No smoke or fire can spawn under these conditions.",
                    MessageType.Error);
            }

            bool lightMissing = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.mainLight == null)
                {
                    lightMissing = true;
                    break;
                }
            }
            if (lightMissing)
            {
                EditorGUILayout.HelpBox(
                    "Primary light is not set" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " Simulation will not start.",
                    MessageType.Error);
            }

            bool gridTooBig = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                instance.UpdateGridSize();
                Vector3Int gridSize = instance.GridSize;
                int nodesCount = gridSize[0] * gridSize[1] * gridSize[2];
                if (nodesCount > GRID_NODE_COUNT_WARNING_THRESHOLD)
                {
                    gridTooBig = true;
                    break;
                }
            }
            if (gridTooBig)
            {
                EditorGUILayout.HelpBox(
                    "Grid resolution is too high" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " High-end hardware is strongly recommended.",
                    MessageType.Info);
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (GUILayout.Button(EditorGUIUtility.IconContent("EditCollider"), GUILayout.MaxWidth(40),
                                 GUILayout.Height(30)))
            {
                editMode = editMode == EditMode.Container ? EditMode.None : EditMode.Container;
                SceneView.RepaintAll();
            }

            bool anyInstanceActivated = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.initialized)
                {
                    anyInstanceActivated = true;
                    break;
                }
            }

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Edit Container Area", containerText, GUILayout.MaxWidth(100),
                                       GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.PropertyField(ContainerSize);

            GUILayout.Space(15);

            EditorGUILayout.PropertyField(manipulators, true);

#if ZIBRA_SMOKE_AND_FIRE_FREE_VERSION
            bool reachedManipulatorLimits = false;

            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.GetManipulatorList().Count >= 1)
                {
                    reachedManipulatorLimits = true;
                    break;
                }
            }
#else
            const bool reachedManipulatorLimits = false;
#endif

            if (!reachedManipulatorLimits)
            {
                GUIContent manipBtnTxt = new GUIContent("Add Manipulator");
                var manipBtn = GUILayoutUtility.GetRect(manipBtnTxt, GUI.skin.button);
                manipBtn.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, manipBtn.center.y);

                if (EditorGUI.DropdownButton(manipBtn, manipBtnTxt, FocusType.Keyboard))
                {
                    manipulatorDropdownToggle = !manipulatorDropdownToggle;
                }

                if (manipulatorDropdownToggle)
                {
                    EditorGUI.indentLevel++;

                    var empty = true;
                    foreach (var manipulator in Manipulator.AllManipulators)
                    {
                        bool presentInAllInstances = true;

                        foreach (var instance in ZibraSmokeAndFireInstances)
                        {
                            if (!instance.HasManipulator(manipulator))
                            {
                                presentInAllInstances = false;
                                break;
                            }
                        }

                        if (presentInAllInstances)
                        {
                            continue;
                        }

                        empty = false;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(manipulator, typeof(Manipulator), false);
                        EditorGUI.EndDisabledGroup();
                        if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                        {
                            foreach (var instance in ZibraSmokeAndFireInstances)
                            {
                                instance.AddManipulator(manipulator);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (empty)
                    {

                        GUIContent labelText = new GUIContent("The list is empty.");
                        var rtLabel = GUILayoutUtility.GetRect(labelText, GUI.skin.label);
                        rtLabel.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rtLabel.center.y);

                        EditorGUI.LabelField(rtLabel, labelText);
                    }
                    else
                    {
                        // No point in "Add all" button on free version since you can only have 1 manipulator anyway
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
                        if (GUILayout.Button("Add all"))
                        {
                            foreach (var instance in ZibraSmokeAndFireInstances)
                            {
                                foreach (var manipulator in Manipulator.AllManipulators)
                                {
                                    instance.AddManipulator(manipulator);
                                }
                            }
                        }
#endif
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(TimeStep, new GUIContent("Simulation Timestep"));
            EditorGUILayout.PropertyField(SimulationIterations);

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);

            EditorGUILayout.PropertyField(CurrentSimulationMode, new GUIContent("Simulation Mode"));
            EditorGUILayout.PropertyField(GridResolution);

            EditorGUI.EndDisabledGroup();

            ZibraSmokeAndFireInstances[0].UpdateGridSize();
            Vector3Int solverRes = ZibraSmokeAndFireInstances[0].GridSize;
            float cellSize = ZibraSmokeAndFireInstances[0].CellSize;
            bool[] sameDimensions = new bool[3];
            bool sameCellSize = true;
            sameDimensions[0] = true;
            sameDimensions[1] = true;
            sameDimensions[2] = true;
            bool anyInstanceHasFixedFramerate = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                instance.UpdateGridSize();
                var currentSolverRes = instance.GridSize;
                for (int i = 0; i < 3; i++)
                {
                    if (solverRes[i] != currentSolverRes[i])
                        sameDimensions[i] = false;
                }
                if (cellSize != instance.CellSize)
                {
                    sameCellSize = false;
                }

                if (instance.limitFramerate)
                {
                    anyInstanceHasFixedFramerate = true;
                    break;
                }
            }
            string effectiveResolutionText =
                $"({(sameDimensions[0] ? solverRes[0].ToString() : "-")}, {(sameDimensions[1] ? solverRes[1].ToString() : "-")}, {(sameDimensions[2] ? solverRes[2].ToString() : "-")})";
            string effectiveVoxelCountText =
                $"{(float)solverRes[0] * solverRes[1] * solverRes[2] / 1000000.0f:0.##}M Voxels";
            GUILayout.Label("Effective grid resolution: " + effectiveResolutionText);
            GUILayout.Label("Effective voxel count: " + effectiveVoxelCountText);

            string cellSizeText = $"{(sameCellSize ? cellSize.ToString() : "-")}";
            GUILayout.Label("Cell size:   " + cellSizeText);

            EditorGUILayout.PropertyField(RunSimulation);
            EditorGUILayout.PropertyField(RunRendering);
            EditorGUILayout.PropertyField(limitFramerate);

            if (anyInstanceHasFixedFramerate)
                EditorGUILayout.PropertyField(maximumFramerate);

            EditorGUILayout.PropertyField(fixVolumeWorldPosition);

            EditorGUILayout.PropertyField(EnableDownscale, new GUIContent("Enable Render Downscale"));
            if (EnableDownscale.boolValue)
            {
                EditorGUILayout.PropertyField(DownscaleFactor);
            }


            EditorGUI.BeginDisabledGroup(anyInstanceActivated);
            EditorGUI.BeginChangeCheck();
            bool needUpdateUnityRender = EditorGUI.EndChangeCheck();

            EditorGUI.EndDisabledGroup();

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
                // Since it's only used in SRP, hide it in case of other render pipelines
                EditorGUILayout.PropertyField(CurrentInjectionPoint);
            }

            EditorGUILayout.PropertyField(mainLight, new GUIContent("Primary Light"));
            EditorGUILayout.PropertyField(lights, new GUIContent("Additional Lights"));

            GUILayout.Space(10);


            serializedObject.ApplyModifiedProperties();


            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUILayout.Label("Current render pipeline: BRP");
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUILayout.Label("Current render pipeline: URP");
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUILayout.Label("Current render pipeline: HDRP");
                    break;
            }

            GUILayout.Space(15);

            if (anyInstanceActivated)
            {
                GUIContent statsButtonText = new GUIContent("Simulation statistics");
                var statsButton = GUILayoutUtility.GetRect(statsButtonText, GUI.skin.button);
                statsButton.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, statsButton.center.y);

                if (EditorGUI.DropdownButton(statsButton, statsButtonText, FocusType.Keyboard))
                {
                    statsDropdownToggle = !statsDropdownToggle;
                }
                if (statsDropdownToggle)
                {
                    if (ZibraSmokeAndFireInstances.Length > 1)
                    {
                        GUILayout.Label(
                            "Selected multiple smoke & fire instances. Please select exactly one instance to view statistics.");
                    }
                    else
                    {
                        GUILayout.Label("Current time step: " + ZibraSmokeAndFireInstances[0].timestep);
                        GUILayout.Label("Internal time: " + ZibraSmokeAndFireInstances[0].simulationInternalTime);
                        GUILayout.Label("Simulation frame: " + ZibraSmokeAndFireInstances[0].simulationInternalFrame);
                    }
                }
            }
        }
    }
}
