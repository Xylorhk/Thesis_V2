using com.zibraai.smoke_and_fire.SDFObjects;
using System;
using UnityEditor;
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Manipulators.Editors
{
    [CustomEditor(typeof(ZibraParticleEmitter))]
    public class ZibraParticlesEmitterEditor : UnityEditor.Editor
    {
        private ZibraParticleEmitter Emitter;

        private SerializedProperty EmitedParticlesPerFrame;
        private SerializedProperty RenderMode;
        private SerializedProperty ParticleSprite;
        private SerializedProperty SizeCurve;
        private SerializedProperty ParticleColor;
        private SerializedProperty ParticleMotionBlur;
        private SerializedProperty ParticleBrightness;
        private SerializedProperty ParticleColorOscillationAmount;
        private SerializedProperty ParticleColorOscillationFrequency;
        private SerializedProperty ParticleSizeOscillationAmount;
        private SerializedProperty ParticleSizeOscillationFrequency;

        private bool ShowColorOscillationOptions = false;
        private bool ShowSizeOscillationOptions = false;
        private void OnEnable()
        {
            Emitter = (ZibraParticleEmitter)target;
            EmitedParticlesPerFrame = serializedObject.FindProperty("EmitedParticlesPerFrame");
            RenderMode = serializedObject.FindProperty("RenderMode");
            ParticleSprite = serializedObject.FindProperty("ParticleSprite");
            SizeCurve = serializedObject.FindProperty("SizeCurve");
            ParticleColor = serializedObject.FindProperty("ParticleColor");
            ParticleMotionBlur = serializedObject.FindProperty("ParticleMotionBlur");
            ParticleBrightness = serializedObject.FindProperty("ParticleBrightness");
            ParticleColorOscillationAmount = serializedObject.FindProperty("ParticleColorOscillationAmount");
            ParticleColorOscillationFrequency = serializedObject.FindProperty("ParticleColorOscillationFrequency");
            ParticleSizeOscillationAmount = serializedObject.FindProperty("ParticleSizeOscillationAmount");
            ParticleSizeOscillationFrequency = serializedObject.FindProperty("ParticleSizeOscillationFrequency");
        }

        public override void OnInspectorGUI()
        {
            var collider = Emitter.GetComponentInParent<SDFObject>();
            if (collider == null)
            {
                EditorGUILayout.HelpBox("No SDF object found", MessageType.Error);
            }

            serializedObject.Update();
            EmitedParticlesPerFrame.floatValue = Mathf.Round(EmitedParticlesPerFrame.floatValue);
            EditorGUILayout.PropertyField(EmitedParticlesPerFrame, new GUIContent("Emited particles per frame"));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Render mode:");
            var renderModeNames = Enum.GetNames(typeof(ZibraParticleEmitter.RenderingMode));
            EditorGUI.BeginChangeCheck();
            RenderMode.enumValueIndex = GUILayout.SelectionGrid(RenderMode.enumValueIndex, renderModeNames, renderModeNames.Length, EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck())
            {
                Emitter.IsDirty = true;
            }
            GUILayout.EndHorizontal();

            if (RenderMode.enumValueIndex == ((int)ZibraParticleEmitter.RenderingMode.Default))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(ParticleColor, new GUIContent("Particles color"));
                if (EditorGUI.EndChangeCheck())
                {
                    Emitter.IsDirty = true;
                }
                EditorGUILayout.PropertyField(ParticleMotionBlur, new GUIContent("Motion Blur"));

                ShowColorOscillationOptions = EditorGUILayout.Foldout(ShowColorOscillationOptions, "Color oscillation");
                if (ShowColorOscillationOptions)
                {
                    EditorGUILayout.PropertyField(ParticleColorOscillationAmount, new GUIContent("Amount"));
                    EditorGUILayout.PropertyField(ParticleColorOscillationFrequency, new GUIContent("Frequency"));
                }
            }
            else if (RenderMode.enumValueIndex == ((int)ZibraParticleEmitter.RenderingMode.Sprite))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(ParticleSprite, new GUIContent("Sprite"));
                if (EditorGUI.EndChangeCheck())
                {
                    Emitter.IsDirty = true;
                }
            }
            EditorGUILayout.PropertyField(ParticleBrightness, new GUIContent("Brightness"));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(SizeCurve, new GUIContent("Size"));
            if (EditorGUI.EndChangeCheck())
            {
                Emitter.IsDirty = true;
            }

            ShowSizeOscillationOptions = EditorGUILayout.Foldout(ShowSizeOscillationOptions, "Size oscillation");
            if (ShowSizeOscillationOptions)
            {
                EditorGUILayout.PropertyField(ParticleSizeOscillationAmount, new GUIContent("Amount"));
                EditorGUILayout.PropertyField(ParticleSizeOscillationFrequency, new GUIContent("Frequency"));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}