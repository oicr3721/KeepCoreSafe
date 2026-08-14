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

        [Header("Related Systems")]
        [SerializeField] private ShopEventController shopEventController;

        [Header("Scripted Supply")]
        [Tooltip("Used by tutorials/cutscenes that require a fixed block order.")]
        [SerializeField] private bool useScriptedSupply;
        [SerializeField] private BlockData[] scriptedBlocks = Array.Empty<BlockData>();

        private readonly List<GrantedBlock> grantedBlocks = new();
        private readonly List<GrantedBlock> guaranteedPreparationBlocks = new();
        private bool hasUsedBlock;
        private bool waitingForShopClose;

        public IReadOnlyList<GrantedBlock> GrantedBlocks => grantedBlocks;
        public int RerollCount { get; private set; }
        public int NextRerollCost => RerollCount + 1;
        public float CurrentRareBlockChance => supplyData != null
            ? supplyData.GetRareBlockChance(RerollCount)
            : 0f;
        public bool CanReroll => !useScriptedSupply
                                 && GameManager.Phase == GamePhase.Preparation
                                 && !hasUsedBlock
                                 && grantedBlocks.Count > 0
                                 && HasEnergyCapacityForNextReroll;
        public bool HasEnergyCapacityForNextReroll =>
            GameManager.Instance != null
            && GameManager.Instance.CanApplyRerollCost(NextRerollCost);

        public event Action<bool> SupplyChanged;

        private void Start()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            if (shopEventController != null)
                shopEventController.ShopClosed += HandleShopClosed;
            BeginPreparation();
        }

        private void OnDestroy()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            if (shopEventController != null)
                shopEventController.ShopClosed -= HandleShopClosed;
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

            if (!GameManager.Instance.TryApplyRerollCost(NextRerollCost))
                return false;

            RerollCount++;
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

            if (waitingForShopClose || (shopEventController != null && shopEventController.IsOpen))
            {
                QueueGuaranteedBlockForNextPreparation(data, isRare);
                return;
            }

            grantedBlocks.Add(new GrantedBlock(data, isRare));
            SupplyChanged?.Invoke(true);
        }

        public bool CanQueueGuaranteedBlockForNextPreparation =>
            !useScriptedSupply
            && supplyData != null
            && guaranteedPreparationBlocks.Count < supplyData.MaximumBlocks;

        public bool QueueGuaranteedBlockForNextPreparation(BlockData data, bool isRare = true)
        {
            if (data == null || !CanQueueGuaranteedBlockForNextPreparation)
                return false;

            guaranteedPreparationBlocks.Add(new GrantedBlock(data, isRare));
            return true;
        }

        public void ResetScriptedSupply()
        {
            if (!useScriptedSupply)
                return;

            hasUsedBlock = false;
            DealBlocks();
        }

        public void EndPreparation()
        {
            grantedBlocks.Clear();
            SupplyChanged?.Invoke(false);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Preparation)
            {
                ResetPreparationState();
                if (ShouldWaitForShop())
                {
                    waitingForShopClose = true;
                    return;
                }

                DealBlocks();
            }
            else if (phase == GamePhase.Combat)
                EndPreparation();
        }

        private bool ShouldWaitForShop()
        {
            return shopEventController != null
                   && shopEventController.WillOpenAfterWave(GameManager.WaveIndex);
        }

        private void HandleShopClosed()
        {
            if (!waitingForShopClose)
                return;

            waitingForShopClose = false;
            DealBlocks();
        }

        private void BeginPreparation()
        {
            ResetPreparationState();
            DealBlocks();
        }

        private void ResetPreparationState()
        {
            hasUsedBlock = false;
            RerollCount = 0;
        }

        private void DealBlocks()
        {
            grantedBlocks.Clear();
            if (useScriptedSupply)
            {
                foreach (BlockData block in scriptedBlocks)
                {
                    if (block != null)
                        grantedBlocks.Add(new GrantedBlock(block, false));
                }

            }
            else if (supplyData == null)
            {
                Debug.LogError("BlockSupplyController has no BlockSupplyData.", this);
            }
            else
            {
                int randomCount = UnityEngine.Random.Range(
                    supplyData.MinimumBlocks,
                    supplyData.MaximumBlocks + 1);
                int count = Mathf.Max(randomCount, guaranteedPreparationBlocks.Count);
                for (int i = 0; i < guaranteedPreparationBlocks.Count; i++)
                    grantedBlocks.Add(guaranteedPreparationBlocks[i]);

                for (int i = guaranteedPreparationBlocks.Count; i < count; i++)
                {
                    bool rare = supplyData.RareBlocks.Count > 0
                                && UnityEngine.Random.value < CurrentRareBlockChance;
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

                Shuffle(grantedBlocks);
            }

            guaranteedPreparationBlocks.Clear();
            SupplyChanged?.Invoke(true);
        }

        private static void Shuffle(List<GrantedBlock> blocks)
        {
            for (int i = blocks.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (blocks[i], blocks[swapIndex]) = (blocks[swapIndex], blocks[i]);
            }
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
