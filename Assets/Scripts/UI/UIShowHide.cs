using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class UIShowHide : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [FormerlySerializedAs("button")]
        [SerializeField] private Selectable interactableTarget;

        [Header("Transition")]
        [SerializeField] private Vector2 hiddenOffset = new(35f, 0f);
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField, Min(0f)] private float hideDuration = 0.12f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Vector2 shownPosition;
        private Sequence transition;

        private void Awake()
        {
            shownPosition = visualRoot != null ? visualRoot.anchoredPosition : Vector2.zero;
            Hide(true);
        }

        public Tween Show(bool immediate = false)
        {
            if (visualRoot == null || canvasGroup == null)
                return null;

            KillTransition();
            gameObject.SetActive(true);
            visualRoot.anchoredPosition = shownPosition + hiddenOffset;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            if (interactableTarget != null)
                interactableTarget.interactable = immediate;

            if (immediate)
            {
                visualRoot.anchoredPosition = shownPosition;
                canvasGroup.alpha = 1f;
                return null;
            }

            transition = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(visualRoot.DOAnchorPos(shownPosition, showDuration).SetEase(showEase))
                .Join(canvasGroup.DOFade(1f, showDuration))
                .OnComplete(() =>
                {
                    if (interactableTarget != null)
                        interactableTarget.interactable = true;
                });

            return transition;
        }

        public Tween Hide(bool immediate = false)
        {
            if (visualRoot == null || canvasGroup == null)
                return null;

            KillTransition();
            if (interactableTarget != null)
                interactableTarget.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (immediate || !gameObject.activeInHierarchy)
            {
                canvasGroup.alpha = 0f;
                visualRoot.anchoredPosition = shownPosition + hiddenOffset;
                return null;
            }

            transition = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(visualRoot.DOAnchorPos(shownPosition + hiddenOffset, hideDuration).SetEase(hideEase))
                .Join(canvasGroup.DOFade(0f, hideDuration));

            return transition;
        }

        private void KillTransition()
        {
            transition?.Kill(false);
            transition = null;
            visualRoot?.DOKill(false);
            canvasGroup?.DOKill(false);
        }

        private void OnDestroy()
        {
            KillTransition();
            this.DOKill(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (visualRoot == null || canvasGroup == null)
            {
                Debug.LogWarning(
                    $"{nameof(UIShowHide)} on {name} has missing prefab or scene references.",
                    this);
            }
        }
#endif
    }
}
