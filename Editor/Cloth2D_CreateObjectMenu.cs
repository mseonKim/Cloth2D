using UnityEditor;
using UnityEngine;

namespace Cloth2D.EditorUtility
{
    public static class Cloth2D_CreateObjectMenu
    {
        [MenuItem("GameObject/2D Object/2D Cloth", false)]
        static void CreateClothSprite(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("2D Cloth");

            go.AddComponent<MeshFilter>();

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            go.AddComponent<PolygonCollider2D>();
            go.AddComponent<ClothSprite>();

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
        static void CreateWind2D(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("2D Wind");
            go.AddComponent<Wind2D>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
