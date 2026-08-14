using DG.Tweening;
using KeepCoreSafe.Data;
using KeepCoreSafe.Localization;
using System.Text;
using TMPro;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class BlockDescriptionTooltip : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text detailsLabel;
        [SerializeField] private Vector2 pointerOffset = new(18f, -18f);
        [SerializeField, Min(0f)] private float fadeDuration = 0.12f;

        private object currentOwner;

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += RefreshCurrent;
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= RefreshCurrent;
        }

        private BlockData currentData;

        public void Show(object owner, BlockData data, Vector2 screenPosition)
        {
            if (owner == null || data == null)
                return;

            currentOwner = owner;
            currentData = data;
            RefreshLabels(data);
            gameObject.SetActive(true);
            SetPosition(owner, screenPosition);
            canvasGroup.DOKill(false);
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }

        public void SetPosition(object owner, Vector2 screenPosition)
        {
            if (owner == null || currentOwner != owner || canvas == null || panel == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPosition);

            Vector2 desired = localPosition + pointerOffset;
            Vector2 halfSize = panel.rect.size * 0.5f;
            Rect bounds = canvasRect.rect;
            desired.x = Mathf.Clamp(desired.x, bounds.xMin + halfSize.x, bounds.xMax - halfSize.x);
            desired.y = Mathf.Clamp(desired.y, bounds.yMin + halfSize.y, bounds.yMax - halfSize.y);
            panel.anchoredPosition = desired;
        }

        public void Hide(object owner)
        {
            if (owner == null || currentOwner != owner)
                return;

            currentOwner = null;
            currentData = null;
            canvasGroup.DOKill(false);
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void RefreshCurrent()
        {
            if (currentOwner != null && currentData != null)
                RefreshLabels(currentData);
        }

        private void RefreshLabels(BlockData data)
        {
            titleLabel.text = data.DisplayName;
            descriptionLabel.text = data.Description;
            if (detailsLabel != null)
                detailsLabel.text = BuildDetails(data);
        }

        private static string BuildDetails(BlockData data)
        {
            StringBuilder builder = new();
            builder.Append(LocalizationManager.Format(
                "tooltip.maxHp",
                data.MaxHP));

            switch (data)
            {
                case BasicBlockData basic when basic.Color != null:
                    builder.Append('\n');
                    builder.Append(LocalizationManager.Format(
                        "tooltip.basicBlock",
                        new object[] { basic.Color.DisplayName }));
                    break;
                case AttackBlockData attack:
                    builder.Append('\n');
                    builder.Append(LocalizationManager.Format(
                        "tooltip.attackDetails",
                        attack.AttackValue,
                        attack.ActionCooldown));
                    break;
                case HealerBlockData healer:
                    builder.Append('\n');
                    builder.Append(LocalizationManager.Format(
                        "tooltip.healerDetails",
                        healer.HealValue,
                        healer.ActionCooldown));
                    break;
                case SupportBlockData support:
                    float reduction = (1f - support.CooldownMultiplier) * 100f;
                    builder.Append('\n');
                    builder.Append(LocalizationManager.Format(
                        "tooltip.supportDetails",
                        reduction));
                    break;
            }

            if (data.EffectRange > 0f && data.AffectedDirections != AdjacencyDirection.None)
            {
                builder.Append('\n');
                builder.Append(LocalizationManager.Format(
                    "tooltip.effectRange",
                    data.EffectRange));
            }

            return builder.ToString();
        }

        private void OnDestroy()
        {
            canvasGroup?.DOKill(false);
        }
    }
}
