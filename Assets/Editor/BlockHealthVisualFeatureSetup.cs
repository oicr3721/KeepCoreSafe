using System;
using System.Collections.Generic;
using System.Linq;
using KeepCoreSafe.Data;
using KeepCoreSafe.UI;
using UnityEditor;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class BlockHealthVisualFeatureSetup
    {
        private const int HealthStageCount = 5;
        private const string BlockDataFolder = "Assets/Resources/Data/Block";
        private const string BlockSpriteSheetPath = "Assets/Resources/Sprites/Blocks-Sheet.png";
        private const string HealthBarPrefabPath = "Assets/Prefabs/UI/BlockHealthBar.prefab";

        [MenuItem("Keep Core Safe/Setup Block Health Visuals")]
        public static void Setup()
        {
            Sprite[] basicHealthSprites = LoadBasicHealthSprites();
            foreach (BlockData blockData in LoadAllBlockData())
            {
                SerializedObject serialized = new(blockData);
                SerializedProperty baseSprite = serialized.FindProperty("sprite");
                SerializedProperty stages = serialized.FindProperty("healthStageSprites");
                stages.arraySize = HealthStageCount;

                if (blockData is BasicBlockData || blockData is WallBlockData)
                {
                    baseSprite.objectReferenceValue = basicHealthSprites[0];
                    for (int i = 0; i < HealthStageCount; i++)
                        stages.GetArrayElementAtIndex(i).objectReferenceValue = basicHealthSprites[i];
                }
                else
                {
                    Sprite existingSprite = baseSprite.objectReferenceValue as Sprite;
                    if (existingSprite == null)
                        throw new InvalidOperationException($"{blockData.name} has no base Sprite.");

                    for (int i = 0; i < HealthStageCount; i++)
                        stages.GetArrayElementAtIndex(i).objectReferenceValue = existingSprite;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(blockData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BLOCK_HEALTH_VISUAL_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Block Health Visuals")]
        public static void Validate()
        {
            Sprite[] basicHealthSprites = LoadBasicHealthSprites();
            foreach (BlockData blockData in LoadAllBlockData())
            {
                SerializedObject serialized = new(blockData);
                SerializedProperty baseSprite = serialized.FindProperty("sprite");
                SerializedProperty stages = serialized.FindProperty("healthStageSprites");
                if (stages.arraySize != HealthStageCount)
                    throw new InvalidOperationException($"{blockData.name} must define five health-stage Sprites.");

                bool usesBasicStages = blockData is BasicBlockData || blockData is WallBlockData;
                Sprite expectedBase = usesBasicStages
                    ? basicHealthSprites[0]
                    : baseSprite.objectReferenceValue as Sprite;
                if (expectedBase == null || baseSprite.objectReferenceValue != expectedBase)
                    throw new InvalidOperationException($"{blockData.name} has an invalid base Sprite.");

                for (int i = 0; i < HealthStageCount; i++)
                {
                    Sprite expected = usesBasicStages ? basicHealthSprites[i] : expectedBase;
                    if (stages.GetArrayElementAtIndex(i).objectReferenceValue != expected)
                    {
                        throw new InvalidOperationException(
                            $"{blockData.name} has an invalid health Sprite at stage {i}.");
                    }
                }

                float[] sampleRatios = { 1f, 0.79f, 0.59f, 0.39f, 0.19f };
                for (int i = 0; i < sampleRatios.Length; i++)
                {
                    Sprite expected = usesBasicStages ? basicHealthSprites[i] : expectedBase;
                    if (blockData.GetHealthSprite(sampleRatios[i]) != expected)
                    {
                        throw new InvalidOperationException(
                            $"{blockData.name} resolves the wrong Sprite for health stage {i}.");
                    }
                }
            }

            GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            BlockHealthBar healthBar = healthBarPrefab != null
                ? healthBarPrefab.GetComponent<BlockHealthBar>()
                : null;
            if (healthBar == null)
                throw new InvalidOperationException("BlockHealthBar prefab is missing its view component.");

            SerializedObject healthBarData = new(healthBar);
            float warningThreshold = healthBarData.FindProperty("warningThreshold").floatValue;
            float criticalThreshold = healthBarData.FindProperty("criticalThreshold").floatValue;
            Color healthyColor = healthBarData.FindProperty("healthyColor").colorValue;
            Color warningColor = healthBarData.FindProperty("warningColor").colorValue;
            Color criticalColor = healthBarData.FindProperty("criticalColor").colorValue;
            if (criticalThreshold < 0f
                || criticalThreshold >= warningThreshold
                || warningThreshold > 1f
                || healthBarData.FindProperty("fill").objectReferenceValue == null
                || healthyColor.g <= healthyColor.r
                || warningColor.r < warningColor.g
                || warningColor.g <= warningColor.b
                || criticalColor.r <= criticalColor.g)
            {
                throw new InvalidOperationException("BlockHealthBar stepped-color configuration is invalid.");
            }

            Debug.Log("BLOCK_HEALTH_VISUAL_VALIDATION_COMPLETE");
        }

        private static Sprite[] LoadBasicHealthSprites()
        {
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(BlockSpriteSheetPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, sprite => sprite);
            Sprite[] ordered = new Sprite[HealthStageCount];
            for (int i = 0; i < HealthStageCount; i++)
            {
                string spriteName = $"Blocks-Sheet_{i}";
                if (!sprites.TryGetValue(spriteName, out ordered[i]))
                    throw new InvalidOperationException($"{spriteName} was not found in {BlockSpriteSheetPath}.");
            }

            return ordered;
        }

        private static IEnumerable<BlockData> LoadAllBlockData()
        {
            string[] guids = AssetDatabase.FindAssets("t:BlockData", new[] { BlockDataFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BlockData data = AssetDatabase.LoadAssetAtPath<BlockData>(path);
                if (data != null)
                    yield return data;
            }
        }
    }
}
