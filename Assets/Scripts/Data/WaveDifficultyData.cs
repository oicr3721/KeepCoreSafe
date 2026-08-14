using System;
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

        [Header("Ranged Enemy Ratio Range")]
        [Tooltip("A ratio is randomly rolled inside this range each wave.")]
        [SerializeField] private Vector2 firstWaveRangedRatio = new(0.12f, 0.24f);
        [SerializeField] private Vector2 lateGameRangedRatio = new(0.22f, 0.42f);

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
        [SerializeField, Min(0f)] private float rangedRatioGrowthPerExtraWave = 0.002f;

        public WaveDifficultySnapshot Roll(int waveIndex)
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

            Vector2 ratioRange = new(
                Mathf.Lerp(firstWaveRangedRatio.x, lateGameRangedRatio.x, difficulty),
                Mathf.Lerp(firstWaveRangedRatio.y, lateGameRangedRatio.y, difficulty));
            ratioRange += Vector2.one * (extraWaves * rangedRatioGrowthPerExtraWave);
            ratioRange.x = Mathf.Clamp01(ratioRange.x);
            ratioRange.y = Mathf.Clamp(ratioRange.y, ratioRange.x, 1f);

            int enemyCount = UnityEngine.Random.Range(countRange.x, countRange.y + 1);
            float rangedRatio = UnityEngine.Random.Range(ratioRange.x, ratioRange.y);
            return new WaveDifficultySnapshot(
                waveIndex,
                difficulty,
                Mathf.RoundToInt(Mathf.Lerp(firstWaveRequiredEnergy, lateGameRequiredEnergy, difficulty))
                + extraWaves * requiredEnergyGrowthPerExtraWave,
                countRange,
                enemyCount,
                ratioRange,
                rangedRatio,
                Mathf.Lerp(firstWaveSpawnInterval, lateGameSpawnInterval, difficulty)
                * Mathf.Pow(spawnIntervalMultiplierPerExtraWave, extraWaves),
                Mathf.Max(
                    0.2f,
                    Mathf.Lerp(firstWaveSpawnMargin, lateGameSpawnMargin, difficulty)
                    - extraWaves * spawnMarginReductionPerExtraWave));
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
            Vector2 rangedRatioRange,
            float rangedRatio,
            float spawnInterval,
            float spawnMargin)
        {
            WaveIndex = waveIndex;
            NormalizedDifficulty = normalizedDifficulty;
            RequiredEnergy = Mathf.Max(1, requiredEnergy);
            EnemyCountRange = enemyCountRange;
            EnemyCount = enemyCount;
            RangedRatioRange = rangedRatioRange;
            RangedRatio = rangedRatio;
            SpawnInterval = spawnInterval;
            SpawnMargin = spawnMargin;
        }

        public int WaveIndex { get; }
        public float NormalizedDifficulty { get; }
        public int RequiredEnergy { get; }
        public Vector2Int EnemyCountRange { get; }
        public int EnemyCount { get; }
        public Vector2 RangedRatioRange { get; }
        public float RangedRatio { get; }
        public float SpawnInterval { get; }
        public float SpawnMargin { get; }
        public int RangedEnemyCount => Mathf.Clamp(
            Mathf.RoundToInt(EnemyCount * RangedRatio),
            0,
            EnemyCount);

    }
}
