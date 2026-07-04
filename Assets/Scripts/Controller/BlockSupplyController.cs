using System;
using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Controllers
{
    public sealed class BlockSupplyController : MonoBehaviour
    {
        public readonly struct GrantedBlock
        {
            public GrantedBlock(BlockData data, bool isRare)
            {
                Data = data;
                IsRare = isRare;
            }

            public BlockData Data { get; }
            public bool IsRare { get; }
        }

        [SerializeField] private BlockSupplyData supplyData;

        private readonly List<GrantedBlock> grantedBlocks = new();
        private int rerollCount;
        private bool hasUsedBlock;

        public IReadOnlyList<GrantedBlock> GrantedBlocks => grantedBlocks;
        public float CurrentRerollCost => supplyData == null
            ? 0f
            : supplyData.InitialRerollCost + rerollCount * supplyData.RerollCostIncrease;
        public bool CanReroll => GameManager.Phase == GamePhase.Preparation
                                 && !hasUsedBlock
                                 && grantedBlocks.Count > 0
                                 && GameManager.PlacePoint.CurrentValue >= CurrentRerollCost;

        public event Action<bool> SupplyChanged;

        private void Start()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            BeginPreparation();
        }

        private void OnDestroy()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }

        public bool TryGet(int index, out GrantedBlock grantedBlock)
        {
            if (index < 0 || index >= grantedBlocks.Count)
            {
                grantedBlock = default;
                return false;
            }

            grantedBlock = grantedBlocks[index];
            return true;
        }

        public bool TryConsume(int index, out GrantedBlock grantedBlock)
        {
            if (!TryGet(index, out grantedBlock))
                return false;

            grantedBlocks.RemoveAt(index);
            hasUsedBlock = true;
            SupplyChanged?.Invoke(false);
            return true;
        }

        public bool TryReroll()
        {
            if (!CanReroll)
                return false;

            GameManager.PlacePoint.SubtractValue(CurrentRerollCost);
            rerollCount++;
            DealBlocks();
            return true;
        }

        public BlockData GetRandomBasicBlock()
        {
            return supplyData != null ? ChooseWeighted(supplyData.BasicBlocks) : null;
        }

        public void AddGrantedBlock(BlockData data, bool isRare = true)
        {
            if (data == null)
                return;

            grantedBlocks.Add(new GrantedBlock(data, isRare));
            SupplyChanged?.Invoke(true);
        }

        public void EndPreparation()
        {
            grantedBlocks.Clear();
            SupplyChanged?.Invoke(false);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Preparation)
                BeginPreparation();
            else if (phase == GamePhase.Combat)
                EndPreparation();
        }

        private void BeginPreparation()
        {
            rerollCount = 0;
            hasUsedBlock = false;
            DealBlocks();
        }

        private void DealBlocks()
        {
            grantedBlocks.Clear();
            if (supplyData == null)
            {
                Debug.LogError("BlockSupplyController has no BlockSupplyData.", this);
                SupplyChanged?.Invoke(true);
                return;
            }

            int count = UnityEngine.Random.Range(
                supplyData.MinimumBlocks,
                supplyData.MaximumBlocks + 1);
            for (int i = 0; i < count; i++)
            {
                bool rare = supplyData.RareBlocks.Count > 0
                            && UnityEngine.Random.value < supplyData.RareBlockChance;
                BlockData block = ChooseWeighted(rare
                    ? supplyData.RareBlocks
                    : supplyData.BasicBlocks);
                if (block == null && rare)
                {
                    rare = false;
                    block = ChooseWeighted(supplyData.BasicBlocks);
                }

                if (block != null)
                    grantedBlocks.Add(new GrantedBlock(block, rare));
            }

            SupplyChanged?.Invoke(true);
        }

        private static BlockData ChooseWeighted(
            IReadOnlyList<BlockSupplyData.WeightedBlock> entries)
        {
            float totalWeight = 0f;
            BlockData lastValidBlock = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Block != null)
                {
                    totalWeight += entries[i].Weight;
                    lastValidBlock = entries[i].Block;
                }
            }

            if (totalWeight <= 0f)
                return null;

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < entries.Count; i++)
            {
                BlockSupplyData.WeightedBlock entry = entries[i];
                if (entry.Block == null)
                    continue;

                roll -= entry.Weight;
                if (roll <= 0f)
                    return entry.Block;
            }

            return lastValidBlock;
        }
    }
}
