using UnityEngine;
using UnityEditor;

namespace Cloth2D.EditorUtility
{
    public struct Cloth2DSettings
    {
        public bool isCopied;
        public Color color;
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
    [CustomEditor(typeof(Cloth2D))]
    public class Cloth2DEditor: Editor
    {
        public static Cloth2DSettings copiedSettings;
        SerializedProperty sprite;
        SerializedProperty color;
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
            color = serializedObject.FindProperty("color");
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
            EditorGUILayout.PropertyField(color);
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
            if (GUILayout.Button("Add Rigidbody"))
            {
                AddRigidbody();
            }
            GUIContent copyButtonContent = new GUIContent("Copy", "Copy all settings except sprite.");
            if (Selection.gameObjects.Length > 1)
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
            copiedSettings.color = color.colorValue;
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
            color.colorValue = copiedSettings.color;
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

        private void AddRigidbody()
        {
            foreach (var selectedObject in Selection.gameObjects)
            {
                selectedObject.GetComponent<Cloth2D>().useFixedUpdate = true;
                Rigidbody2D rigidbody = selectedObject.AddComponent<Rigidbody2D>();
                rigidbody.angularDrag = 0f;
                rigidbody.gravityScale = 0f;
                rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
                Undo.RegisterCreatedObjectUndo(rigidbody, "Create rigidbody " + rigidbody.name);
            }
        }
    }
}