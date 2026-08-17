using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.Analytics
{
    public sealed class AnalyticsConsentPrompt : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button allowButton;
        [SerializeField] private Button declineButton;
        [SerializeField, Min(0f)] private float animationDuration = 0.16f;

        private Action completion;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            allowButton?.onClick.AddListener(Allow);
            declineButton?.onClick.AddListener(Decline);
        }

        private void OnDisable()
        {
            allowButton?.onClick.RemoveListener(Allow);
            declineButton?.onClick.RemoveListener(Decline);
            canvasGroup?.DOKill();
            panel?.DOKill();
        }

        public void Show(Action onComplete)
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

        private void Allow() => Resolve(true);
        private void Decline() => Resolve(false);

        private void Resolve(bool granted)
        {
            AnalyticsConsentSettings.SetGranted(granted);
            Action callback = completion;
            completion = null;
            callback?.Invoke();
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (canvasGroup == null || panel == null || allowButton == null || declineButton == null)
                Debug.LogWarning($"{nameof(AnalyticsConsentPrompt)} on {name} has missing references.", this);
        }
#endif
    }
}
