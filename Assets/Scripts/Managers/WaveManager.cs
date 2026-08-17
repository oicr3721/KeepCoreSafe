using System;
using System.Collections;
using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Core;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using UnityEngine;

namespace KeepCoreSafe.Managers
{
    public sealed class WaveManager : MonoBehaviour
    {
        private readonly struct SpawnRequest
        {
            public SpawnRequest(
                EnemyData data,
                Vector3 position,
                IReadOnlyList<Vector2Int> pathCells,
                Block routeTarget)
            {
                Data = data;
                Position = position;
                PathCells = pathCells ?? Array.Empty<Vector2Int>();
                RouteTarget = routeTarget;
            }

            public EnemyData Data { get; }
            public Vector3 Position { get; }
            public IReadOnlyList<Vector2Int> PathCells { get; }
            public Block RouteTarget { get; }
        }

        private readonly struct PreparedSpawn
        {
            public PreparedSpawn(EnemyData data, Vector3 position, bool targetsSupply)
            {
                Data = data;
                Position = position;
                TargetsSupply = targetsSupply;
            }

            public EnemyData Data { get; }
            public Vector3 Position { get; }
            public bool TargetsSupply { get; }
        }

        [Header("Fallback")]
        [Tooltip("Used only when the selected WaveData has no valid weighted composition.")]
        [SerializeField] private EnemyData fallbackEnemyData;

        [Header("Audio")]
        [Tooltip("Played once when a combat wave begins.")]
        [SerializeField] private AudioCue waveStartSound = new();

        [Header("Next Wave Spawn Indicators")]
        [SerializeField] private GameObject spawnIndicatorPrefab;
        [SerializeField] private Transform spawnIndicatorRoot;
        [SerializeField, Min(0)] private int initialIndicatorPoolSize = 8;
        [SerializeField, Min(0.1f)] private float spawnIndicatorDiameter = 0.85f;
        [SerializeField, Min(0f)] private float spawnIndicatorHideDuration = 0.12f;

        [Header("Related Systems")]
        [SerializeField] private ShopEventController supplyEventController;

        private readonly HashSet<Enemy> activeEnemies = new();
        private Coroutine spawnRoutine;
        private Camera worldCamera;
        private bool isSpawning;
        private int currentEnemyCount;
        private int currentRangedEnemyCount;
        private int currentSuicideEnemyCount;
        private WaveData currentWaveData;
        private float currentSpawnInterval;
        private float currentSpawnMargin;
        private readonly List<PreparedSpawn> preparedSpawns = new();
        private readonly List<EnemySpawnIndicatorView> activeIndicators = new();
        private ComponentPool<EnemySpawnIndicatorView> indicatorPool;
        private int preparedWaveIndex = -1;

        public int ActiveEnemyCount => activeEnemies.Count;
        public IReadOnlyCollection<Enemy> ActiveEnemies => activeEnemies;
        public int CurrentWaveEnemyCount => currentEnemyCount;
        public int CurrentWaveRangedEnemyCount => currentRangedEnemyCount;
        public int CurrentWaveSuicideEnemyCount => currentSuicideEnemyCount;
        public WaveData CurrentWaveData => currentWaveData;
        public bool HasPreparedWave(int waveIndex) => preparedWaveIndex == waveIndex;

        public event Action WaveCompleted;

        private void Awake()
        {
            worldCamera = Camera.main;
            if (fallbackEnemyData == null)
                fallbackEnemyData = Resources.Load<EnemyData>("Data/Enemy/MeleeEnemyData");

            if (spawnIndicatorPrefab != null)
            {
                EnemySpawnIndicatorView indicatorView =
                    spawnIndicatorPrefab.GetComponent<EnemySpawnIndicatorView>();
                indicatorPool = new ComponentPool<EnemySpawnIndicatorView>(
                    indicatorView,
                    initialIndicatorPoolSize,
                    spawnIndicatorRoot != null ? spawnIndicatorRoot : transform);
            }
        }

        public void PrepareWave(int waveIndex, WaveDifficultySnapshot difficulty)
        {
            StopSpawnRoutine();
            ApplyDifficulty(waveIndex, ref difficulty);
            preparedSpawns.Clear();

            int regularEnemyCount = currentEnemyCount;
            List<EnemyData> spawnTypes = CreateSpawnTypes();
            for (int i = 0; i < regularEnemyCount; i++)
            {
                Vector3 position = GetAvailableSpawnPosition();
                preparedSpawns.Add(new PreparedSpawn(spawnTypes[i], position, false));
            }

            int supplyHunterCount = supplyEventController != null
                ? supplyEventController.GetSupplyHunterCount(regularEnemyCount)
                : 0;
            for (int i = 0; i < supplyHunterCount; i++)
            {
                EnemyData data = currentWaveData?.ChooseWeightedEnemy() ?? fallbackEnemyData;
                preparedSpawns.Add(new PreparedSpawn(data, GetAvailableSpawnPosition(), true));
                CountEnemyType(data);
            }

            currentEnemyCount = preparedSpawns.Count;

            preparedWaveIndex = waveIndex;
            ShowPreparedSpawnIndicators();
        }

