using System;
using System.Collections.Generic;
using DG.Tweening;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class SupplyPresentationUI : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private RectTransform presentationRoot;
        [SerializeField] private CanvasGroup presentationGroup;
        [SerializeField] private RectTransform backgroundPanel;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform blockContainer;
        [SerializeField] private RectTransform dockTarget;

        [Header("Controls")]
        [SerializeField] private RectTransform controlsRoot;
        [SerializeField] private CanvasGroup controlsGroup;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button rerollButton;
        [SerializeField, Min(0f)] private float controlsFadeInDuration = 0.18f;
        [SerializeField, Min(0f)] private float controlsFadeOutDuration = 0.14f;

        [Header("Audio")]
        [Tooltip("Played whenever a supplied block finishes landing.")]
        [SerializeField] private AudioCue blockLandingSound = new();
        [Tooltip("Played with the rare appearance effect after landing.")]
        [SerializeField] private AudioCue rareBlockSound = new();
        [Tooltip("A short confirmation thunk played before the Presentation docks.")]
        [SerializeField] private AudioCue confirmSound = new();

        [Header("Deal Animation")]
        [SerializeField, Min(1f)] private float itemSpacing = 118f;
        [SerializeField, Min(0f)] private float itemRevealDuration = 0.18f;
        [SerializeField, Min(0f)] private float rearrangeDuration = 0.16f;
        [SerializeField, Min(0f)] private float itemInterval = 0.055f;
        [SerializeField, Min(0f)] private float landingShake = 5f;

        [Header("Reroll Animation")]
        [SerializeField, Min(0f)] private float rerollExitDuration = 0.14f;
        [SerializeField, Min(0f)] private float rerollStagger = 0.025f;

        [Header("Dock Animation")]
        [SerializeField, Min(0f)] private float confirmFeedbackDelay = 0.11f;
        [SerializeField, Min(0f)] private float dockDuration = 0.34f;
        [SerializeField] private Color expandedBackgroundColor = new(0.015f, 0.03f, 0.05f, 0.82f);
        [SerializeField] private Color dockedBackgroundColor = new(0.035f, 0.08f, 0.1f, 0.94f);

        private readonly List<Button> currentButtons = new();
        private Sequence sequence;
        private Vector2 expandedContentPosition;
        private Vector2 expandedContentSizeDelta;
        private Vector2 expandedContentAnchorMin;
        private Vector2 expandedContentAnchorMax;
        private Vector2 expandedContentPivot;
        private bool isAnimating;
        private bool isDocked;
        private bool isDealReady;

        public bool IsAnimating => isAnimating;
        public bool IsDocked => isDocked;
        public bool CanReroll => isDealReady && !isDocked && !isAnimating;

        private void Awake()
        {
            if (presentationRoot == null)
                presentationRoot = transform as RectTransform;
            if (contentRoot != null)
            {
                expandedContentPosition = contentRoot.anchoredPosition;
                expandedContentSizeDelta = contentRoot.sizeDelta;
                expandedContentAnchorMin = contentRoot.anchorMin;
                expandedContentAnchorMax = contentRoot.anchorMax;
                expandedContentPivot = contentRoot.pivot;
            }
        }

        public void PlayDeal(
            IReadOnlyList<Button> buttons,
            IReadOnlyList<bool> rareFlags,
            Action onComplete)
        {
            KillSequence();
            Show();
            isAnimating = true;
            isDocked = false;
            isDealReady = false;
            CacheButtons(buttons);
            SetExpandedLayout();
            SetControlsInteractable(false);
            SetBlockButtonsInteractable(false);

            sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (int i = 0; i < currentButtons.Count; i++)
            {
                int revealIndex = i;
                Button button = currentButtons[i];
                RectTransform rect = button.transform as RectTransform;
                CanvasGroup group = button.GetComponent<CanvasGroup>();
                rect.DOKill(false);
                group?.DOKill(false);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one * 0.3f;
                if (group != null)
                    group.alpha = 0f;

                sequence.AppendCallback(() =>
                {
                    if (group != null)
                        group.alpha = 1f;
                });

                for (int layoutIndex = 0; layoutIndex <= revealIndex; layoutIndex++)
                {
                    RectTransform layoutRect = currentButtons[layoutIndex].transform as RectTransform;
                    sequence.Join(layoutRect.DOAnchorPos(
                        GetCenteredPosition(layoutIndex, revealIndex + 1),
                        rearrangeDuration).SetEase(Ease.OutCubic));
                }

                sequence.Join(rect.DOScale(1f, itemRevealDuration).SetEase(Ease.OutBack));
                sequence.AppendCallback(() => AudioManager.Play(blockLandingSound));
                sequence.Append(rect.DOShakeAnchorPos(
                    itemRevealDuration * 0.45f,
                    landingShake,
                    10,
                    65f,
                    false,
                    true));
                if (rareFlags != null
                    && revealIndex < rareFlags.Count
                    && rareFlags[revealIndex])
                {
                    sequence.AppendCallback(() =>
                    {
                        button.GetComponent<RareBlockAppearance>()?.Play();
                        AudioManager.Play(rareBlockSound);
                    });
                }

                sequence.AppendInterval(itemInterval);
            }

            if (controlsGroup != null)
                sequence.Append(controlsGroup.DOFade(1f, controlsFadeInDuration));

            sequence.OnComplete(() =>
            {
                isAnimating = false;
                isDealReady = true;
                SetControlsInteractable(true);
                SetBlockButtonsInteractable(false);
                onComplete?.Invoke();
            });
        }

        public void PlayRerollOut(IReadOnlyList<Button> buttons, Action onComplete)
        {
            if (!CanReroll)
                return;

            KillSequence();
            isAnimating = true;
            isDealReady = false;
            SetControlsInteractable(false);
            CacheButtons(buttons);

            sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (int i = 0; i < currentButtons.Count; i++)
            {
                RectTransform rect = currentButtons[i].transform as RectTransform;
                CanvasGroup group = currentButtons[i].GetComponent<CanvasGroup>();
                Tween scale = rect.DOScale(0f, rerollExitDuration).SetEase(Ease.InBack);
                if (i == 0)
                    sequence.Append(scale);
                else
                    sequence.Insert(i * rerollStagger, scale);
                if (group != null)
                    sequence.Insert(i * rerollStagger, group.DOFade(0f, rerollExitDuration));
            }

            sequence.OnComplete(() =>
            {
                isAnimating = false;
                onComplete?.Invoke();
            });
        }

        public void PlayConfirm(IReadOnlyList<Button> buttons, Action onComplete)
        {
            if (!isDealReady || isAnimating || isDocked)
                return;

            KillSequence();
            CacheButtons(buttons);
            isAnimating = true;
            isDealReady = false;
            SetControlsInteractable(false);
            SetBlockButtonsInteractable(false);
            AudioManager.Play(confirmSound);

            GetDockRect(out Vector2 dockPosition, out Vector2 dockSize);
            GetMatchingLayoutInCurrentAnchorSpace(
                contentRoot,
                dockTarget,
                out Vector2 contentPosition,
                out Vector2 contentSizeDelta);
            sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            if (controlsGroup != null)
                sequence.Append(controlsGroup.DOFade(0f, controlsFadeOutDuration));
            sequence.AppendInterval(confirmFeedbackDelay)
                .Append(backgroundPanel.DOAnchorPos(dockPosition, dockDuration).SetEase(Ease.InOutCubic))
                .Join(backgroundPanel.DOSizeDelta(dockSize, dockDuration).SetEase(Ease.InOutCubic))
                .Join(backgroundImage.DOColor(dockedBackgroundColor, dockDuration))
                .Join(contentRoot.DOAnchorPos(contentPosition, dockDuration).SetEase(Ease.InOutCubic))
                .Join(contentRoot.DOSizeDelta(contentSizeDelta, dockDuration).SetEase(Ease.InOutCubic));

            sequence.OnComplete(() =>
            {
                CopyLayout(contentRoot, dockTarget);
                isAnimating = false;
                isDocked = true;
                if (backgroundImage != null)
                    backgroundImage.raycastTarget = false;
                HideControls();
                SetBlockButtonsInteractable(true);
                onComplete?.Invoke();
            });
        }

        public void RefreshDockedLayout(IReadOnlyList<Button> buttons)
        {
            CacheButtons(buttons);
            for (int i = 0; i < currentButtons.Count; i++)
            {
                RectTransform rect = currentButtons[i].transform as RectTransform;
                rect.DOKill(false);
                rect.DOAnchorPos(GetCenteredPosition(i, currentButtons.Count), rearrangeDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
                rect.DOScale(1f, rearrangeDuration).SetUpdate(true);
                CanvasGroup group = currentButtons[i].GetComponent<CanvasGroup>();
                if (group != null)
                    group.alpha = 1f;
            }

            SetBlockButtonsInteractable(isDocked && !isAnimating);
        }

        public void Hide()
        {
            KillSequence();
            isAnimating = false;
            isDealReady = false;
            isDocked = false;
            if (presentationGroup != null)
            {
                presentationGroup.alpha = 0f;
                presentationGroup.interactable = false;
                presentationGroup.blocksRaycasts = false;
            }
            HideControls();
        }

        private void Show()
        {
            if (presentationGroup == null)
                return;

            presentationGroup.alpha = 1f;
            presentationGroup.interactable = true;
            presentationGroup.blocksRaycasts = true;
        }

        private void SetExpandedLayout()
        {
            Canvas.ForceUpdateCanvases();
            Rect rootRect = presentationRoot.rect;
            backgroundPanel.anchorMin = backgroundPanel.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundPanel.anchoredPosition = Vector2.zero;
            backgroundPanel.sizeDelta = rootRect.size;
            backgroundImage.color = expandedBackgroundColor;
            backgroundImage.raycastTarget = true;
            contentRoot.anchorMin = expandedContentAnchorMin;
            contentRoot.anchorMax = expandedContentAnchorMax;
            contentRoot.pivot = expandedContentPivot;
            contentRoot.anchoredPosition = expandedContentPosition;
            contentRoot.sizeDelta = expandedContentSizeDelta;
            if (controlsRoot != null)
                controlsRoot.gameObject.SetActive(true);
            if (controlsGroup != null)
            {
                controlsGroup.DOKill(false);
                controlsGroup.alpha = 0f;
                controlsGroup.interactable = false;
                controlsGroup.blocksRaycasts = false;
            }
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(true);
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (controlsGroup != null)
            {
                controlsGroup.interactable = interactable;
                controlsGroup.blocksRaycasts = interactable;
            }
            if (confirmButton != null)
                confirmButton.interactable = interactable;
            if (rerollButton != null)
                rerollButton.interactable = interactable;
        }

        private void HideControls()
        {
            if (controlsGroup != null)
            {
                controlsGroup.DOKill(false);
                controlsGroup.alpha = 0f;
                controlsGroup.interactable = false;
                controlsGroup.blocksRaycasts = false;
            }
            if (controlsRoot != null)
                controlsRoot.gameObject.SetActive(false);
        }

        private void SetBlockButtonsInteractable(bool interactable)
        {
            foreach (Button button in currentButtons)
            {
                if (button != null)
                    button.interactable = interactable;
            }
        }

        private void CacheButtons(IReadOnlyList<Button> buttons)
        {
            currentButtons.Clear();
            if (buttons == null)
                return;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.activeSelf)
                    currentButtons.Add(buttons[i]);
            }
        }

        private Vector2 GetCenteredPosition(int index, int count)
        {
            return new Vector2((index - (count - 1) * 0.5f) * itemSpacing, 0f);
        }

        private void GetDockRect(out Vector2 position, out Vector2 size)
        {
            Vector3[] corners = new Vector3[4];
            dockTarget.GetWorldCorners(corners);
            Vector3 min = presentationRoot.InverseTransformPoint(corners[0]);
            Vector3 max = presentationRoot.InverseTransformPoint(corners[2]);
            position = (min + max) * 0.5f;
            size = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
        }

        private static void GetMatchingLayoutInCurrentAnchorSpace(
            RectTransform rect,
            RectTransform target,
            out Vector2 anchoredPosition,
            out Vector2 sizeDelta)
        {
            RectTransform parent = rect.parent as RectTransform;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 minimum = parent.InverseTransformPoint(corners[0]);
            Vector2 maximum = parent.InverseTransformPoint(corners[2]);
            Vector2 scaledSize = maximum - minimum;
            Vector2 scale = new(
                Mathf.Max(0.0001f, Mathf.Abs(rect.localScale.x)),
                Mathf.Max(0.0001f, Mathf.Abs(rect.localScale.y)));
            Vector2 localSize = new(scaledSize.x / scale.x, scaledSize.y / scale.y);
            Vector2 pivotPosition = minimum + Vector2.Scale(scaledSize, rect.pivot);
            Vector2 anchorReference = parent.rect.min + Vector2.Scale(
                parent.rect.size,
                rect.anchorMin + Vector2.Scale(rect.anchorMax - rect.anchorMin, rect.pivot));

            anchoredPosition = pivotPosition - anchorReference;
            sizeDelta = localSize - Vector2.Scale(
                parent.rect.size,
                rect.anchorMax - rect.anchorMin);
        }

        private static void CopyLayout(RectTransform destination, RectTransform source)
        {
            destination.anchorMin = source.anchorMin;
            destination.anchorMax = source.anchorMax;
            destination.pivot = source.pivot;
            destination.anchoredPosition = source.anchoredPosition;
            destination.sizeDelta = source.sizeDelta;
        }

        private void KillSequence()
        {
            sequence?.Kill(false);
            sequence = null;
            this.DOKill(false);
        }

        private void OnDestroy()
        {
            KillSequence();
        }
    }
}
