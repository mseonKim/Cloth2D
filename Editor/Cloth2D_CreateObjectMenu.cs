using UnityEditor;
using UnityEngine;

namespace Cloth2D.EditorUtility
{
    public static class Cloth2D_CreateObjectMenu
    {
        private static SerializedObject tagManager;

        [MenuItem("GameObject/2D Object/2D Cloth", false)]
        private static void CreateClothChain(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("2D Cloth");

            go.AddComponent<MeshFilter>();

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            go.AddComponent<PolygonCollider2D>();
            PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            collider.enabled = false;
            go.AddComponent<Cloth2D>();

            Prepare2DClothTag();
            go.tag = Cloth2D.clothTag;

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            // Create wind if not exists
            if (GameObject.FindObjectOfType<Wind2D>() == null)
            {
                CreateWind2D(menuCommand);
            }
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/2D Object/2D Wind", false)]
        private static void CreateWind2D(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Wind 2D");
            go.AddComponent<Wind2D>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/2D Object/2D ChainCloth", false)]
        private static void CreateCloth2DChain(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("2D ChainCloth");

            go.AddComponent<MeshFilter>();

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            go.AddComponent<Cloth2DChain>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            // Create wind if not exists
            if (GameObject.FindObjectOfType<Wind2D>() == null)
            {
                CreateWind2D(menuCommand);
            }
            Selection.activeObject = go;
        }


        private static void Prepare2DClothTag()
        {
            if (tagManager == null)
            {
                tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            }
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(Cloth2D.clothTag)) { return; }
            }

            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = Cloth2D.clothTag;
            tagManager.ApplyModifiedProperties();
            tagManager.Update();
        }
    }
}
