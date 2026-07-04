using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class GameDefaultUI : MonoBehaviour
    {
        public static Transform BlockHPBarRoot;

        [SerializeField] private CountTextUI placePointUI;
        [SerializeField] private Button timeScaleButton;
        [SerializeField] private TMP_Text timeScaleText;
        [SerializeField] private Transform blockHPBarRoot;

        private void Awake()
        {
            if (blockHPBarRoot != null)
                BlockHPBarRoot = blockHPBarRoot;
            else
                BlockHPBarRoot = transform;
        }

        void Start()
        {
            placePointUI.Initialize(GameManager.PlacePoint);
            if (timeScaleButton != null)
                timeScaleButton.onClick.AddListener(GameManager.Instance.CycleTimeScale);

            GameManager.TimeScaleChanged += RefreshTimeScale;
            RefreshTimeScale(GameManager.Instance.CurrentTimeScale);
        }

        private void OnDestroy()
        {
            if (timeScaleButton != null && GameManager.Instance != null)
                timeScaleButton.onClick.RemoveListener(GameManager.Instance.CycleTimeScale);

            GameManager.TimeScaleChanged -= RefreshTimeScale;
        }

        private void RefreshTimeScale(float scale)
        {
            if (timeScaleText != null)
                timeScaleText.text = $"{scale:0}x";
        }
    }
}

