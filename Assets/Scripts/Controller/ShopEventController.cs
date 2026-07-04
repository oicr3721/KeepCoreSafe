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

        public IReadOnlyList<ShopOfferData> CurrentOffers => currentOffers;
        public event Action ShopOpened;
        public event Action ShopClosed;
        public event Action OffersChanged;

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
            if (offerIndex < 0 || offerIndex >= currentOffers.Count)
                return false;

            bool purchased = currentOffers[offerIndex].TryPurchase(supplyController);
            if (purchased)
                OffersChanged?.Invoke();
            return purchased;
        }

        public void CloseShop()
        {
            currentOffers.Clear();
            ShopClosed?.Invoke();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Preparation
                || GameManager.WaveIndex <= 0
                || shopData == null
                || !shopData.ShouldOpenAfterWave(GameManager.WaveIndex))
            {
                return;
            }

            BuildOfferList();
            ShopOpened?.Invoke();
        }

        private void BuildOfferList()
        {
            currentOffers.Clear();
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
