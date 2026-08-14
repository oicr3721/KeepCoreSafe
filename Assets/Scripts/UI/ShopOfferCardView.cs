using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopOfferCardView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject cardBack;
        [SerializeField] private GameObject cardFront;
        [SerializeField] private Image cardBackImage;
        [SerializeField] private Image cardFrontBackground;
        [SerializeField] private Image selectedMask;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup selectedMaskGroup;
        [SerializeField] private ShopOfferCardMotion motion;

        [Header("Visual Style")]
        [SerializeField] private Color frontColor = new(0.156f, 0.139f, 0.198f, 0.92f);
        [SerializeField] private Color backColor = new(0.08f, 0.11f, 0.18f, 0.96f);
        [SerializeField] private Color selectedMaskColor = new(0.18f, 0.18f, 0.2f, 0.52f);
        [SerializeField, Range(0f, 1f)] private float selectedMaskAlpha = 0.52f;

        private bool inputEnabled;
        private bool isAffordable = true;
        private bool isRevealed;
        private bool isSelected;

        public Button Button => button;
        public TMP_Text Label => label;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            ApplyStaticVisualStyle();
            ShowBackImmediate();
        }

        public void SetText(string text)
        {
            if (label != null)
                label.text = text;
        }

        public void SetAffordable(bool affordable)
        {
            isAffordable = affordable;
            RefreshButtonState();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            motion?.SetInputEnabled(enabled);
            RefreshButtonState();
        }

        public void ShowBackImmediate()
        {
            isSelected = false;
            isRevealed = false;
            motion?.ShowBackImmediate();
            SetFrontVisible(false);
            RefreshSelectedMask(true);
            RefreshButtonState();
        }

        public void ShowFrontImmediate(bool selected)
        {
            isSelected = selected;
            isRevealed = true;
            motion?.ShowFrontImmediate(selected);
            SetFrontVisible(true);
            RefreshSelectedMask(true);
            RefreshButtonState();
        }

        public Tween FlipToFront(float duration = -1f)
        {
            if (motion == null)
            {
                Debug.LogError($"{nameof(ShopOfferCardView)} on {name} needs a {nameof(ShopOfferCardMotion)} reference.", this);
                return DOTween.Sequence().SetUpdate(true);
            }

            SetInputEnabled(false);
            SetFrontVisible(false);
            return motion.FlipToFront(
                () => SetFrontVisible(true),
                () =>
                {
                    isRevealed = true;
                    RefreshButtonState();
                },
                duration);
        }

        public Tween FlipToBack(float duration = -1f)
        {
            if (motion == null)
            {
                Debug.LogError($"{nameof(ShopOfferCardView)} on {name} needs a {nameof(ShopOfferCardMotion)} reference.", this);
                return DOTween.Sequence().SetUpdate(true);
            }

            SetInputEnabled(false);
            SetFrontVisible(true);
            return motion.FlipToBack(
                () =>
                {
                    isSelected = false;
                    RefreshSelectedMask(true);
                    SetFrontVisible(false);
                },
                () =>
                {
                    isRevealed = false;
                    RefreshButtonState();
                },
                duration);
        }

        public void StartFloating(float phaseOffset)
        {
            motion?.StartFloating(phaseOffset);
        }

        public void StopFloating(bool resetPosition)
        {
            motion?.StopFloating(resetPosition);
        }

        public void PrepareForReroll()
        {
            SetInputEnabled(false);
            motion?.PrepareForReroll();
        }

        public void MarkSelected()
        {
            if (!isRevealed || isSelected)
                return;

            isSelected = true;
            inputEnabled = false;
            RefreshSelectedMask(false);
            RefreshButtonState();
        }

        public void PlayClickPopup()
        {
            if (!isRevealed || isSelected)
                return;

            motion?.PlayClickPopup();
        }

        public void PrepareSupplyReveal(float verticalOffset)
        {
            ShowFrontImmediate(false);
            SetInputEnabled(false);
            motion?.PrepareSupplyReveal(verticalOffset);
        }

        public Tween PlaySupplyReveal(float duration) => motion?.PlaySupplyReveal(duration);
        public Tween PlaySupplySelected(float duration) => motion?.PlaySupplySelected(duration);
        public Tween PlaySupplyUnselected(float duration, float offset) =>
            motion?.PlaySupplyUnselected(duration, offset);
        public Tween PlaySupplyExit(float duration, float distance) =>
            motion?.PlaySupplyExit(duration, distance);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanUseHover())
                motion?.PointerEnter(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (CanUseHover())
                motion?.PointerMove(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            motion?.PointerExit();
        }

        private void ApplyStaticVisualStyle()
        {
            if (cardBackImage != null)
            {
                cardBackImage.color = backColor;
                cardBackImage.raycastTarget = false;
            }

            if (cardFrontBackground != null)
            {
                cardFrontBackground.color = frontColor;
                cardFrontBackground.raycastTarget = false;
            }

            if (selectedMask != null)
            {
                selectedMask.color = new Color(
                    selectedMaskColor.r,
                    selectedMaskColor.g,
                    selectedMaskColor.b,
                    selectedMaskAlpha);
                selectedMask.raycastTarget = false;
            }
        }

        private void SetFrontVisible(bool visible)
        {
            if (cardFront != null)
                cardFront.SetActive(visible);
            if (cardBack != null)
                cardBack.SetActive(!visible);
        }

        private void RefreshSelectedMask(bool immediate)
        {
            if (selectedMaskGroup == null)
                return;

            float alpha = isSelected ? 1f : 0f;
            selectedMaskGroup.DOKill(false);
            if (immediate)
                selectedMaskGroup.alpha = alpha;
            else
                selectedMaskGroup.DOFade(alpha, 0.14f).SetUpdate(true).SetTarget(this);
        }

        private void RefreshButtonState()
        {
            if (button != null)
                button.interactable = inputEnabled && isRevealed && !isSelected;
        }

        private bool CanUseHover()
        {
            return inputEnabled && isRevealed && !isSelected;
        }

        private void OnDisable()
        {
            motion?.KillTweens();
            if (selectedMaskGroup != null)
                selectedMaskGroup.DOKill(false);
        }

        private void OnDestroy()
        {
            motion?.KillTweens();
            if (selectedMaskGroup != null)
                selectedMaskGroup.DOKill(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cardBack == null || cardFront == null || cardBackImage == null
                || cardFrontBackground == null || selectedMask == null
                || label == null || selectedMaskGroup == null || motion == null)
            {
                Debug.LogWarning(
                    $"{nameof(ShopOfferCardView)} on {name} has missing prefab references.",
                    this);
            }
        }
#endif
    }
}
