using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class LanguageButtonView : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public Button Button => button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Bind(string displayName, Action onClicked)
        {
            if (label != null)
                label.text = displayName;

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (onClicked != null)
                button.onClick.AddListener(() => onClicked());
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null || label == null)
            {
                Debug.LogWarning(
                    $"{nameof(LanguageButtonView)} on {name} has missing prefab references.",
                    this);
            }
        }
#endif
    }
}
