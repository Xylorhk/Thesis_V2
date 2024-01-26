#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION

using com.zibraai.smoke_and_fire.Manipulators;
using com.zibraai.smoke_and_fire.SDFObjects;
using UnityEditor;

namespace com.zibraai.smoke_and_fire.Editor.SDFObjects
{
    [CustomEditor(typeof(ZibraSmokeAndFireCollider))]
    [CanEditMultipleObjects]
    public class ColliderEditor : UnityEditor.Editor
    {
        static ColliderEditor EditorInstance;

        private ZibraSmokeAndFireCollider[] Colliders;

        SerializedProperty FluidFriction;

        protected void Awake()
        {
            ZibraServerAuthenticationManager.GetInstance().Initialize();
        }

        protected void OnEnable()
        {
            EditorInstance = this;

            Colliders = new ZibraSmokeAndFireCollider[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                Colliders[i] = targets[i] as ZibraSmokeAndFireCollider;
            }

            serializedObject.Update();
            FluidFriction = serializedObject.FindProperty("FluidFriction");
            serializedObject.ApplyModifiedProperties();
        }

        protected void OnDisable()
        {
            if (EditorInstance == this)
            {
                EditorInstance = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool hasSDF = false;
            foreach (var instance in Colliders)
            {
                if (instance.GetComponent<SDFObject>() != null)
                {
                    hasSDF = true;
                    break;
                }
            }

            if (!hasSDF)
            {
                EditorGUILayout.HelpBox(
                    "You need to add a SDFObject component(AnalyticalSDF/NeuralSDF/SkinnedMeshSDF) to define the shape of the collider." +
                    "The default is currently set to a sphere.",
                    MessageType.Info);
            }


            EditorGUILayout.PropertyField(FluidFriction);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
