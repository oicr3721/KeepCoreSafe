using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "WaveDifficultyData", menuName = "Keep Core Safe/Systems/Wave Difficulty")]
    public sealed class WaveDifficultyData : ScriptableObject
    {
        [Header("Progression Curve")]
        [Tooltip("The wave that reaches the late-game values below. Endless-growth settings continue scaling afterward.")]
        [SerializeField, Min(2)] private int lateGameWave = 30;

        [Tooltip("X is normalized wave progress and Y is applied difficulty. The default curve grows gently early and faster later.")]
        [SerializeField] private AnimationCurve progressionCurve = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.35f, 0.14f),
            new Keyframe(0.7f, 0.48f),
            new Keyframe(1f, 1f));

        [Header("Shockwave Required Energy")]
        [SerializeField, Min(1)] private int firstWaveRequiredEnergy = 12;
        [SerializeField, Min(1)] private int lateGameRequiredEnergy = 80;

        [Header("Enemy Count Range")]
        [Tooltip("Random Min/Max enemy count on the first wave.")]
        [SerializeField] private Vector2Int firstWaveEnemyCount = new(5, 8);
        [Tooltip("Random Min/Max enemy count at the late-game wave.")]
        [SerializeField] private Vector2Int lateGameEnemyCount = new(38, 56);

        [Header("Wave Data Pool")]
        [SerializeField] private List<WaveData> normalWaveList = new();
        [SerializeField] private List<WaveData> specialWaveList = new();
        [Tooltip("Every Nth wave uses the Special Wave List. Set to 0 to disable special waves.")]
        [SerializeField, Min(0)] private int specialWaveInterval = 5;

        [Header("Spawn Pressure")]
        [SerializeField, Min(0.02f)] private float firstWaveSpawnInterval = 0.58f;
        [SerializeField, Min(0.02f)] private float lateGameSpawnInterval = 0.12f;
        [SerializeField, Min(0f)] private float firstWaveSpawnMargin = 1.8f;
        [SerializeField, Min(0f)] private float lateGameSpawnMargin = 0.45f;

        [Header("Beyond Late Game")]
        [Tooltip("Keeps endless waves growing after Late Game Wave instead of hard-capping.")]
        [SerializeField, Min(0f)] private float enemyGrowthPerExtraWave = 1.4f;
        [SerializeField, Min(0)] private int requiredEnergyGrowthPerExtraWave = 2;
        [SerializeField, Range(0.9f, 1f)] private float spawnIntervalMultiplierPerExtraWave = 0.985f;
        [SerializeField, Min(0f)] private float spawnMarginReductionPerExtraWave = 0.01f;

        public IReadOnlyList<WaveData> NormalWaveList => normalWaveList;
        public IReadOnlyList<WaveData> SpecialWaveList => specialWaveList;
        public int SpecialWaveInterval => specialWaveInterval;

        public WaveDifficultySnapshot Roll(int waveIndex, WaveData previousWaveData = null)
        {
            float normalizedWave = Mathf.InverseLerp(1f, lateGameWave, Mathf.Max(1, waveIndex));
            float difficulty = Mathf.Clamp01(progressionCurve.Evaluate(normalizedWave));
            int extraWaves = Mathf.Max(0, waveIndex - lateGameWave);

            Vector2Int countRange = new(
                Mathf.RoundToInt(Mathf.Lerp(firstWaveEnemyCount.x, lateGameEnemyCount.x, difficulty)),
                Mathf.RoundToInt(Mathf.Lerp(firstWaveEnemyCount.y, lateGameEnemyCount.y, difficulty)));
            int extraEnemies = Mathf.RoundToInt(extraWaves * enemyGrowthPerExtraWave);
            countRange += new Vector2Int(extraEnemies, extraEnemies + Mathf.RoundToInt(extraWaves * 0.2f));
            countRange.x = Mathf.Max(1, countRange.x);
            countRange.y = Mathf.Max(countRange.x, countRange.y);

            bool isSpecialWave = specialWaveInterval > 0 && waveIndex % specialWaveInterval == 0;
            WaveData selectedWaveData = SelectWaveData(
                isSpecialWave ? specialWaveList : normalWaveList,
                previousWaveData);

            return new WaveDifficultySnapshot(
                waveIndex,
                difficulty,
                Mathf.RoundToInt(Mathf.Lerp(firstWaveRequiredEnergy, lateGameRequiredEnergy, difficulty))
                + extraWaves * requiredEnergyGrowthPerExtraWave,
                countRange,
                UnityEngine.Random.Range(countRange.x, countRange.y + 1),
                Mathf.Lerp(firstWaveSpawnInterval, lateGameSpawnInterval, difficulty)
                * Mathf.Pow(spawnIntervalMultiplierPerExtraWave, extraWaves),
                Mathf.Max(
                    0.2f,
                    Mathf.Lerp(firstWaveSpawnMargin, lateGameSpawnMargin, difficulty)
                    - extraWaves * spawnMarginReductionPerExtraWave),
                selectedWaveData,
                isSpecialWave);
        }

        private static WaveData SelectWaveData(IReadOnlyList<WaveData> pool, WaveData previous)
        {
            if (pool == null || pool.Count == 0)
                return null;

            List<WaveData> valid = new(pool.Count);
            bool hasAlternative = false;
            foreach (WaveData waveData in pool)
            {
                if (waveData == null || !waveData.HasValidComposition() || valid.Contains(waveData))
                    continue;

                valid.Add(waveData);
                if (waveData != previous)
                    hasAlternative = true;
            }

            if (valid.Count == 0)
                return null;
            if (valid.Count == 1)
                return valid[0];

            if (hasAlternative && previous != null)
                valid.Remove(previous);
            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
    }

    public readonly struct WaveDifficultySnapshot
    {
        public WaveDifficultySnapshot(
            int waveIndex,
            float normalizedDifficulty,
            int requiredEnergy,
            Vector2Int enemyCountRange,
            int enemyCount,
            float spawnInterval,
            float spawnMargin,
            WaveData waveData = null,
            bool isSpecialWave = false)
        {
            WaveIndex = waveIndex;
            NormalizedDifficulty = normalizedDifficulty;
            RequiredEnergy = Mathf.Max(1, requiredEnergy);
            EnemyCountRange = enemyCountRange;
            EnemyCount = enemyCount;
            SpawnInterval = spawnInterval;
            SpawnMargin = spawnMargin;
            WaveData = waveData;
            IsSpecialWave = isSpecialWave;
        }

        public int WaveIndex { get; }
        public float NormalizedDifficulty { get; }
        public int RequiredEnergy { get; }
        public Vector2Int EnemyCountRange { get; }
        public int EnemyCount { get; }
        public float SpawnInterval { get; }
        public float SpawnMargin { get; }
        public WaveData WaveData { get; }
        public bool IsSpecialWave { get; }
    }
}
