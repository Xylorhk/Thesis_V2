#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
using com.zibraai.smoke_and_fire.Manipulators;
using UnityEditor;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireDetector))]
    [CanEditMultipleObjects]
    public class ZibraSmokeAndFireDetectorEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraSmokeAndFireDetector[] DetectorInstances;
        private SerializedProperty CurrentDetectorMode;
        private SerializedProperty ControlLight;
        private SerializedProperty RelativeBrightness;


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(CurrentDetectorMode);



            Vector3 illumination = Vector3.zero;

            bool IlluminationEdit = false;
            bool NotPointLight = false;
            foreach (var instance in DetectorInstances)
            {
                if (instance.CurrentDetectorMode == ZibraSmokeAndFireDetector.DetectorModes.Illumination)
                {
                    illumination += instance.CurrentIllumination;
                    IlluminationEdit = true;

                    if (instance.LightToControl != null && instance.LightToControl.type != LightType.Point)
                    {
                        NotPointLight = true;
                    }
                }
            }

            if (NotPointLight)
            {
                EditorGUILayout.HelpBox("Only a point light can be used", MessageType.Error);
            }

            if (DetectorInstances.Length > 1)
                GUILayout.Label("Multiple detectors selected. Showing sum of all selected instances.");

            if (IlluminationEdit)
            {
                EditorGUILayout.PropertyField(ControlLight);
                EditorGUILayout.PropertyField(RelativeBrightness);
                GUILayout.Label("Average brightness of smoke: " + illumination / DetectorInstances.Length);

                if (DetectorInstances.Length == 1)
                {
                    Vector3 center = DetectorInstances[0].CurrentIlluminationCenter;

                    GUILayout.Label("Relative center of illumination: " + center);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            DetectorInstances = new ZibraSmokeAndFireDetector[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                DetectorInstances[i] = targets[i] as ZibraSmokeAndFireDetector;
            }

            CurrentDetectorMode = serializedObject.FindProperty("CurrentDetectorMode");
            ControlLight = serializedObject.FindProperty("LightToControl");
            RelativeBrightness = serializedObject.FindProperty("RelativeBrightness");
        }
    }
}
#endif