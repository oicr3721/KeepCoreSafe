using System;
using System.Collections;
using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using UnityEngine;
using UnityEngine.Serialization;

namespace KeepCoreSafe.Managers
{
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("enemyData")]
        private MeleeEnemyData meleeEnemyData;

        [SerializeField]
        private RangedEnemyData rangedEnemyData;

        [Header("Audio")]
        [Tooltip("Played once when a combat wave begins.")]
        [SerializeField] private AudioCue waveStartSound = new();

        private readonly HashSet<Enemy> activeEnemies = new();
        private Coroutine spawnRoutine;
        private Camera worldCamera;
        private bool isSpawning;
        private int currentEnemyCount;
        private int currentRangedEnemyCount;
        private float currentSpawnInterval;
        private float currentSpawnMargin;

        public int ActiveEnemyCount => activeEnemies.Count;
        public IReadOnlyCollection<Enemy> ActiveEnemies => activeEnemies;
        public int CurrentWaveEnemyCount => currentEnemyCount;
        public int CurrentWaveRangedEnemyCount => currentRangedEnemyCount;

        public event Action WaveCompleted;

        private void Awake()
        {
            worldCamera = Camera.main;
            if (meleeEnemyData == null)
                meleeEnemyData = Resources.Load<MeleeEnemyData>("Data/Enemy/MeleeEnemyData");
            if (rangedEnemyData == null)
                rangedEnemyData = Resources.Load<RangedEnemyData>("Data/Enemy/RangedEnemyData");
        }

        public void StartWave(int waveIndex, WaveDifficultySnapshot difficulty)
        {
            StopSpawnRoutine();
            if (difficulty.EnemyCount <= 0)
            {
                difficulty = new WaveDifficultySnapshot(
                    waveIndex,
                    0f,
                    30f,
                    new Vector2Int(5, 8),
                    UnityEngine.Random.Range(5, 9),
                    new Vector2(0.15f, 0.25f),
                    UnityEngine.Random.Range(0.15f, 0.25f),
                    0.5f,
                    1.2f);
            }

            currentEnemyCount = Mathf.Max(1, difficulty.EnemyCount);
            currentRangedEnemyCount = rangedEnemyData == null
                ? 0
                : Mathf.Min(difficulty.RangedEnemyCount, currentEnemyCount);
            currentSpawnInterval = Mathf.Max(0.02f, difficulty.SpawnInterval);
            currentSpawnMargin = Mathf.Max(0f, difficulty.SpawnMargin);
            AudioManager.Play(waveStartSound);
            spawnRoutine = StartCoroutine(SpawnWave());
            Debug.Log(
                $"Wave {waveIndex}: {currentEnemyCount} enemies " +
                $"({currentRangedEnemyCount} ranged), interval {currentSpawnInterval:0.00}s, " +
                $"charge {difficulty.CombatDuration:0.0}s.");
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

        private IEnumerator SpawnWave()
        {
            isSpawning = true;
            List<bool> spawnTypes = CreateSpawnTypes();
            for (int i = 0; i < currentEnemyCount; i++)
            {
                SpawnEnemy(spawnTypes[i]);
                yield return new WaitForSeconds(currentSpawnInterval);
            }

            isSpawning = false;
            spawnRoutine = null;
            CheckWaveCompleted();
        }

        private List<bool> CreateSpawnTypes()
        {
            List<bool> types = new List<bool>(currentEnemyCount);
            for (int i = 0; i < currentEnemyCount; i++)
                types.Add(i < currentRangedEnemyCount);

            for (int i = types.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (types[i], types[swapIndex]) = (types[swapIndex], types[i]);
            }

            return types;
        }

        private void SpawnEnemy(bool spawnRanged)
        {
            EnemyData data = spawnRanged ? rangedEnemyData : meleeEnemyData;
            if (data == null || data.Prefab == null)
            {
                Debug.LogError($"{data?.name ?? "EnemyData"} has no Enemy prefab assigned.", data);
                return;
            }

            Enemy enemy = Instantiate(
                data.Prefab,
                GetRandomSpawnPosition(),
                Quaternion.identity);
            enemy.name = data.DisplayName;
            enemy.Initialize(data);
            enemy.Died += HandleEnemyDied;
            activeEnemies.Add(enemy);
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

    }
}