        public void StartWave(int waveIndex, WaveDifficultySnapshot difficulty)
        {
            StopSpawnRoutine();
            if (!HasPreparedWave(waveIndex))
                PrepareWave(waveIndex, difficulty);

            HidePreparedSpawnIndicators();
            AudioManager.Play(waveStartSound);
            List<SpawnRequest> spawnPlan = CreateSpawnPlan();
            preparedWaveIndex = -1;
            spawnRoutine = StartCoroutine(SpawnWave(spawnPlan, spawnIndicatorHideDuration));
            Debug.Log(
                $"Wave {waveIndex}: {currentEnemyCount} enemies " +
                $"({currentRangedEnemyCount} ranged, {currentSuicideEnemyCount} suicide), " +
                $"composition {currentWaveData?.WaveName ?? "Fallback"}, " +
                $"special {difficulty.IsSpecialWave}, " +
                $"interval {currentSpawnInterval:0.00}s, " +
                $"shockwave target {difficulty.RequiredEnergy} energy.");
        }

        public void StopWave()
        {
            StopSpawnRoutine();
            foreach (Enemy enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.Died -= HandleEnemyDied;
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
        }

        public void StopSpawning()
        {
            StopSpawnRoutine();
        }

        private IEnumerator SpawnWave(
            IReadOnlyList<SpawnRequest> spawnPlan,
            float indicatorHideDelay)
        {
            isSpawning = true;
            if (indicatorHideDelay > 0f)
                yield return new WaitForSecondsRealtime(indicatorHideDelay);

            WaitForSeconds spawnDelay = new(currentSpawnInterval);
            for (int i = 0; i < spawnPlan.Count; i++)
            {
                SpawnEnemy(spawnPlan[i]);
                yield return spawnDelay;
            }

            isSpawning = false;
            spawnRoutine = null;
            CheckWaveCompleted();
        }

        private List<SpawnRequest> CreateSpawnPlan()
        {
            List<SpawnRequest> plan = new(preparedSpawns.Count);
            GridManager gridManager = GridManager.Instance;
            Block core = gridManager?.Grid?.Core;
            Block supplyTarget = supplyEventController?.ActiveSupplyBlock;
            for (int i = 0; i < preparedSpawns.Count; i++)
            {
                PreparedSpawn prepared = preparedSpawns[i];
                EnemyData data = prepared.Data;
                Vector3 spawnPosition = prepared.Position;
                Block routeTarget = prepared.TargetsSupply && supplyTarget != null ? supplyTarget : core;
                IReadOnlyList<Vector2Int> pathCells = Array.Empty<Vector2Int>();
                if (gridManager != null && data != null && routeTarget != null)
                {
                    GridPathfinder pathfinder = new(
                        gridManager,
                        data,
                        UnityEngine.Random.Range(1, int.MaxValue));
                    if (pathfinder.TryBuildPath(
                            spawnPosition,
                            routeTarget,
                            out GridPathfinder.PathResult path))
                    {
                        pathCells = path.Cells;
                    }
                }

                plan.Add(new SpawnRequest(data, spawnPosition, pathCells, routeTarget));
            }

            return plan;
        }

        private void ApplyDifficulty(int waveIndex, ref WaveDifficultySnapshot difficulty)
        {
            if (difficulty.EnemyCount <= 0)
            {
                difficulty = new WaveDifficultySnapshot(
                    waveIndex,
                    0f,
                    12,
                    new Vector2Int(5, 8),
                    UnityEngine.Random.Range(5, 9),
                    0.5f,
                    1.2f);
            }

            currentEnemyCount = Mathf.Max(1, difficulty.EnemyCount);
            currentRangedEnemyCount = 0;
            currentSuicideEnemyCount = 0;
            currentWaveData = difficulty.WaveData;
            currentSpawnInterval = Mathf.Max(0.02f, difficulty.SpawnInterval);
            currentSpawnMargin = Mathf.Max(0f, difficulty.SpawnMargin);
        }

        private List<EnemyData> CreateSpawnTypes()
        {
            List<EnemyData> types = new(currentEnemyCount);
            if (currentWaveData == null
                || !currentWaveData.BuildComposition(currentEnemyCount, types))
            {
                Debug.LogError(
                    $"Wave {preparedWaveIndex} has no valid WaveData composition. Using fallback EnemyData.",
                    currentWaveData);
                types.Clear();
                for (int i = 0; i < currentEnemyCount; i++)
                    types.Add(fallbackEnemyData);
            }

            foreach (EnemyData data in types)
                CountEnemyType(data);

            for (int i = types.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (types[i], types[swapIndex]) = (types[swapIndex], types[i]);
            }

            return types;
        }

        private void SpawnEnemy(SpawnRequest request)
        {
            EnemyData data = request.Data;
            if (data == null || data.Prefab == null)
            {
                Debug.LogError($"{data?.name ?? "EnemyData"} has no Enemy prefab assigned.", data);
                return;
            }

            Enemy enemy = Instantiate(
                data.Prefab,
                request.Position,
                Quaternion.identity);
            enemy.name = data.DisplayName;
            IReadOnlyList<Vector2Int> pathCells = request.PathCells;
            Block routeTarget = request.RouteTarget;
            if (routeTarget == null || !routeTarget.HasGridPosition)
            {
                routeTarget = GridManager.Instance?.Grid?.Core;
                if (routeTarget != null)
                {
                    GridPathfinder pathfinder = new(
                        GridManager.Instance,
                        data,
                        UnityEngine.Random.Range(1, int.MaxValue));
                    if (pathfinder.TryBuildPath(request.Position, routeTarget, out GridPathfinder.PathResult path))
                        pathCells = path.Cells;
                }
            }

            enemy.Initialize(data, pathCells, routeTarget);
            enemy.Died += HandleEnemyDied;
            activeEnemies.Add(enemy);
        }

        private void CountEnemyType(EnemyData data)
        {
            if (data is RangedEnemyData)
                currentRangedEnemyCount++;
            else if (data is SuicideEnemyData)
                currentSuicideEnemyCount++;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float halfHeight = worldCamera.orthographicSize;
            float halfWidth = halfHeight * worldCamera.aspect;
            Vector3 center = worldCamera.transform.position;
            float x = UnityEngine.Random.Range(-halfWidth, halfWidth);
            float y = UnityEngine.Random.Range(-halfHeight, halfHeight);
            return UnityEngine.Random.Range(0, 4) switch
            {
                0 => new Vector3(center.x - halfWidth - currentSpawnMargin, center.y + y, 0f),
                1 => new Vector3(center.x + halfWidth + currentSpawnMargin, center.y + y, 0f),
                2 => new Vector3(center.x + x, center.y - halfHeight - currentSpawnMargin, 0f),
                _ => new Vector3(center.x + x, center.y + halfHeight + currentSpawnMargin, 0f)
            };
        }

        private Vector3 GetAvailableSpawnPosition()
        {
            const int maximumAttempts = 32;
            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                Vector3 candidate = GetRandomSpawnPosition();
                if (!IsOccupiedGridPosition(candidate))
                    return candidate;
            }

            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return GetRandomSpawnPosition();

            int edge = UnityEngine.Random.Range(0, 4);
            int x = UnityEngine.Random.Range(0, gridManager.Width);
            int y = UnityEngine.Random.Range(0, gridManager.Height);
            Vector2Int outsideCell = edge switch
            {
                0 => new Vector2Int(-1, y),
                1 => new Vector2Int(gridManager.Width, y),
                2 => new Vector2Int(x, -1),
                _ => new Vector2Int(x, gridManager.Height)
            };
            return gridManager.GridToWorld(outsideCell);
        }

