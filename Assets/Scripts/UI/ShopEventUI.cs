using System.Collections.Generic;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class ShopEventUI : MonoBehaviour
    {
        [SerializeField] private ShopEventController controller;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Transform offerRoot;
        [SerializeField] private GameObject offerButtonPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<Button> buttonPool = new();

        private void Awake()
        {
            visualRoot.SetActive(false);
            closeButton.onClick.AddListener(controller.CloseShop);
        }

        private void OnEnable()
        {
            controller.ShopOpened += Show;
            controller.ShopClosed += Hide;
            controller.OffersChanged += Refresh;
            GameManager.PlacePoint.OnValueChanged += HandlePointsChanged;
        }

        private void OnDisable()
        {
            controller.ShopOpened -= Show;
            controller.ShopClosed -= Hide;
            controller.OffersChanged -= Refresh;
            GameManager.PlacePoint.OnValueChanged -= HandlePointsChanged;
        }

        private void Show()
        {
            visualRoot.SetActive(true);
            Refresh();
        }

        private void Hide()
        {
            visualRoot.SetActive(false);
        }

        private void Refresh()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            EnsureButtonCount(offers.Count);
            for (int i = 0; i < buttonPool.Count; i++)
            {
                Button button = buttonPool[i];
                bool active = i < offers.Count;
                button.gameObject.SetActive(active);
                if (!active)
                    continue;

                int offerIndex = i;
                ShopOfferData offer = offers[i];
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                label.text = $"{offer.DisplayName}\n{offer.Description}\n{offer.Cost:0} Point";
                button.interactable = GameManager.PlacePoint.CurrentValue >= offer.Cost;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => controller.TryPurchase(offerIndex));
            }
        }

        private void EnsureButtonCount(int count)
        {
            while (buttonPool.Count < count)
            {
                GameObject instance = Instantiate(offerButtonPrefab, offerRoot);
                buttonPool.Add(instance.GetComponent<Button>());
            }
        }

        private void HandlePointsChanged(float _, float __)
        {
            if (visualRoot.activeSelf)
                Refresh();
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(controller.CloseShop);
        }
    }
}
