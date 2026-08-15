#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Enemies;
using UnityEditor;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class GridGameplayPhysicsRemovalSetup
    {
        private static readonly string[] PrefabFolders =
        {
            "Assets/Prefabs/Blocks",
            "Assets/Prefabs/Enemies"
        };

        [MenuItem("Keep Core Safe/Setup/Remove Grid Gameplay Physics")]
        public static void Apply()
        {
            foreach (string path in FindGameplayPrefabPaths())
                RemovePhysicsComponents(path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("GRID_GAMEPLAY_PHYSICS_REMOVAL_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Grid Gameplay Uses No Physics")]
        public static void Validate()
        {
            foreach (string path in FindGameplayPrefabPaths())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                if (prefab.GetComponentInChildren<Rigidbody2D>(true) != null
                    || prefab.GetComponentInChildren<Collider2D>(true) != null)
                {
                    throw new InvalidOperationException(
                        $"Grid gameplay prefab still contains 2D physics: {path}");
                }
            }

            Debug.Log("GRID_GAMEPLAY_NO_PHYSICS_VALIDATION_COMPLETE");
        }

        private static IEnumerable<string> FindGameplayPrefabPaths()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null
                    && (prefab.GetComponentInChildren<Enemy>(true) != null
                        || prefab.GetComponentInChildren<Block>(true) != null))
                {
                    yield return path;
                }
            }
        }

        private static void RemovePhysicsComponents(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
                    UnityEngine.Object.DestroyImmediate(collider, true);
                foreach (Rigidbody2D body in root.GetComponentsInChildren<Rigidbody2D>(true))
                    UnityEngine.Object.DestroyImmediate(body, true);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
