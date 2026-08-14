using System;
using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "BlockSupplyData", menuName = "Keep Core Safe/Block System/Supply")]
    public sealed class BlockSupplyData : ScriptableObject
    {
        [Serializable]
        public sealed class WeightedBlock
        {
            [SerializeField] private BlockData block;
            [SerializeField, Min(0f)] private float weight = 1f;

            public BlockData Block => block;
            public float Weight => weight;
        }

        [Header("Grant Count")]
        [SerializeField, Min(1)] private int minimumBlocks = 3;
        [SerializeField, Min(1)] private int maximumBlocks = 5;

        [Header("Basic Block Pool")]
        [SerializeField] private List<WeightedBlock> basicBlocks = new();

        [Header("Rare Completed Block Pool")]
        [SerializeField, Range(0f, 1f)] private float rareBlockChance = 0.05f;
        [SerializeField, Min(0f)] private float rareChanceIncreasePerReroll = 0.01f;
        [SerializeField] private List<WeightedBlock> rareBlocks = new();

        public int MinimumBlocks => Mathf.Min(minimumBlocks, maximumBlocks);
        public int MaximumBlocks => Mathf.Max(minimumBlocks, maximumBlocks);
        public float RareBlockChance => rareBlockChance;
        public float RareChanceIncreasePerReroll => rareChanceIncreasePerReroll;
        public IReadOnlyList<WeightedBlock> BasicBlocks => basicBlocks;
        public IReadOnlyList<WeightedBlock> RareBlocks => rareBlocks;

        public float GetRareBlockChance(int rerollCount)
        {
            return Mathf.Clamp01(
                rareBlockChance
                + Mathf.Max(0, rerollCount) * rareChanceIncreasePerReroll);
        }
    }
}
