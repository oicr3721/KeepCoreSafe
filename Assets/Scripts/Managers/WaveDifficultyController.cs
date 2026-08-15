using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.Managers
{
    public sealed class WaveDifficultyController : MonoBehaviour
    {
        [Tooltip("Single source of truth for every wave-scaled balance value.")]
        [SerializeField] private WaveDifficultyData difficultyData;

        private WaveData previousWaveData;

        public WaveDifficultySnapshot Current { get; private set; }

        public WaveDifficultySnapshot RollForWave(int waveIndex)
        {
            if (difficultyData == null)
            {
                Debug.LogError("WaveDifficultyController has no WaveDifficultyData.", this);
                Current = new WaveDifficultySnapshot(
                    waveIndex,
                    0f,
                    12,
                    new Vector2Int(5, 8),
                    UnityEngine.Random.Range(5, 9),
                    0.5f,
                    1.2f);
                return Current;
            }

            if (Current.WaveIndex == waveIndex && Current.WaveData != null)
                return Current;

            Current = difficultyData.Roll(waveIndex, previousWaveData);
            if (Current.WaveData != null)
                previousWaveData = Current.WaveData;
            return Current;
        }
    }
}
