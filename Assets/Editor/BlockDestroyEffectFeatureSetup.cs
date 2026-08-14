using System;
using System.Collections.Generic;
using System.Linq;
using KeepCoreSafe.Data;
using KeepCoreSafe.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class BlockDestroyEffectFeatureSetup
    {
        private const int PoolSize = 12;
        private const string EffectPrefabPath = "Assets/Prefabs/Blocks/Block Destroy Effect.prefab";
        private const string PoolPrefabPath = "Assets/Prefabs/Presentation/Block Destroy Effect Pool.prefab";
        private const string PieceSheetPath = "Assets/Resources/Sprites/BlockPiece-Sheet.png";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        private static readonly Dictionary<string, string> BlockColorAssignments = new()
        {
            ["Assets/Resources/Data/Block/Basic/RedBasic.asset"] = "Assets/Resources/Data/Block/Colors/Red.asset",
            ["Assets/Resources/Data/Block/Basic/BlueBasic.asset"] = "Assets/Resources/Data/Block/Colors/Blue.asset",
            ["Assets/Resources/Data/Block/Basic/GreenBasic.asset"] = "Assets/Resources/Data/Block/Colors/Green.asset",
            ["Assets/Resources/Data/Block/Basic/YellowBasic.asset"] = "Assets/Resources/Data/Block/Colors/Yellow.asset",
            ["Assets/Resources/Data/Block/AttackData.asset"] = "Assets/Resources/Data/Block/Colors/Red.asset",
            ["Assets/Resources/Data/Block/SupportData.asset"] = "Assets/Resources/Data/Block/Colors/Blue.asset",
            ["Assets/Resources/Data/Block/HealerData.asset"] = "Assets/Resources/Data/Block/Colors/Green.asset",
            ["Assets/Resources/Data/Block/CoreData.asset"] = "Assets/Resources/Data/Block/Colors/Yellow.asset",
            ["Assets/Resources/Data/Block/TutorialCoreData.asset"] = "Assets/Resources/Data/Block/Colors/Yellow.asset",
            ["Assets/Resources/Data/Block/WallData.asset"] = "Assets/Resources/Data/Block/Colors/Yellow.asset"
        };

        [MenuItem("Keep Core Safe/Setup Block Destroy Effect")]
        public static void Setup()
        {
            GameObject effectPrefab = ConfigureEffectPrefab();
            GameObject poolPrefab = CreatePoolPrefab(effectPrefab);
            ConfigureBlockColors();
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath, poolPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BLOCK_DESTROY_EFFECT_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Block Destroy Effect")]
        public static void Validate()
        {
            GameObject effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EffectPrefabPath);
            BlockDestroyEffect effect = effectPrefab != null ? effectPrefab.GetComponent<BlockDestroyEffect>() : null;
            if (effect == null)
                throw new InvalidOperationException("Block Destroy Effect prefab has no playback component.");

            SerializedObject effectData = new(effect);
            SerializedProperty pieces = effectData.FindProperty("pieces");
            SerializedProperty sprites = effectData.FindProperty("pieceSprites");
            int rendererCount = effectPrefab.GetComponentsInChildren<SpriteRenderer>(true).Length;
            if (pieces.arraySize == 0
                || pieces.arraySize != rendererCount
                || sprites.arraySize != 4)
                throw new InvalidOperationException("Block Destroy Effect prefab references are incomplete.");
            for (int i = 0; i < pieces.arraySize; i++)
            {
                if (pieces.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    throw new InvalidOperationException($"Block Piece reference {i} is missing.");
            }

            GameObject poolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PoolPrefabPath);
            BlockDestroyEffectManager manager = poolPrefab != null
                ? poolPrefab.GetComponent<BlockDestroyEffectManager>()
                : null;
            if (manager == null)
                throw new InvalidOperationException("Block Destroy Effect Pool prefab is missing.");
            SerializedObject managerData = new(manager);
            if (managerData.FindProperty("effectPrefab").objectReferenceValue != effect
                && managerData.FindProperty("effectPrefab").objectReferenceValue != effectPrefab)
            {
                throw new InvalidOperationException("Block Destroy Effect Pool has no expansion prefab.");
            }

            SerializedProperty pool = managerData.FindProperty("effectPool");
            if (pool.arraySize != PoolSize)
                throw new InvalidOperationException("Block Destroy Effect Pool has the wrong size.");

            foreach (KeyValuePair<string, string> assignment in BlockColorAssignments)
            {
                BlockData blockData = AssetDatabase.LoadAssetAtPath<BlockData>(assignment.Key);
                BlockColorData expected = AssetDatabase.LoadAssetAtPath<BlockColorData>(assignment.Value);
                if (blockData == null || expected == null || blockData.Color != expected)
                    throw new InvalidOperationException($"Block color is not configured for {assignment.Key}.");
            }

            foreach (string scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                BlockDestroyEffectManager sceneManager =
                    UnityEngine.Object.FindFirstObjectByType<BlockDestroyEffectManager>(FindObjectsInactive.Include);
                if (sceneManager == null)
                    throw new InvalidOperationException($"{scenePath} has no Block Destroy Effect Pool.");
            }

            Debug.Log("BLOCK_DESTROY_EFFECT_VALIDATION_COMPLETE");
        }

        private static GameObject ConfigureEffectPrefab()
        {
            Sprite[] pieceSprites = AssetDatabase.LoadAllAssetsAtPath(PieceSheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            if (pieceSprites.Length != 4)
                throw new InvalidOperationException("BlockPiece-Sheet must contain exactly four Sprites.");

            GameObject root = PrefabUtility.LoadPrefabContents(EffectPrefabPath);
            try
            {
                BlockDestroyEffect effect = root.GetComponent<BlockDestroyEffect>();
                if (effect == null)
                    effect = root.AddComponent<BlockDestroyEffect>();

                SpriteRenderer[] pieces = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .OrderBy(piece => piece.transform.GetSiblingIndex())
                    .ToArray();
                if (pieces.Length == 0)
                    throw new InvalidOperationException("Block Destroy Effect prefab has no Block Pieces.");
                foreach (SpriteRenderer piece in pieces)
                    piece.sortingOrder = 20;

                SerializedObject serialized = new(effect);
                AssignArray(serialized.FindProperty("pieces"), pieces);
                AssignArray(serialized.FindProperty("pieceSprites"), pieceSprites);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, EffectPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(EffectPrefabPath);
        }

        private static GameObject CreatePoolPrefab(GameObject effectPrefab)
        {
            if (effectPrefab == null)
                throw new InvalidOperationException("Block Destroy Effect prefab is missing.");

            GameObject root = new("Block Destroy Effect Pool", typeof(BlockDestroyEffectManager));
            try
            {
                BlockDestroyEffect[] effects = new BlockDestroyEffect[PoolSize];
                for (int i = 0; i < PoolSize; i++)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(effectPrefab) as GameObject;
                    if (instance == null)
                        throw new InvalidOperationException("Could not create a pooled Block Destroy Effect instance.");
                    instance.name = $"Block Destroy Effect {i + 1:00}";
                    instance.transform.SetParent(root.transform, false);
                    effects[i] = instance.GetComponent<BlockDestroyEffect>();
                }

                SerializedObject manager = new(root.GetComponent<BlockDestroyEffectManager>());
                manager.FindProperty("effectPrefab").objectReferenceValue =
                    effectPrefab.GetComponent<BlockDestroyEffect>();
                AssignArray(manager.FindProperty("effectPool"), effects);
                manager.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PoolPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PoolPrefabPath);
        }

        private static void ConfigureBlockColors()
        {
            foreach (KeyValuePair<string, string> assignment in BlockColorAssignments)
            {
                BlockData blockData = AssetDatabase.LoadAssetAtPath<BlockData>(assignment.Key);
                BlockColorData colorData = AssetDatabase.LoadAssetAtPath<BlockColorData>(assignment.Value);
                if (blockData == null || colorData == null)
                    throw new InvalidOperationException($"Could not load color assignment for {assignment.Key}.");

                SerializedObject serialized = new(blockData);
                serialized.FindProperty("color").objectReferenceValue = colorData;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(blockData);
            }
        }

        private static void ConfigureScene(string scenePath, GameObject poolPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BlockDestroyEffectManager existing =
                UnityEngine.Object.FindFirstObjectByType<BlockDestroyEffectManager>(FindObjectsInactive.Include);
            if (existing == null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(poolPrefab, scene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException($"Could not add the effect pool to {scenePath}.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void AssignArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
