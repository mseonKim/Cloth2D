﻿using UnityEditor;
using UnityEngine;

namespace Cloth2D.EditorUtility
{
    public static class Cloth2D_CreateObjectMenu
    {
        [MenuItem("GameObject/2D Object/Cloth Sprite", false)]
        static void CreateClothSprite(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("New Cloth Sprite");

            go.AddComponent<MeshFilter>();

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            go.AddComponent<PolygonCollider2D>();
            go.AddComponent<ClothSprite>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            // Create wind if not exists
            if (GameObject.FindObjectOfType<Wind2D>() == null)
            {
                go = new GameObject("2D Wind");
                go.AddComponent<Wind2D>();
                GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
                Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            }
        }
    }
}