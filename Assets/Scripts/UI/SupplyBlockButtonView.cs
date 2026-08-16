using System;
using KeepCoreSafe.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class SupplyBlockButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private BlockButtonTooltipTrigger tooltipTrigger;

        [Header("Selection")]
        [SerializeField] private Image selectionHighlight;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.9f;
        [SerializeField, Min(0f)] private float selectionDuration = 0.14f;
        [SerializeField, Min(1f)] private float selectedHighlightScale = 1.12f;

        public Button Button => button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (icon == null)
                icon = GetComponent<Image>();
            if (tooltipTrigger == null)
                tooltipTrigger = GetComponent<BlockButtonTooltipTrigger>();
        }

        public void Bind(
            BlockData data,
            BlockDescriptionTooltip tooltip,
            Action onClicked)
        {
            if (label != null)
                label.text = data != null ? data.DisplayName : string.Empty;
            if (icon != null && data != null)
            {
                icon.sprite = data.Sprite;
                icon.SetNativeSize();
                icon.color = data.VisualColor;
            }

            tooltipTrigger?.Initialize(data, tooltip);
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (onClicked != null)
                button.onClick.AddListener(() => onClicked());
        }

        public void SetSelected(bool selected, bool immediate = false)
        {
            if (selectionHighlight == null)
                return;

            RectTransform highlightTransform = selectionHighlight.rectTransform;
            selectionHighlight.DOKill();
            highlightTransform.DOKill();

            float targetAlpha = selected ? selectedAlpha : 0f;
            float targetScale = selected ? selectedHighlightScale : 1f;
            if (immediate || selectionDuration <= 0f)
            {
                Color color = selectionHighlight.color;
                color.a = targetAlpha;
                selectionHighlight.color = color;
                highlightTransform.localScale = Vector3.one * targetScale;
                return;
            }

            selectionHighlight.DOFade(targetAlpha, selectionDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(selectionHighlight);
            highlightTransform.DOScale(targetScale, selectionDuration)
                .SetEase(selected ? Ease.OutBack : Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(highlightTransform);
        }

        private void OnDisable()
        {
            if (selectionHighlight == null)
                return;

            selectionHighlight.DOKill();
            selectionHighlight.rectTransform.DOKill();
            Color color = selectionHighlight.color;
            color.a = 0f;
            selectionHighlight.color = color;
            selectionHighlight.rectTransform.localScale = Vector3.one;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null
                || icon == null
                || label == null
                || tooltipTrigger == null
                || selectionHighlight == null)
            {
                Debug.LogWarning(
                    $"{nameof(SupplyBlockButtonView)} on {name} has missing prefab references.",
                    this);
            }
        }
#endif
    }
}
