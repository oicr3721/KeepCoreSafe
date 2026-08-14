using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class FixedEnemyPathValidation
    {
        private const string MeleeDataPath =
            "Assets/Resources/Data/Enemy/MeleeEnemyData.asset";
        private const string CoreDataPath =
            "Assets/Resources/Data/Block/CoreData.asset";
        private const string BlockDataPath =
            "Assets/Resources/Data/Block/Basic/RedBasic.asset";

        [MenuItem("Keep Core Safe/Validate Fixed Enemy Paths")]
        public static void Validate()
        {
            ValidateEmptyGrid();
            ValidateShortestPlusTwoSelection();
            ValidateOutOfRangeRouteExclusion();
            ValidateRandomTieSelection();
            ValidateNoRuntimeRepathHooks();
            ValidateConfiguredAssets();
            Debug.Log("FIXED_ENEMY_PATH_VALIDATION_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Ranged Enemy Outside Entry")]
        public static void ValidateRangedEnemyOutsideEntry()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
            GridManager manager = UnityEngine.Object.FindFirstObjectByType<GridManager>(
                FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException("GameScene has no GridManager.");

            if (manager.Grid == null)
            {
                typeof(GridManager).GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(manager, null);
            }

            RangedEnemyData data = AssetDatabase.LoadAssetAtPath<RangedEnemyData>(
                "Assets/Resources/Data/Enemy/RangedEnemyData.asset");
            if (data == null || data.Prefab == null)
                throw new InvalidOperationException("Ranged enemy data is incomplete.");

            Vector2Int entryCell = new(0, manager.Height / 2);
            Vector3 outsidePosition = manager.GridToWorld(entryCell)
                + Vector3.left * manager.CellSize * 2f;
            RangedEnemy ranged = UnityEngine.Object.Instantiate(
                    data.Prefab,
                    outsidePosition,
                    Quaternion.identity)
                as RangedEnemy;
            if (ranged == null)
                throw new InvalidOperationException("Ranged enemy prefab has the wrong type.");

            try
            {
                PropertyInfo bodyProperty = typeof(Enemy).GetProperty(
                    "Body",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (bodyProperty?.GetValue(ranged) == null)
                {
                    typeof(Enemy).GetMethod(
                            "Awake",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.Invoke(ranged, null);
                }

                ranged.Initialize(data, new[] { entryCell });
                typeof(RangedEnemy).GetMethod(
                        "Start",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(ranged, null);
                typeof(RangedEnemy).GetMethod(
                        "OnCombatUpdate",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(ranged, new object[] { 0.02f });

                bool isMoving = (bool)typeof(Enemy).GetField(
                        "isMovingToCell",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ranged);
                Vector2Int destination = (Vector2Int)typeof(Enemy).GetField(
                        "movementDestination",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ranged);
                if (!isMoving || destination != entryCell || !manager.Grid.IsWithinBounds(destination))
                {
                    throw new InvalidOperationException(
                        "Ranged enemy did not enter through its first in-bounds path cell.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ranged.gameObject);
            }

            Debug.Log("RANGED_ENEMY_OUTSIDE_ENTRY_VALIDATION_COMPLETE");
        }

        private static void ValidateEmptyGrid()
        {
            using TestGrid test = new(7, 7, 2);
            GridPathfinder.PathResult path = test.BuildPath(new Vector2Int(0, 3));
            if (path.SelectedDistance != path.ShortestDistance
                || path.BlockingBlockCount != 0)
            {
                throw new InvalidOperationException("Empty-grid path selection failed.");
            }
        }

        private static void ValidateShortestPlusTwoSelection()
        {
            using TestGrid test = new(7, 5, 2);
            test.PlaceBlocks(
                new Vector2Int(1, 2),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2));

            GridPathfinder.PathResult path = test.BuildPath(new Vector2Int(0, 2));
            if (path.ShortestDistance != 3
                || path.SelectedDistance != 5
                || path.BlockingBlockCount != 0)
            {
                throw new InvalidOperationException(
                    "Shortest+2 candidate did not prefer the block-free route.");
            }
        }

        private static void ValidateOutOfRangeRouteExclusion()
        {
            using TestGrid test = new(7, 5, 2);
            test.PlaceBlocks(
                new Vector2Int(1, 2),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(3, 1),
                new Vector2Int(4, 1),
                new Vector2Int(1, 3),
                new Vector2Int(2, 3),
                new Vector2Int(3, 3),
                new Vector2Int(4, 3));

            GridPathfinder.PathResult path = test.BuildPath(new Vector2Int(0, 2));
            if (path.SelectedDistance > path.ShortestDistance + 2
                || path.BlockingBlockCount == 0)
            {
                throw new InvalidOperationException(
                    "A block-free route outside shortest+2 was incorrectly selected.");
            }
        }

        private static void ValidateRandomTieSelection()
        {
            HashSet<string> selectedPaths = new();
            for (int seed = 1; seed <= 12; seed++)
            {
                using TestGrid test = new(7, 7, 0, seed);
                GridPathfinder.PathResult path = test.BuildPath(new Vector2Int(0, 0));
                selectedPaths.Add(string.Join(";", path.Cells));
            }

            if (selectedPaths.Count < 2)
                throw new InvalidOperationException("Equal path candidates are not randomized.");
        }

        private static void ValidateNoRuntimeRepathHooks()
        {
            string enemySource = File.ReadAllText("Assets/Scripts/Enemy/Enemy.cs");
            string meleeSource = File.ReadAllText("Assets/Scripts/Enemy/MeleeEnemy.cs");
            string rangedSource = File.ReadAllText("Assets/Scripts/Enemy/RangedEnemy.cs");
            string waveSource = File.ReadAllText("Assets/Scripts/Managers/WaveManager.cs");
            string combined = enemySource + meleeSource + rangedSource;
            if (combined.Contains("GridChanged", StringComparison.Ordinal)
                || combined.Contains("RebuildPlan", StringComparison.Ordinal)
                || combined.Contains("RepathInterval", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A runtime path-recalculation hook remains.");
            }

            if (!waveSource.Contains("CreateSpawnPlan()", StringComparison.Ordinal)
                || !waveSource.Contains("enemy.Initialize(data, pathCells, routeTarget)", StringComparison.Ordinal)
                || !rangedSource.Contains("EnterGridFromSpawn()", StringComparison.Ordinal)
                || !rangedSource.Contains("TryBeginCellMovement(entryCell)", StringComparison.Ordinal)
                || !meleeSource.Contains("HandleRouteGoalDied", StringComparison.Ordinal)
                || !rangedSource.Contains("HandleRouteGoalDied", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Enemy routes are not fully captured by WaveManager at wave start.");
            }
        }

        private static void ValidateConfiguredAssets()
        {
            string[] dataPaths =
            {
                MeleeDataPath,
                "Assets/Resources/Data/Enemy/RangedEnemyData.asset"
            };
            foreach (string path in dataPaths)
            {
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (data == null || data.PathLengthTolerance < 0 || data.Prefab == null)
                    throw new InvalidOperationException($"Enemy navigation data is incomplete: {path}");
            }

            GameObject meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/MeleeEnemy.prefab");
            GameObject rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/RangedEnemy.prefab");
            if (meleePrefab == null
                || meleePrefab.GetComponent<MeleeEnemy>() == null
                || rangedPrefab == null
                || rangedPrefab.GetComponent<RangedEnemy>() == null)
            {
                throw new InvalidOperationException("General enemy Prefab references are incomplete.");
            }
        }

        private sealed class TestGrid : IDisposable
        {
            private readonly List<GameObject> objects = new();
            private readonly EnemyData enemyData;
            private readonly int seed;
            private readonly GridManager manager;
            private readonly Block core;

            public TestGrid(int width, int height, int tolerance, int seed = 1001)
            {
                this.seed = seed;
                enemyData = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<EnemyData>(MeleeDataPath));
                SerializedObject enemySerialized = new(enemyData);
                enemySerialized.FindProperty("pathLengthTolerance").intValue = tolerance;
                enemySerialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject managerObject = CreateObject("Path Validation Grid");
                manager = managerObject.AddComponent<GridManager>();
                SerializedObject gridSerialized = new(manager);
                gridSerialized.FindProperty("width").intValue = width;
                gridSerialized.FindProperty("height").intValue = height;
                gridSerialized.ApplyModifiedPropertiesWithoutUndo();
                typeof(GridManager).GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(manager, null);

                core = CreateBlock(
                    AssetDatabase.LoadAssetAtPath<BlockData>(CoreDataPath),
                    new Vector2Int(4, 2));
            }

            public void PlaceBlocks(params Vector2Int[] positions)
            {
                BlockData blockData = AssetDatabase.LoadAssetAtPath<BlockData>(BlockDataPath);
                foreach (Vector2Int position in positions)
                    CreateBlock(blockData, position);
            }

            public GridPathfinder.PathResult BuildPath(Vector2Int start)
            {
                GridPathfinder pathfinder = new(manager, enemyData, seed);
                if (!pathfinder.TryBuildPath(start, core, out GridPathfinder.PathResult result))
                    throw new InvalidOperationException("Pathfinder returned no path.");
                return result;
            }

            private Block CreateBlock(BlockData data, Vector2Int position)
            {
                GameObject blockObject = CreateObject($"Validation Block {position}");
                Block block = blockObject.AddComponent<WallBlock>();
                SerializedObject serialized = new(block);
                serialized.FindProperty("data").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!manager.TryPlaceBlock(block, position))
                    throw new InvalidOperationException($"Could not place validation block at {position}.");
                return block;
            }

            private GameObject CreateObject(string name)
            {
                GameObject created = new(name);
                objects.Add(created);
                return created;
            }

            public void Dispose()
            {
                for (int i = objects.Count - 1; i >= 0; i--)
                {
                    if (objects[i] != null)
                        UnityEngine.Object.DestroyImmediate(objects[i]);
                }

                UnityEngine.Object.DestroyImmediate(enemyData);
            }
        }
    }
}
