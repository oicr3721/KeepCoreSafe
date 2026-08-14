using TMPro;
using UnityEngine;

namespace KeepCoreSafe.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string localizationKey;
        [SerializeField] private string fallbackText;

        public string LocalizationKey => localizationKey;

        private void Awake()
        {
            if (label == null)
                label = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(fallbackText) && label != null)
                fallbackText = label.text;
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= Refresh;
        }

        public void SetKey(string key, string fallback = null)
        {
            localizationKey = key;
            if (fallback != null)
                fallbackText = fallback;
            Refresh();
        }

        public void Refresh()
        {
            if (label == null || string.IsNullOrWhiteSpace(localizationKey))
                return;

            label.text = LocalizationManager.Get(localizationKey, fallbackText);
        }
    }
}
