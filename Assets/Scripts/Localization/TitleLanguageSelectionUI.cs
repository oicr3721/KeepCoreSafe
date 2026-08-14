using System.Collections.Generic;
using KeepCoreSafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.Localization
{
    public sealed class TitleLanguageSelectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private Button buttonTemplate;

        private readonly List<LanguageButtonView> languageButtons = new();
        private readonly List<string> buttonLocales = new();
        private bool isBuilt;

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += RefreshSelection;
            EnsureBuilt();
            RefreshSelection();
        }

        private void OnDisable()
        {
            LocalizationManager.LanguageChanged -= RefreshSelection;
        }

        public void Rebuild()
        {
            ClearGeneratedButtons();
            isBuilt = true;
            if (buttonRoot == null || buttonTemplate == null)
                return;

            IReadOnlyList<LocalizationLanguageInfo> languages =
                LocalizationManager.AvailableLanguages;
            for (int i = 0; i < languages.Count; i++)
            {
                LocalizationLanguageInfo language = languages[i];
                Button buttonInstance = Instantiate(buttonTemplate, buttonRoot);
                buttonInstance.name = $"Language Button - {language.Locale}";
                buttonInstance.gameObject.SetActive(true);
                buttonInstance.transform.SetSiblingIndex(buttonRoot.childCount - 1);

                if (!buttonInstance.TryGetComponent(out LanguageButtonView buttonView))
                {
                    Debug.LogError(
                        $"{nameof(TitleLanguageSelectionUI)} requires {nameof(buttonTemplate)} to contain a preconfigured {nameof(LanguageButtonView)} component.",
                        buttonTemplate);
                    Destroy(buttonInstance.gameObject);
                    continue;
                }

                languageButtons.Add(buttonView);
                buttonLocales.Add(language.Locale);

                string locale = language.Locale;
                buttonView.Bind(
                    language.DisplayName,
                    () => LocalizationManager.ChangeLanguage(locale));
            }

            RefreshSelection();
        }

        public void Refresh()
        {
            if (!isBuilt || languageButtons.Count != LocalizationManager.AvailableLanguages.Count)
                Rebuild();
            else
                RefreshSelection();
        }

        private void EnsureBuilt()
        {
            if (isBuilt)
                return;

            Rebuild();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < languageButtons.Count; i++)
            {
                if (languageButtons[i] == null || i >= buttonLocales.Count)
                    continue;

                languageButtons[i].SetInteractable(
                    buttonLocales[i] != LocalizationManager.CurrentLocale);
            }
        }

        private void ClearGeneratedButtons()
        {
            for (int i = languageButtons.Count - 1; i >= 0; i--)
            {
                if (languageButtons[i] != null)
                    Destroy(languageButtons[i].gameObject);
            }

            languageButtons.Clear();
            buttonLocales.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (buttonRoot == null)
                Debug.LogWarning($"{nameof(TitleLanguageSelectionUI)} on {name} needs a Button Root reference.", this);
            if (buttonTemplate == null)
                Debug.LogWarning($"{nameof(TitleLanguageSelectionUI)} on {name} needs a Button Template reference.", this);
        }
#endif
    }
}
