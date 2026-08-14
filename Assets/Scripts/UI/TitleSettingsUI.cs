using KeepCoreSafe.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class TitleSettingsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject settingsWindow;
        [SerializeField] private UIShowHide settingsWindowVisibility;
        [SerializeField] private TitleLanguageSelectionUI languageSelectionUI;

        private void Awake()
        {
            if (settingsWindowVisibility != null)
                settingsWindowVisibility.Hide(true);
            else if (settingsWindow != null)
                settingsWindow.SetActive(false);
        }

        private void OnEnable()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(Open);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(Open);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (settingsWindow == null)
                return;

            if (settingsWindowVisibility != null)
                settingsWindowVisibility.Show();
            else
                settingsWindow.SetActive(true);
            languageSelectionUI?.Refresh();
        }

        public void Close()
        {
            if (settingsWindowVisibility != null)
                settingsWindowVisibility.Hide();
            else if (settingsWindow != null)
                settingsWindow.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (settingsButton == null)
                Debug.LogWarning($"{nameof(TitleSettingsUI)} on {name} needs a Settings Button reference.", this);
            if (closeButton == null)
                Debug.LogWarning($"{nameof(TitleSettingsUI)} on {name} needs a Close Button reference.", this);
            if (settingsWindow == null)
                Debug.LogWarning($"{nameof(TitleSettingsUI)} on {name} needs a Settings Window reference.", this);
        }
#endif
    }
}
