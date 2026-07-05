using System;
using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Controllers
{
    public sealed class ShopEventController : MonoBehaviour
    {
        [SerializeField] private ShopEventData shopData;
        [SerializeField] private BlockSupplyController supplyController;

        private readonly List<ShopOfferData> currentOffers = new();
        private readonly HashSet<int> purchasedOfferIndices = new();

        public IReadOnlyList<ShopOfferData> CurrentOffers => currentOffers;
        public bool IsOpen { get; private set; }
        public event Action ShopOpened;
        public event Action ShopClosed;
        public event Action OffersChanged;
        public event Action<int> OfferPurchased;

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }

        public bool TryPurchase(int offerIndex)
        {
            if (!IsOpen
                || offerIndex < 0
                || offerIndex >= currentOffers.Count
                || purchasedOfferIndices.Contains(offerIndex))
            {
                return false;
            }

            bool purchased = currentOffers[offerIndex].TryPurchase(supplyController);
            if (purchased)
            {
                purchasedOfferIndices.Add(offerIndex);
                OfferPurchased?.Invoke(offerIndex);
            }
            return purchased;
        }

        public bool IsPurchased(int offerIndex)
        {
            return purchasedOfferIndices.Contains(offerIndex);
        }

        public bool WillOpenAfterWave(int completedWave)
        {
            return isActiveAndEnabled
                   && completedWave > 0
                   && shopData != null
                   && shopData.ShouldOpenAfterWave(completedWave);
        }

        public void CloseShop()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            currentOffers.Clear();
            purchasedOfferIndices.Clear();
            ShopClosed?.Invoke();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Preparation
                || !WillOpenAfterWave(GameManager.WaveIndex))
            {
                return;
            }

            BuildOfferList();
            IsOpen = true;
            ShopOpened?.Invoke();
        }

        private void BuildOfferList()
        {
            currentOffers.Clear();
            purchasedOfferIndices.Clear();
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