        private static bool IsOccupiedGridPosition(Vector3 worldPosition)
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager?.Grid == null)
                return false;

            Vector2Int cell = gridManager.WorldToGrid(worldPosition);
            return gridManager.Grid.IsWithinBounds(cell) && !gridManager.IsCellEmpty(cell);
        }

        public bool IsSpawnCellReserved(Vector2Int cell)
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return false;

            foreach (PreparedSpawn spawn in preparedSpawns)
            {
                if (gridManager.WorldToGrid(spawn.Position) == cell)
                    return true;
            }

            return false;
        }

        private void ShowPreparedSpawnIndicators()
        {
            HidePreparedSpawnIndicators(0f);
            if (indicatorPool == null)
                return;

            HashSet<Vector3> uniquePositions = new();
            foreach (PreparedSpawn spawn in preparedSpawns)
            {
                if (!uniquePositions.Add(spawn.Position))
                    continue;

                EnemySpawnIndicatorView indicator = indicatorPool.Rent();
                if (indicator == null)
                    continue;

                activeIndicators.Add(indicator);
                indicator.Show(spawn.Position, spawnIndicatorDiameter);
            }
        }

        private void HidePreparedSpawnIndicators()
        {
            HidePreparedSpawnIndicators(spawnIndicatorHideDuration);
        }

        private void HidePreparedSpawnIndicators(float duration)
        {
            if (indicatorPool == null)
            {
                activeIndicators.Clear();
                return;
            }

            for (int i = activeIndicators.Count - 1; i >= 0; i--)
            {
                EnemySpawnIndicatorView indicator = activeIndicators[i];
                if (indicator == null)
                    continue;

                indicator.Hide(duration, () => indicatorPool.Return(indicator));
            }

            activeIndicators.Clear();
        }

        private void HandleEnemyDied(Enemy enemy)
        {
            enemy.Died -= HandleEnemyDied;
            activeEnemies.Remove(enemy);
            CheckWaveCompleted();
        }

        private void CheckWaveCompleted()
        {
            if (!isSpawning && activeEnemies.Count == 0)
            {
                WaveCompleted?.Invoke();
            }
        }

        private void StopSpawnRoutine()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            isSpawning = false;
        }

        private void OnDestroy()
        {
            HidePreparedSpawnIndicators(0f);
        }

    }
}
