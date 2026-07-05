using System.Collections.Generic;
using DG.Tweening;
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

        [Header("Offer Purchase Animation")]
        [SerializeField, Min(0f)] private float purchasedExitDuration = 0.18f;
        [SerializeField, Range(0f, 1f)] private float purchasedExitScale = 0.15f;
        [SerializeField] private float purchasedDropDistance = 24f;

        private readonly List<Button> buttonPool = new();

        private void Awake()
        {
            visualRoot.SetActive(false);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        private void OnEnable()
        {
            controller.ShopOpened += Show;
            controller.ShopClosed += Hide;
            controller.OffersChanged += Refresh;
            controller.OfferPurchased += HandleOfferPurchased;
            GameManager.PlacePoint.OnValueChanged += HandlePointsChanged;
        }

        private void OnDisable()
        {
            controller.ShopOpened -= Show;
            controller.ShopClosed -= Hide;
            controller.OffersChanged -= Refresh;
            controller.OfferPurchased -= HandleOfferPurchased;
            GameManager.PlacePoint.OnValueChanged -= HandlePointsChanged;
        }

        private void Show()
        {
            visualRoot.SetActive(true);
            Refresh();
        }

        private void Hide()
        {
            foreach (Button button in buttonPool)
                button?.transform.DOKill(false);
            visualRoot.SetActive(false);
        }

        private void Refresh()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            EnsureButtonCount(offers.Count);
            for (int i = 0; i < buttonPool.Count; i++)
            {
                Button button = buttonPool[i];
                bool active = i < offers.Count && !controller.IsPurchased(i);
                button.gameObject.SetActive(active);
                if (!active)
                    continue;

                button.transform.DOKill(false);
                button.transform.localScale = Vector3.one;
                int offerIndex = i;
                ShopOfferData offer = offers[i];
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                label.text = $"{offer.DisplayName}\n{offer.Description}\n{offer.Cost:0} Point";
                button.interactable = GameManager.PlacePoint.CurrentValue >= offer.Cost;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => controller.TryPurchase(offerIndex));
            }
        }

        private void HandleOfferPurchased(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= buttonPool.Count)
                return;

            Button button = buttonPool[offerIndex];
            if (button == null || !button.gameObject.activeSelf)
                return;

            button.interactable = false;
            RectTransform rect = button.transform as RectTransform;
            rect.DOKill(false);
            Vector3 originalPosition = rect.localPosition;
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(rect)
                .Append(rect.DOScale(purchasedExitScale, purchasedExitDuration)
                    .SetEase(Ease.InBack))
                .Join(rect.DOLocalMoveY(
                    originalPosition.y - purchasedDropDistance,
                    purchasedExitDuration).SetEase(Ease.InCubic));
            sequence.OnComplete(() =>
            {
                if (rect == null)
                    return;

                button.gameObject.SetActive(false);
                rect.localScale = Vector3.one;
                rect.localPosition = originalPosition;
            });
        }

        private void HandleCloseClicked()
        {
            if (!controller.IsOpen)
                return;

            // The shop is visually gone before ShopClosed releases the pending Supply deal.
            Hide();
            controller.CloseShop();
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
                RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            int count = Mathf.Min(buttonPool.Count, offers.Count);
            for (int i = 0; i < count; i++)
            {
                if (!controller.IsPurchased(i) && buttonPool[i].gameObject.activeSelf)
                    buttonPool[i].interactable = GameManager.PlacePoint.CurrentValue >= offers[i].Cost;
            }
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(HandleCloseClicked);
            foreach (Button button in buttonPool)
                button?.transform.DOKill(false);
        }
    }
}
