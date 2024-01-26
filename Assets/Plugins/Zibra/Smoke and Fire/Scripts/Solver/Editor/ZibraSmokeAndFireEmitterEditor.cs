using com.zibraai.smoke_and_fire.Manipulators;
using UnityEditor;

namespace com.zibraai.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireEmitter))]
    [CanEditMultipleObjects]
    public class ZibraSmokeAndFireEmitterEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraSmokeAndFireEmitter[] EmitterInstances;

        private SerializedProperty InitialVelocity;
        private SerializedProperty SmokeColor;
        private SerializedProperty SmokeDensity;
        private SerializedProperty EmitterTemperature;
        private SerializedProperty EmitterFuel;
        private SerializedProperty UseObjectVelocity;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(InitialVelocity);
            EditorGUILayout.PropertyField(SmokeColor);
            EditorGUILayout.PropertyField(SmokeDensity);
            EditorGUILayout.PropertyField(EmitterTemperature);
            EditorGUILayout.PropertyField(EmitterFuel);
            EditorGUILayout.PropertyField(UseObjectVelocity);

            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            EmitterInstances = new ZibraSmokeAndFireEmitter[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                EmitterInstances[i] = targets[i] as ZibraSmokeAndFireEmitter;
            }

            SmokeColor = serializedObject.FindProperty("SmokeColor");
            SmokeDensity = serializedObject.FindProperty("SmokeDensity");
            InitialVelocity = serializedObject.FindProperty("InitialVelocity");
            EmitterTemperature = serializedObject.FindProperty("EmitterTemperature");
            EmitterFuel = serializedObject.FindProperty("EmitterFuel");
            UseObjectVelocity = serializedObject.FindProperty("UseObjectVelocity");
        }
    }
}