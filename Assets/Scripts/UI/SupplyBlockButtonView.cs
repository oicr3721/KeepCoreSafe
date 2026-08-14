using System;
using KeepCoreSafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class SupplyBlockButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private BlockButtonTooltipTrigger tooltipTrigger;

        public Button Button => button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (icon == null)
                icon = GetComponent<Image>();
            if (tooltipTrigger == null)
                tooltipTrigger = GetComponent<BlockButtonTooltipTrigger>();
        }

        public void Bind(
            BlockData data,
            BlockDescriptionTooltip tooltip,
            Action onClicked)
        {
            if (label != null)
                label.text = data != null ? data.DisplayName : string.Empty;
            if (icon != null && data != null)
            {
                icon.sprite = data.Sprite;
                icon.color = data.VisualColor;
            }

            tooltipTrigger?.Initialize(data, tooltip);
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (onClicked != null)
                button.onClick.AddListener(() => onClicked());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null || icon == null || label == null || tooltipTrigger == null)
            {
                Debug.LogWarning(
                    $"{nameof(SupplyBlockButtonView)} on {name} has missing prefab references.",
                    this);
            }
        }
#endif
    }
}
