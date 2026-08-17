using System;
using System.Collections.Generic;
using KeepCoreSafe.Analytics;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Controllers
{
    // Keeps the existing serialized component identity while owning the new Supply Event flow.
    public sealed class ShopEventController : MonoBehaviour
    {
        [SerializeField] private ShopEventData shopData;
        [SerializeField] private BlockSupplyController supplyController;
        [SerializeField] private SupplyBlockData supplyBlockData;
        [SerializeField] private SupplySpawnPresentationController spawnPresentation;

        private readonly List<ShopOfferData> currentOffers = new();
        private SupplyBlock activeSupplyBlock;
        private int activeSupplyWave = -1;
        private int lastEventWave;
        private bool resolvingSupply;
        private int selectedOfferIndex = -1;
        private ShopOfferData pendingSelectedOffer;

        public IReadOnlyList<ShopOfferData> CurrentOffers => currentOffers;
        public SupplyBlock ActiveSupplyBlock => activeSupplyBlock != null
            && activeSupplyBlock.HasGridPosition ? activeSupplyBlock : null;
        public bool HasActiveSupply => ActiveSupplyBlock != null;
        public bool IsOpen { get; private set; }
        public event Action ShopOpened;
        public event Action ShopClosing;
        public event Action ShopClosed;
        public event Action OffersChanged;
        public event Action<int> OfferSelected;
        public event Action<SupplyBlock> SupplyEventStarted;
        public event Action SupplyEventFailed;

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            if (GridManager.Instance != null)
                GridManager.Instance.GridChanged += HandleGridChanged;
        }

        private void Start()
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.GridChanged -= HandleGridChanged;
                GridManager.Instance.GridChanged += HandleGridChanged;
            }
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            if (GridManager.Instance != null)
                GridManager.Instance.GridChanged -= HandleGridChanged;
        }

        public bool TrySelectOffer(int offerIndex)
        {
            if (!CanSelectOffer(offerIndex))
                return false;

            pendingSelectedOffer = currentOffers[offerIndex];

            selectedOfferIndex = offerIndex;
            AnalyticsService.OfferSelected(pendingSelectedOffer, GameManager.WaveIndex);
            OfferSelected?.Invoke(offerIndex);
            return true;
        }

        public bool CanSelectOffer(int offerIndex)
        {
            return IsOpen
                   && selectedOfferIndex < 0
                   && offerIndex >= 0
                   && offerIndex < currentOffers.Count
                   && currentOffers[offerIndex].CanSelect(supplyController);
        }

        public bool IsSelected(int offerIndex) => selectedOfferIndex == offerIndex;

        public bool WillOpenAfterWave(int completedWave)
        {
            return activeSupplyBlock != null
                   && activeSupplyBlock.HasGridPosition
                   && activeSupplyWave == completedWave;
        }

        public int GetSupplyHunterCount(int enemyCount)
        {
            if (!HasActiveSupply || shopData == null || enemyCount <= 0)
                return 0;

            return Mathf.Clamp(
                Mathf.Max(shopData.MinimumSupplyHunters,
                    Mathf.CeilToInt(enemyCount * shopData.SupplyHunterRatio)),
                1,
                enemyCount);
        }

        public void CloseShop()
        {
            if (!IsOpen)
                return;

            ShopOfferData offerToApply = pendingSelectedOffer;
            pendingSelectedOffer = null;
            IsOpen = false;
            currentOffers.Clear();
            selectedOfferIndex = -1;
            resolvingSupply = true;
            RemoveActiveSupply();
            resolvingSupply = false;
            // Presentation closes first so every offer's result is visible during
            // the supply/placement transition. Apply before ShopClosed because
            // guaranteed blocks must be queued before BlockSupplyController deals.
            ShopClosing?.Invoke();
            if (offerToApply != null && !offerToApply.TryApply(supplyController))
            {
                Debug.LogWarning(
                    $"Selected shop offer '{offerToApply.name}' could not be applied during the supply transition.",
                    offerToApply);
            }
            ShopClosed?.Invoke();
            GameManager.Instance?.RefreshPreparedWave();
        }

        public bool TryStartPostWaveSupplySequence(int completedWave, Action onComplete)
        {
            if (!isActiveAndEnabled
                || IsOpen
                || HasActiveSupply
                || shopData == null
                || supplyBlockData == null
                || !shopData.ShouldStartAfterWave(completedWave, lastEventWave)
                || !TryChooseSupplyCell(out Vector2Int position))
            {
                return false;
            }

            SupplyBlock block = Instantiate(supplyBlockData.Prefab) as SupplyBlock;
            if (block == null)
            {
                Debug.LogError("SupplyBlockData must reference a SupplyBlock prefab.", supplyBlockData);
                return false;
            }

            block.Initialize(supplyBlockData);
            if (!GridManager.Instance.TryPlaceBlock(block, position))
            {
                Destroy(block.gameObject);
                return false;
            }

            activeSupplyBlock = block;
            activeSupplyWave = completedWave + 1;
            lastEventWave = completedWave;
            activeSupplyBlock.Died += HandleSupplyDestroyed;
            SupplyEventStarted?.Invoke(activeSupplyBlock);

            if (spawnPresentation != null && spawnPresentation.Play(activeSupplyBlock, onComplete))
                return true;

            Debug.LogWarning(
                "Supply spawn presentation is not configured. Continuing without a recognition hold.",
                this);
            activeSupplyBlock.PlayPlacementAnimation();
            onComplete?.Invoke();
            return true;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Preparation)
                return;

            if (WillOpenAfterWave(GameManager.WaveIndex))
            {
                CompleteSuccessfulSupply();
                return;
            }

            if (activeSupplyWave == GameManager.WaveIndex)
                ClearFailedSupply();
        }

        private void CompleteSuccessfulSupply()
        {
            BuildOfferList();
            IsOpen = currentOffers.Count > 0;
            if (IsOpen)
                ShopOpened?.Invoke();
            else
            {
                resolvingSupply = true;
                RemoveActiveSupply();
                resolvingSupply = false;
                // BlockSupplyController may already be waiting depending on listener order.
                ShopClosing?.Invoke();
                ShopClosed?.Invoke();
            }
        }

        private bool TryChooseSupplyCell(out Vector2Int selected)
        {
            selected = default;
            GridManager grid = GridManager.Instance;
            if (grid?.Grid == null)
                return false;

            List<Vector2Int> candidates = new();
            HashSet<Vector2Int> unique = new();
            foreach (Block block in grid.GetBlocks())
            {
                if (block == null || !block.HasGridPosition)
                    continue;

                foreach (Vector2Int direction in GridPathfinder.Directions)
                {
                    Vector2Int cell = block.GridPosition + direction;
                    if (grid.Grid.IsWithinBounds(cell)
                        && grid.IsCellEmpty(cell)
                        && unique.Add(cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            if (candidates.Count == 0)
                return false;

            selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }

        private void HandleSupplyDestroyed(Block block)
        {
            if (activeSupplyBlock != block)
                return;

            activeSupplyBlock.Died -= HandleSupplyDestroyed;
            activeSupplyBlock = null;
            SupplyEventFailed?.Invoke();
        }

        private void HandleGridChanged()
        {
            if (resolvingSupply || activeSupplyBlock == null || activeSupplyBlock.HasGridPosition)
                return;

            activeSupplyBlock.Died -= HandleSupplyDestroyed;
            activeSupplyBlock = null;
            SupplyEventFailed?.Invoke();
        }

        private void ClearFailedSupply()
        {
            activeSupplyBlock = null;
            activeSupplyWave = -1;
        }

        private void RemoveActiveSupply()
        {
            if (activeSupplyBlock == null)
                return;

            activeSupplyBlock.Died -= HandleSupplyDestroyed;
            if (activeSupplyBlock.HasGridPosition)
                GridManager.Instance.TryRemoveBlock(activeSupplyBlock.GridPosition, out _);
            Destroy(activeSupplyBlock.gameObject);
            activeSupplyBlock = null;
            activeSupplyWave = -1;
        }

        private void BuildOfferList()
        {
            currentOffers.Clear();
            selectedOfferIndex = -1;
            pendingSelectedOffer = null;
            List<ShopOfferData> candidates = new();
            foreach (ShopOfferData offer in shopData.Offers)
            {
                if (offer != null)
                    candidates.Add(offer);
            }

            int count = Mathf.Min(shopData.OffersPerEvent, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                currentOffers.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            OffersChanged?.Invoke();
        }
    }
}
