#if UNITY_EDITOR
using System;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class TutorialRedGreenReferenceSwapSetup
    {
        private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
        private const string RedPath = "Assets/Resources/Data/Block/Basic/RedBasic.asset";
        private const string GreenPath = "Assets/Resources/Data/Block/Basic/GreenBasic.asset";
        private const string BluePath = "Assets/Resources/Data/Block/Basic/BlueBasic.asset";

        [MenuItem("Keep Core Safe/Setup/Tutorial Red Green Reference Swap")]
        public static void Apply()
        {
            BasicBlockData red = Load<BasicBlockData>(RedPath);
            BasicBlockData green = Load<BasicBlockData>(GreenPath);
            BasicBlockData blue = Load<BasicBlockData>(BluePath);
            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            BlockSupplyController supply = FindInScene<BlockSupplyController>(scene);
            PlacementController placement = FindInScene<PlacementController>(scene);
            TutorialDirector director = FindInScene<TutorialDirector>(scene);
            if (supply == null || placement == null || director == null)
                throw new InvalidOperationException("Tutorial block references cannot be configured.");

            ConfigureSupply(supply, green, red);
            ConfigureStartingBlocks(placement, green, red, blue);

            // These two fields remain color identities. TutorialDirector swaps where
            // each identity is used; the BlockData assets themselves are never edited.
            SerializedObject directorSerialized = new(director);
            directorSerialized.FindProperty("redBlock").objectReferenceValue = red;
            directorSerialized.FindProperty("greenBlock").objectReferenceValue = green;
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("TUTORIAL_RED_GREEN_REFERENCE_SWAP_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Tutorial Red Green Reference Swap")]
        public static void Validate()
        {
            BasicBlockData red = Load<BasicBlockData>(RedPath);
            BasicBlockData green = Load<BasicBlockData>(GreenPath);
            BasicBlockData blue = Load<BasicBlockData>(BluePath);
            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            BlockSupplyController supply = FindInScene<BlockSupplyController>(scene);
            PlacementController placement = FindInScene<PlacementController>(scene);
            TutorialDirector director = FindInScene<TutorialDirector>(scene);

            SerializedObject supplySerialized = new(supply);
            SerializedProperty supplyBlocks = supplySerialized.FindProperty("scriptedBlocks");
            if (supplyBlocks.arraySize != 3
                || supplyBlocks.GetArrayElementAtIndex(0).objectReferenceValue != green
                || supplyBlocks.GetArrayElementAtIndex(1).objectReferenceValue != green
                || supplyBlocks.GetArrayElementAtIndex(2).objectReferenceValue != red)
            {
                throw new InvalidOperationException("Tutorial scripted supply was not swapped to Green, Green, Red.");
            }

            SerializedObject placementSerialized = new(placement);
            SerializedProperty starting = placementSerialized.FindProperty("scriptedStartingBlocks");
            ValidateStartingBlock(starting, Vector2Int.up, green);
            ValidateStartingBlock(starting, Vector2Int.down, red);
            ValidateStartingBlock(starting, Vector2Int.left, blue);
            ValidateStartingBlock(starting, Vector2Int.right, red);
            BlockMatchData matchData = placementSerialized.FindProperty("matchData").objectReferenceValue
                as BlockMatchData;
            if (matchData == null
                || !matchData.TryGetRule(red.Color, out BlockMatchData.Rule redRule)
                || redRule.ResultBlock is not AttackBlockData
                || !matchData.TryGetRule(green.Color, out BlockMatchData.Rule greenRule)
                || greenRule.ResultBlock is not HealerBlockData)
            {
                throw new InvalidOperationException(
                    "Tutorial swapped references do not resolve to Red Attack and Green Healer matches.");
            }

            SerializedObject directorSerialized = new(director);
            if (directorSerialized.FindProperty("redBlock").objectReferenceValue != red
                || directorSerialized.FindProperty("greenBlock").objectReferenceValue != green)
            {
                throw new InvalidOperationException("TutorialDirector color identity references are invalid.");
            }

            Debug.Log("TUTORIAL_RED_GREEN_REFERENCE_SWAP_VALIDATION_COMPLETE");
        }

        private static void ConfigureSupply(
            BlockSupplyController supply,
            BasicBlockData green,
            BasicBlockData red)
        {
            SerializedObject serialized = new(supply);
            SerializedProperty blocks = serialized.FindProperty("scriptedBlocks");
            blocks.arraySize = 3;
            blocks.GetArrayElementAtIndex(0).objectReferenceValue = green;
            blocks.GetArrayElementAtIndex(1).objectReferenceValue = green;
            blocks.GetArrayElementAtIndex(2).objectReferenceValue = red;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStartingBlocks(
            PlacementController placement,
            BasicBlockData green,
            BasicBlockData red,
            BasicBlockData blue)
        {
            SerializedObject serialized = new(placement);
            SerializedProperty blocks = serialized.FindProperty("scriptedStartingBlocks");
            if (blocks.arraySize != 4)
                throw new InvalidOperationException("Tutorial starting-block layout must contain four entries.");

            SetStartingReference(blocks, Vector2Int.up, green);
            SetStartingReference(blocks, Vector2Int.down, red);
            SetStartingReference(blocks, Vector2Int.left, blue);
            SetStartingReference(blocks, Vector2Int.right, red);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStartingReference(
            SerializedProperty blocks,
            Vector2Int offset,
            BlockData data)
        {
            for (int i = 0; i < blocks.arraySize; i++)
            {
                SerializedProperty entry = blocks.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("offset").vector2IntValue != offset)
                    continue;

                entry.FindPropertyRelative("data").objectReferenceValue = data;
                return;
            }

            throw new InvalidOperationException($"Tutorial starting position {offset} is missing.");
        }

        private static void ValidateStartingBlock(
            SerializedProperty blocks,
            Vector2Int offset,
            BlockData expected)
        {
            for (int i = 0; i < blocks.arraySize; i++)
            {
                SerializedProperty entry = blocks.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("offset").vector2IntValue == offset
                    && entry.FindPropertyRelative("data").objectReferenceValue == expected)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Tutorial starting reference at {offset} is invalid.");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset is missing: {path}");
            return asset;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
#endif
