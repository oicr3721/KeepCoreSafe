using DG.Tweening;
using KeepCoreSafe.Analytics;
using KeepCoreSafe.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform pausePanel;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject warningRoot;
        [SerializeField] private RectTransform warningPanel;
        [SerializeField] private Button confirmTitleButton;
        [SerializeField] private Button cancelButton;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float animationDuration = 0.16f;

        private float timeScaleBeforePause = 1f;
        private bool isPaused;
        private bool isReturningToTitle;

        private void Awake()
        {
            titleButton?.onClick.AddListener(OpenWarning);
            closeButton?.onClick.AddListener(Close);
            confirmTitleButton?.onClick.AddListener(ReturnToTitle);
            cancelButton?.onClick.AddListener(CloseWarning);
            visualRoot?.SetActive(false);
            warningRoot?.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true || isReturningToTitle)
                return;

            if (warningRoot != null && warningRoot.activeSelf)
            {
                CloseWarning();
                return;
            }

            if (isPaused)
                Close();
            else if (CanOpen())
                Open();
        }

        private static bool CanOpen()
        {
            return GameManager.Instance == null || GameManager.Phase != GamePhase.GameOver;
        }

        private void Open()
        {
            if (isPaused || visualRoot == null)
                return;

            isPaused = true;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            warningRoot?.SetActive(false);
            visualRoot.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.DOFade(1f, animationDuration).SetUpdate(true).SetTarget(this);
            }
            AnimatePanel(pausePanel);
        }

        public void Close()
        {
            if (!isPaused || isReturningToTitle)
                return;

            isPaused = false;
            warningRoot?.SetActive(false);
            visualRoot?.SetActive(false);
            Time.timeScale = timeScaleBeforePause;
        }

        private void OpenWarning()
        {
            if (!isPaused || warningRoot == null)
                return;

            warningRoot.SetActive(true);
            AnimatePanel(warningPanel);
        }

        private void CloseWarning()
        {
            warningRoot?.SetActive(false);
        }

        private void ReturnToTitle()
        {
            if (isReturningToTitle)
                return;

            isReturningToTitle = true;
            AnalyticsService.GameAbandoned();
            Time.timeScale = 1f;
            SceneLoader.Load(SceneType.Title);
        }

        private void AnimatePanel(RectTransform panel)
        {
            if (panel == null)
                return;
            panel.DOKill(false);
            panel.localScale = Vector3.one * 0.94f;
            panel.DOScale(1f, animationDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void OnDisable()
        {
            canvasGroup?.DOKill(false);
            pausePanel?.DOKill(false);
            warningPanel?.DOKill(false);
            if (isPaused && !isReturningToTitle)
                Time.timeScale = timeScaleBeforePause;
            isPaused = false;
        }

        private void OnDestroy()
        {
            titleButton?.onClick.RemoveListener(OpenWarning);
            closeButton?.onClick.RemoveListener(Close);
            confirmTitleButton?.onClick.RemoveListener(ReturnToTitle);
            cancelButton?.onClick.RemoveListener(CloseWarning);
            this.DOKill(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (visualRoot == null
                || canvasGroup == null
                || pausePanel == null
                || titleButton == null
                || closeButton == null
                || warningRoot == null
                || warningPanel == null
                || confirmTitleButton == null
                || cancelButton == null)
            {
                Debug.LogWarning($"{nameof(PauseMenuUI)} on {name} has missing references.", this);
            }
        }
#endif
    }
}
