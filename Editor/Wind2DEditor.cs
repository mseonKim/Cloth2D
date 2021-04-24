using UnityEditor;

namespace Cloth2D.EditorUtility
{
    [CustomEditor(typeof(Wind2D))]
    public class Wind2DEditor: Editor
    {
        SerializedProperty wind;
        SerializedProperty infiniteDistance;
        SerializedProperty attenuation;
        SerializedProperty maxDistance;
        SerializedProperty turbulence;


        void OnEnable()
        {
            wind = serializedObject.FindProperty("_wind");
            infiniteDistance = serializedObject.FindProperty("infiniteDistance");
            attenuation = serializedObject.FindProperty("attenuation");
            maxDistance = serializedObject.FindProperty("maxDistance");
            turbulence = serializedObject.FindProperty("_turbulence");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(wind);
            EditorGUILayout.PropertyField(infiniteDistance);
            using (new EditorGUI.DisabledGroupScope(infiniteDistance.boolValue))
            {
                EditorGUILayout.PropertyField(attenuation);
                EditorGUILayout.PropertyField(maxDistance);
            }
            EditorGUILayout.PropertyField(turbulence);

            serializedObject.ApplyModifiedProperties();
        }


    }

}