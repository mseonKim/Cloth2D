using UnityEngine;
using UnityEditor;

namespace Cloth2D.EditorUtility
{
    public struct Cloth2DChainSettings
    {
        public bool isCopied;
        public bool useFixedUpdate;
        public float anchorOffset;
        public int chainPoints;
        public float gravity;
        public float mass;
        public float elasticResponse;
        public int lastAnchor;
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Cloth2DChain))]
    public class Cloth2DChainEditor: Editor
    {
        public static Cloth2DChainSettings copiedSettings;
        SerializedProperty sprite;
        SerializedProperty useFixedUpdate;
        SerializedProperty anchorOffset;
        SerializedProperty chainPoints;
        SerializedProperty gravity;
        SerializedProperty mass;
        SerializedProperty elasticResponse;
        SerializedProperty lastAnchor;


        void OnEnable()
        {
            sprite = serializedObject.FindProperty("sprite");
            useFixedUpdate = serializedObject.FindProperty("useFixedUpdate");
            anchorOffset = serializedObject.FindProperty("anchorOffset");
            chainPoints = serializedObject.FindProperty("chainPoints");
            gravity = serializedObject.FindProperty("gravity");
            mass = serializedObject.FindProperty("mass");
            elasticResponse = serializedObject.FindProperty("elasticResponse");
            lastAnchor = serializedObject.FindProperty("lastAnchor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(sprite);
            EditorGUILayout.PropertyField(useFixedUpdate);
            EditorGUILayout.PropertyField(anchorOffset);
            EditorGUILayout.PropertyField(chainPoints);
            EditorGUILayout.PropertyField(gravity);
            EditorGUILayout.PropertyField(mass);
            EditorGUILayout.PropertyField(elasticResponse);
            EditorGUILayout.PropertyField(lastAnchor);

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
            copiedSettings.useFixedUpdate = useFixedUpdate.boolValue;
            copiedSettings.anchorOffset = anchorOffset.floatValue;
            copiedSettings.chainPoints = chainPoints.intValue;
            copiedSettings.gravity = gravity.floatValue;
            copiedSettings.mass = mass.floatValue;
            copiedSettings.elasticResponse = elasticResponse.floatValue;
            copiedSettings.lastAnchor = lastAnchor.intValue;
        }

        private void PasteSettings()
        {
            useFixedUpdate.boolValue = copiedSettings.useFixedUpdate;
            anchorOffset.floatValue = copiedSettings.anchorOffset;
            chainPoints.intValue = copiedSettings.chainPoints;
            gravity.floatValue = copiedSettings.gravity;
            mass.floatValue = copiedSettings.mass;
            elasticResponse.floatValue = copiedSettings.elasticResponse;
            lastAnchor.intValue = copiedSettings.lastAnchor;
        }
    }
}