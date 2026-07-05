using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class BlockEffectVisualizerSetup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string EffectCellPrefabPath = "Assets/Prefabs/Presentation/EffectCell.prefab";
        private const string EffectCellSpritePath = "Assets/Sprites/WhiteSquare.png";

        [MenuItem("Keep Core Safe/Setup Pooled Block Effect Visualizer")]
        public static void Setup()
        {
            GameObject effectCellPrefab = CreateEffectCellPrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlacementVisualizer visualizer =
                Object.FindFirstObjectByType<PlacementVisualizer>(FindObjectsInactive.Include);
            if (visualizer == null)
                throw new System.InvalidOperationException("PlacementVisualizer was not found in GameScene.");

            ConfigureVisualizer(visualizer, effectCellPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("POOLED_BLOCK_EFFECT_VISUALIZER_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Pooled Block Effect Visualizer")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlacementVisualizer visualizer =
                Object.FindFirstObjectByType<PlacementVisualizer>(FindObjectsInactive.Include);
            if (visualizer == null
                || new SerializedObject(visualizer)
                    .FindProperty("effectCellPrefab").objectReferenceValue == null)
            {
                throw new System.InvalidOperationException("PlacementVisualizer Effect Cell prefab is missing.");
            }

            AssertOffsets(AdjacencyDirection.UpLeft, 2f,
                new Vector2Int(-1, 1), new Vector2Int(-2, 2));
            AssertOffsets(AdjacencyDirection.Cardinal, 2f,
                Vector2Int.up, Vector2Int.up * 2,
                Vector2Int.down, Vector2Int.down * 2,
                Vector2Int.left, Vector2Int.left * 2,
                Vector2Int.right, Vector2Int.right * 2);

            int everythingCount = 0;
            foreach (Vector2Int _ in GridEffectArea.EnumerateOffsets(
                         AdjacencyDirection.Everything, 2f))
            {
                everythingCount++;
            }
            if (everythingCount != 24)
                throw new System.InvalidOperationException($"Everything Range 2 expected 24 cells, got {everythingCount}.");

            Debug.Log("POOLED_BLOCK_EFFECT_VISUALIZER_VALIDATION_COMPLETE");
        }

        public static void ConfigureVisualizer(
            PlacementVisualizer visualizer,
            GameObject effectCellPrefab = null)
        {
            if (effectCellPrefab == null)
                effectCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EffectCellPrefabPath);
            EffectCellView cellView = effectCellPrefab != null
                ? effectCellPrefab.GetComponent<EffectCellView>()
                : null;
            if (cellView == null)
                throw new System.InvalidOperationException("EffectCell prefab is missing EffectCellView.");

            for (int i = visualizer.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(visualizer.transform.GetChild(i).gameObject);

            SerializedObject data = new(visualizer);
            data.FindProperty("effectCellPrefab").objectReferenceValue = cellView;
            data.FindProperty("initialPoolSize").intValue = 32;
            data.FindProperty("effectCellRoot").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateEffectCellPrefab()
        {
            GameObject root = new("EffectCell", typeof(SpriteRenderer), typeof(EffectCellView));
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EffectCellSpritePath);
            renderer.color = new Color(0.2f, 0.85f, 1f, 0.35f);
            renderer.sortingOrder = 48;

            SerializedObject data = new(root.GetComponent<EffectCellView>());
            data.FindProperty("cellRenderer").objectReferenceValue = renderer;
            data.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, EffectCellPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(EffectCellPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(EffectCellPrefabPath);
        }

        private static void AssertOffsets(
            AdjacencyDirection directions,
            float range,
            params Vector2Int[] expected)
        {
            HashSet<Vector2Int> actual = new(
                GridEffectArea.EnumerateOffsets(directions, range));
            if (!actual.SetEquals(expected))
            {
                throw new System.InvalidOperationException(
                    $"Unexpected offsets for {directions} Range {range}: {string.Join(", ", actual)}");
            }
        }
    }
}
