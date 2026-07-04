using DG.Tweening;
using KeepCoreSafe.Data;
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

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private object currentOwner;

        public void Show(object owner, BlockData data, Vector2 screenPosition)
        {
            if (owner == null || data == null)
                return;

            currentOwner = owner;
            titleLabel.text = data.DisplayName;
            descriptionLabel.text = data.Description;
            if (detailsLabel != null)
                detailsLabel.text = BuildDetails(data);
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
            canvasGroup.DOKill(false);
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private static string BuildDetails(BlockData data)
        {
            StringBuilder builder = new();
            builder.Append($"최대 HP {data.MaxHP}");

            switch (data)
            {
                case BasicBlockData basic when basic.Color != null:
                    builder.Append($"\n기본 블록 · {basic.Color.DisplayName}");
                    break;
                case AttackBlockData attack:
                    builder.Append($"\n공격 {attack.AttackValue}  |  주기 {attack.ActionCooldown:0.##}초");
                    break;
                case HealerBlockData healer:
                    builder.Append($"\n회복 {healer.HealValue}  |  주기 {healer.ActionCooldown:0.##}초");
                    break;
                case SupportBlockData support:
                    float reduction = (1f - support.CooldownMultiplier) * 100f;
                    builder.Append($"\n재사용 대기시간 감소 {reduction:0}%");
                    break;
            }

            if (data.EffectRange > 0f && data.AffectedDirections != AdjacencyDirection.None)
                builder.Append($"\n효과 범위 {data.EffectRange:0.#}칸");
            return builder.ToString();
        }

        private void OnDestroy()
        {
            canvasGroup?.DOKill(false);
        }
    }
}
