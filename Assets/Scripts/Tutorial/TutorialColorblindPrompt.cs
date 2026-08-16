using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.Tutorial
{
    public sealed class TutorialColorblindPrompt : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button declineButton;
        [SerializeField, Min(0f)] private float animationDuration = 0.16f;

        private Action<bool> completion;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            applyButton?.onClick.AddListener(Apply);
            declineButton?.onClick.AddListener(Decline);
        }

        private void OnDisable()
        {
            applyButton?.onClick.RemoveListener(Apply);
            declineButton?.onClick.RemoveListener(Decline);
            canvasGroup?.DOKill();
            panel?.DOKill();
        }

        public void Show(Action<bool> onComplete)
        {
            completion = onComplete;
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.DOFade(1f, animationDuration).SetUpdate(true).SetTarget(this);
            }
            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.92f;
                panel.DOScale(1f, animationDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetTarget(this);
            }
        }

        private void Apply() => Resolve(true);
        private void Decline() => Resolve(false);

        private void Resolve(bool enabled)
        {
            Action<bool> callback = completion;
            completion = null;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
            callback?.Invoke(enabled);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (canvasGroup == null || panel == null || applyButton == null || declineButton == null)
                Debug.LogWarning($"{nameof(TutorialColorblindPrompt)} on {name} has missing references.", this);
        }
#endif
    }
}
