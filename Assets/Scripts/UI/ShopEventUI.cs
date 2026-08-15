using System.Collections.Generic;
using DG.Tweening;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Localization;
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
        [SerializeField] private CanvasGroup offerVisualGroup;

        [Header("Card Reveal")]
        [SerializeField, Min(0.05f)] private float cardRevealDuration = 0.32f;
        [SerializeField, Min(0f)] private float revealStagger = 0.12f;
        [SerializeField, Min(0f)] private float revealSlideDistance = 90f;

        [Header("Card Floating")]
        [SerializeField, Min(0f)] private float floatingPhaseOffset = 0.18f;

        [Header("Selection Feedback")]
        [SerializeField, Min(0.05f)] private float selectionDuration = 0.48f;
        [SerializeField, Min(0f)] private float selectionHoldDuration = 0.42f;
        [SerializeField, Min(0f)] private float unselectedBackwardOffset = 24f;
        [SerializeField, Min(0.05f)] private float exitDuration = 0.24f;
        [SerializeField, Min(0f)] private float exitStagger = 0.1f;
        [SerializeField, Min(0f)] private float exitDistance = 120f;
        [SerializeField, Min(0f)] private float backgroundFadeDuration = 0.14f;
        [SerializeField] private AudioCue purchaseSuccessSound = new();

        private readonly List<Button> buttonPool = new();
        private readonly List<ShopOfferCardView> cardPool = new();
        private Sequence cardSequence;
        private bool isAnimatingCards;

        private void Awake()
        {
            visualRoot.SetActive(false);
            if (offerVisualGroup == null && visualRoot != null)
                offerVisualGroup = visualRoot.GetComponentInChildren<CanvasGroup>();
        }

        private void OnEnable()
        {
            controller.ShopOpened += Show;
            controller.ShopClosing += Hide;
            controller.OffersChanged += HandleOffersChanged;
            controller.OfferSelected += HandleOfferSelected;
            LocalizationManager.LanguageChanged += RefreshVisibleText;
        }

        private void OnDisable()
        {
            controller.ShopOpened -= Show;
            controller.ShopClosing -= Hide;
            controller.OffersChanged -= HandleOffersChanged;
            controller.OfferSelected -= HandleOfferSelected;
            LocalizationManager.LanguageChanged -= RefreshVisibleText;
        }

        private void Show()
        {
            visualRoot.SetActive(true);
            if (offerVisualGroup != null)
            {
                offerVisualGroup.alpha = 0f;
                offerVisualGroup.interactable = true;
                offerVisualGroup.blocksRaycasts = true;
                offerVisualGroup.DOFade(1f, backgroundFadeDuration).SetUpdate(true);
            }
            PrepareCardsBack();
            PlayRevealSequence();
        }

        private void Hide()
        {
            KillCardSequence();
            foreach (ShopOfferCardView card in cardPool)
            {
                if (card == null)
                    continue;

                card.StopFloating(true);
                card.gameObject.SetActive(false);
            }
            if (offerVisualGroup != null)
            {
                offerVisualGroup.blocksRaycasts = false;
                offerVisualGroup.interactable = false;
            }
            visualRoot.SetActive(false);
        }

        private void HandleOffersChanged()
        {
            if (visualRoot.activeSelf && controller.IsOpen)
                PlayRerollSequence();
            else
                PrepareCardsBack();
        }

        private void HandleOfferSelected(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= buttonPool.Count)
                return;

            ShopOfferCardView card = cardPool[offerIndex];
            if (card == null || !card.gameObject.activeSelf)
                return;

            if (offerVisualGroup != null)
            {
                offerVisualGroup.interactable = false;
                offerVisualGroup.blocksRaycasts = false;
            }

            PlaySelectionSequence(offerIndex);
        }

        private void EnsureButtonCount(int count)
        {
            if (offerButtonPrefab == null)
                return;

            while (cardPool.Count < count)
            {
                GameObject instance = Instantiate(offerButtonPrefab, offerRoot);
                if (!instance.TryGetComponent(out ShopOfferCardView card))
                {
                    Debug.LogError(
                        $"{nameof(ShopEventUI)} requires {nameof(offerButtonPrefab)} to contain a preconfigured {nameof(ShopOfferCardView)} component.",
                        offerButtonPrefab);
                    Destroy(instance);
                    break;
                }

                cardPool.Add(card);
                buttonPool.Add(card.Button);
            }
        }

        private void RefreshInteractable()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            int count = Mathf.Min(cardPool.Count, offers.Count);
            for (int i = 0; i < count; i++)
            {
                if (cardPool[i] == null || !cardPool[i].gameObject.activeSelf)
                    continue;

                cardPool[i].SetAffordable(controller.CanSelectOffer(i));
                cardPool[i].SetInputEnabled(!isAnimatingCards);
            }
        }

        private void PrepareCardsBack()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            EnsureButtonCount(offers.Count);
            for (int i = 0; i < cardPool.Count; i++)
            {
                ShopOfferCardView card = cardPool[i];
                bool active = i < offers.Count;
                card.gameObject.SetActive(active);
                if (!active)
                    continue;

                ApplyOfferToCard(card, offers[i], i);
                card.PrepareSupplyReveal(revealSlideDistance);
                card.SetAffordable(true);
                card.SetInputEnabled(false);
            }
        }

        private void ApplyCurrentOffersToBackCards()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            EnsureButtonCount(offers.Count);
            for (int i = 0; i < cardPool.Count; i++)
            {
                bool active = i < offers.Count;
                cardPool[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                ApplyOfferToCard(cardPool[i], offers[i], i);
                cardPool[i].SetAffordable(true);
            }
        }

        private void ApplyOfferToCard(ShopOfferCardView card, ShopOfferData offer, int offerIndex)
        {
            card.SetInfo(offer.DisplayImage, offer.DisplayName, offer.Description);

            Button button = card.Button;
            if (button == null)
                return;

            int capturedIndex = offerIndex;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleCardClicked(capturedIndex));
        }

        private void HandleCardClicked(int offerIndex)
        {
            if (isAnimatingCards || !controller.IsOpen)
                return;

            if (offerIndex >= 0 && offerIndex < cardPool.Count)
                cardPool[offerIndex]?.PlayClickPopup();

            if (!controller.CanSelectOffer(offerIndex))
                return;

            if (controller.TrySelectOffer(offerIndex))
                AudioManager.Play(purchaseSuccessSound);
        }

        private void RefreshVisibleText()
        {
            IReadOnlyList<ShopOfferData> offers = controller.CurrentOffers;
            int count = Mathf.Min(cardPool.Count, offers.Count);
            for (int i = 0; i < count; i++)
            {
                if (cardPool[i] != null && cardPool[i].gameObject.activeSelf)
                    ApplyOfferToCard(cardPool[i], offers[i], i);
            }
        }

        private void PlayRevealSequence()
        {
            KillCardSequence();
            isAnimatingCards = true;
            SetCardsInput(false);
            cardSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            int activeCount = GetActiveCardCount();
            for (int i = 0; i < activeCount; i++)
            {
                ShopOfferCardView card = cardPool[i];
                cardSequence.Insert(i * revealStagger, card.PlaySupplyReveal(cardRevealDuration));
            }

            cardSequence.OnComplete(() =>
            {
                isAnimatingCards = false;
                StartFloating();
                RefreshInteractable();
            });
        }

        private void PlayRerollSequence()
        {
            KillCardSequence();
            EnsureButtonCount(controller.CurrentOffers.Count);
            isAnimatingCards = true;
            SetCardsInput(false);
            foreach (ShopOfferCardView card in cardPool)
            {
                if (card != null && card.gameObject.activeSelf)
                    card.PrepareForReroll();
            }

            cardSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            Sequence backSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            foreach (ShopOfferCardView card in cardPool)
            {
                if (card != null && card.gameObject.activeSelf)
                    backSequence.Join(card.FlipToBack(cardRevealDuration));
            }

            cardSequence
                .Append(backSequence)
                .AppendCallback(ApplyCurrentOffersToBackCards);

            Sequence revealSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            int activeCount = controller.CurrentOffers.Count;
            for (int i = 0; i < activeCount; i++)
            {
                ShopOfferCardView card = cardPool[i];
                revealSequence.Insert(i * revealStagger, card.PlaySupplyReveal(cardRevealDuration));
            }
            cardSequence.Append(revealSequence);

            cardSequence.OnComplete(() =>
            {
                isAnimatingCards = false;
                StartFloating();
                RefreshInteractable();
            });
        }

        private void StartFloating()
        {
            int activeCount = GetActiveCardCount();
            for (int i = 0; i < activeCount; i++)
                cardPool[i].StartFloating(i * floatingPhaseOffset);
        }

        private void SetCardsInput(bool enabled)
        {
            foreach (ShopOfferCardView card in cardPool)
            {
                if (card != null)
                    card.SetInputEnabled(enabled);
            }
        }

        private int GetActiveCardCount()
        {
            int count = 0;
            foreach (ShopOfferCardView card in cardPool)
            {
                if (card != null && card.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private void KillCardSequence()
        {
            cardSequence?.Kill(false);
            cardSequence = null;
            isAnimatingCards = false;
        }

        private void PlaySelectionSequence(int selectedIndex)
        {
            KillCardSequence();
            isAnimatingCards = true;
            SetCardsInput(false);
            cardSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (int i = 0; i < GetActiveCardCount(); i++)
            {
                Tween emphasis = i == selectedIndex
                    ? cardPool[i].PlaySupplySelected(selectionDuration)
                    : cardPool[i].PlaySupplyUnselected(selectionDuration * 0.7f, unselectedBackwardOffset);
                if (emphasis != null)
                    cardSequence.Join(emphasis);
            }

            cardSequence.AppendInterval(selectionHoldDuration);
            int activeCount = GetActiveCardCount();
            // Every exit is scheduled from one fixed origin so the full-duration tweens overlap.
            float exitStartTime = cardSequence.Duration();
            for (int i = 0; i < activeCount; i++)
            {
                Tween exit = cardPool[i].PlaySupplyExit(exitDuration, exitDistance);
                if (exit != null)
                    cardSequence.Insert(exitStartTime + i * exitStagger, exit);
            }

            if (offerVisualGroup != null)
                cardSequence.Append(offerVisualGroup.DOFade(0f, backgroundFadeDuration));
            cardSequence.OnComplete(controller.CloseShop);
        }

        private void OnDestroy()
        {
            KillCardSequence();
            foreach (ShopOfferCardView card in cardPool)
                card?.StopFloating(true);
        }
    }
}
