using UnityEngine;
using UnityEditor;

namespace Cloth2D.EditorUtility
{
    public struct ClothSpriteSettings
    {
        public bool isCopied;
        public bool flipTexture;
        public bool useFixedUpdate;
        public int resolution;
        public float gravity;
        public float mass;
        public float stiffness;
        public float wetness;
        public float drySpeed;
        public int mode;
        public float collisionResponse;
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ClothSprite))]
    public class ClothSpriteEditor: Editor
    {
        public static ClothSpriteSettings copiedSettings;
        SerializedProperty sprite;
        SerializedProperty flipTexture;
        SerializedProperty useFixedUpdate;
        SerializedProperty resolution;
        SerializedProperty gravity;
        SerializedProperty mass;
        SerializedProperty stiffness;
        SerializedProperty wetness;
        SerializedProperty drySpeed;
        SerializedProperty mode;
        SerializedProperty collisionResponse;
        private bool showAdditionalGroup;


        void OnEnable()
        {
            sprite = serializedObject.FindProperty("sprite");
            flipTexture = serializedObject.FindProperty("flipTexture");
            useFixedUpdate = serializedObject.FindProperty("useFixedUpdate");
            resolution = serializedObject.FindProperty("resolution");
            gravity = serializedObject.FindProperty("gravity");
            mass = serializedObject.FindProperty("mass");
            stiffness = serializedObject.FindProperty("stiffness");
            wetness = serializedObject.FindProperty("wetness");
            drySpeed = serializedObject.FindProperty("drySpeed");
            mode = serializedObject.FindProperty("mode");
            collisionResponse = serializedObject.FindProperty("collisionResponse");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(sprite);
            EditorGUILayout.PropertyField(mode);
            EditorGUILayout.PropertyField(useFixedUpdate);
            EditorGUILayout.PropertyField(resolution);
            EditorGUILayout.PropertyField(gravity);
            EditorGUILayout.PropertyField(mass);
            EditorGUILayout.PropertyField(stiffness);
            EditorGUILayout.PropertyField(collisionResponse);

            showAdditionalGroup = EditorGUILayout.Foldout(showAdditionalGroup, "Additional Settings");
            if (showAdditionalGroup)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(flipTexture);
                EditorGUILayout.PropertyField(wetness);
                EditorGUILayout.PropertyField(drySpeed);
                EditorGUI.indentLevel--;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIContent copyButtonContent = new GUIContent("Copy", "Copy all settings except sprite.");
            if (Selection.objects.Length > 1)
            {
                GUI.enabled = false;
                copyButtonContent.tooltip = "Can't copy multiple settings.";
            }
            if (GUILayout.Button(copyButtonContent))
            {
                CopySettings();
            }
            GUI.enabled = true;
            if (GUILayout.Button(new GUIContent("Paste", "Paste all settings except sprite.")))
            {
                if (copiedSettings.isCopied)
                {
                    PasteSettings();
                }
            }
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void CopySettings()
        {
            copiedSettings.isCopied = true;
            copiedSettings.flipTexture = flipTexture.boolValue;
            copiedSettings.useFixedUpdate = useFixedUpdate.boolValue;
            copiedSettings.resolution = resolution.intValue;
            copiedSettings.gravity = gravity.floatValue;
            copiedSettings.mass = mass.floatValue;
            copiedSettings.stiffness = stiffness.floatValue;
            copiedSettings.wetness = wetness.floatValue;
            copiedSettings.drySpeed = drySpeed.floatValue;
            copiedSettings.mode = mode.enumValueIndex;
            copiedSettings.collisionResponse = collisionResponse.floatValue;
        }

        private void PasteSettings()
        {
            flipTexture.boolValue = copiedSettings.flipTexture;
            useFixedUpdate.boolValue = copiedSettings.useFixedUpdate;
            resolution.intValue = copiedSettings.resolution;
            gravity.floatValue = copiedSettings.gravity;
            mass.floatValue = copiedSettings.mass;
            stiffness.floatValue = copiedSettings.stiffness;
            wetness.floatValue = copiedSettings.wetness;
            drySpeed.floatValue = copiedSettings.drySpeed;
            mode.enumValueIndex = copiedSettings.mode;
            collisionResponse.floatValue = copiedSettings.collisionResponse;
        }
    }
}