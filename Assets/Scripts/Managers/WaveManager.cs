using System;
using System.Collections;
using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using UnityEngine;
using UnityEngine.Serialization;

namespace KeepCoreSafe.Managers
{
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField, Range(1, 50)]
        private int enemiesPerWave = 20;

        [SerializeField, Min(0.05f)]
        private float spawnInterval = 0.4f;

        [SerializeField, Min(0f)]
        private float spawnMargin = 1.2f;

        [SerializeField]
        [FormerlySerializedAs("enemyData")]
        private MeleeEnemyData meleeEnemyData;

        [SerializeField, Range(0, 30)]
        private int rangedEnemiesPerWave = 10;

        [SerializeField]
        private RangedEnemyData rangedEnemyData;

        private readonly HashSet<Enemy> activeEnemies = new();
        private Coroutine spawnRoutine;
        private Camera worldCamera;
        private bool isSpawning;

        public int ActiveEnemyCount => activeEnemies.Count;

        public event Action WaveCompleted;

        private void Awake()
        {
            worldCamera = Camera.main;
            if (meleeEnemyData == null)
                meleeEnemyData = Resources.Load<MeleeEnemyData>("Data/Enemy/MeleeEnemyData");
            if (rangedEnemyData == null)
                rangedEnemyData = Resources.Load<RangedEnemyData>("Data/Enemy/RangedEnemyData");
        }

        public void StartWave(int waveIndex)
        {
            StopSpawnRoutine();
            spawnRoutine = StartCoroutine(SpawnWave());
            Debug.Log($"Wave {waveIndex} started.");
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

        private IEnumerator SpawnWave()
        {
            isSpawning = true;
            List<bool> spawnTypes = CreateSpawnTypes();
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy(spawnTypes[i]);
                yield return new WaitForSeconds(spawnInterval);
            }

            isSpawning = false;
            spawnRoutine = null;
            CheckWaveCompleted();
        }

        private List<bool> CreateSpawnTypes()
        {
            int rangedCount = rangedEnemyData == null
                ? 0
                : Mathf.Min(rangedEnemiesPerWave, enemiesPerWave);
            List<bool> types = new List<bool>(enemiesPerWave);
            for (int i = 0; i < enemiesPerWave; i++)
                types.Add(i < rangedCount);

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
                0 => new Vector3(center.x - halfWidth - spawnMargin, center.y + y, 0f),
                1 => new Vector3(center.x + halfWidth + spawnMargin, center.y + y, 0f),
                2 => new Vector3(center.x + x, center.y - halfHeight - spawnMargin, 0f),
                _ => new Vector3(center.x + x, center.y + halfHeight + spawnMargin, 0f)
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
