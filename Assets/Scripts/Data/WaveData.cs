using System;
using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "WaveData", menuName = "Keep Core Safe/Waves/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        [Serializable]
        public sealed class EnemyWeight
        {
            [SerializeField] private EnemyData enemyData;
            [SerializeField, Min(0f)] private float weight = 1f;

            public EnemyData EnemyData => enemyData;
            public float Weight => Mathf.Max(0f, weight);
        }

        private struct Allocation
        {
            public EnemyData Data;
            public int Count;
            public float Remainder;
        }

        [Header("Identity")]
        [SerializeField] private string waveName;
        [SerializeField, TextArea(2, 5)] private string designIntent;
        [SerializeField, TextArea(2, 5)] private string keyStrategy;

        [Header("Enemy Composition")]
        [SerializeField] private List<EnemyWeight> enemyComposition = new();

        public string WaveName => string.IsNullOrWhiteSpace(waveName) ? name : waveName;
        public string DesignIntent => designIntent;
        public string KeyStrategy => keyStrategy;
        public IReadOnlyList<EnemyWeight> EnemyComposition => enemyComposition;

        public bool BuildComposition(int enemyCount, List<EnemyData> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            result.Clear();
            enemyCount = Mathf.Max(0, enemyCount);
            float totalWeight = GetTotalWeight();
            if (enemyCount == 0)
                return true;
            if (totalWeight <= 0f)
                return false;

            List<Allocation> allocations = new(enemyComposition.Count);
            int allocatedCount = 0;
            foreach (EnemyWeight entry in enemyComposition)
            {
                if (!IsValid(entry))
                    continue;

                float exactCount = enemyCount * entry.Weight / totalWeight;
                int count = Mathf.FloorToInt(exactCount);
                allocations.Add(new Allocation
                {
                    Data = entry.EnemyData,
                    Count = count,
                    Remainder = exactCount - count
                });
                allocatedCount += count;
            }

            int remaining = enemyCount - allocatedCount;
            while (remaining-- > 0)
            {
                int bestIndex = 0;
                for (int i = 1; i < allocations.Count; i++)
                {
                    if (allocations[i].Remainder > allocations[bestIndex].Remainder)
                        bestIndex = i;
                }

                Allocation allocation = allocations[bestIndex];
                allocation.Count++;
                allocation.Remainder = -1f;
                allocations[bestIndex] = allocation;
            }

            foreach (Allocation allocation in allocations)
            {
                for (int i = 0; i < allocation.Count; i++)
                    result.Add(allocation.Data);
            }

            return result.Count == enemyCount;
        }

        public EnemyData ChooseWeightedEnemy()
        {
            float totalWeight = GetTotalWeight();
            if (totalWeight <= 0f)
                return null;

            float roll = UnityEngine.Random.value * totalWeight;
            EnemyData lastValid = null;
            foreach (EnemyWeight entry in enemyComposition)
            {
                if (!IsValid(entry))
                    continue;

                lastValid = entry.EnemyData;
                roll -= entry.Weight;
                if (roll <= 0f)
                    return entry.EnemyData;
            }

            return lastValid;
        }

        public bool HasValidComposition()
        {
            return GetTotalWeight() > 0f;
        }

        private float GetTotalWeight()
        {
            float total = 0f;
            foreach (EnemyWeight entry in enemyComposition)
            {
                if (IsValid(entry))
                    total += entry.Weight;
            }

            return total;
        }

        private static bool IsValid(EnemyWeight entry)
        {
            return entry?.EnemyData != null && entry.Weight > 0f;
        }
    }
}
