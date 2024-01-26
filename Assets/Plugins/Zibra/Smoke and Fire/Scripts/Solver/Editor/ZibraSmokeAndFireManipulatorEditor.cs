using com.zibraai.smoke_and_fire.Manipulators;
using UnityEditor;

namespace com.zibraai.smoke_and_fire.Editor.Solver
{
    public class ZibraSmokeAndFireManipulatorEditor : UnityEditor.Editor
    {
        protected SerializedProperty CurrentInteractionMode;
        protected SerializedProperty ParticleSpecies;

        protected void TriggerRepaint()
        {
            Repaint();
        }

        protected void OnEnable()
        {
            Manipulator manipulator = target as Manipulator;
            manipulator.onChanged += TriggerRepaint;

            ParticleSpecies = serializedObject.FindProperty("ParticleSpecies");
            CurrentInteractionMode = serializedObject.FindProperty("CurrentInteractionMode");
        }

        protected void OnDisable()
        {
            Manipulator manipulator = target as Manipulator;
            manipulator.onChanged -= TriggerRepaint;
        }
    }
}
