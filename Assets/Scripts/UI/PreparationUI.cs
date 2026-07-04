using KeepCoreSafe.Data;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace KeepCoreSafe.UI
{
    public class PreparationUI : MonoBehaviour
    {
        [SerializeField] private PlacementController controller;

        [SerializeField] private Button blockButtonPrefab;

        [SerializeField] private List<BlockData> blockDatas = new();

        [SerializeField] private BlockDescriptionTooltip descriptionTooltip;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            CreateBlockButtons();
        }

        void CreateBlockButtons()
        {
            foreach (var bd in blockDatas)
            {
                Button button = Instantiate(blockButtonPrefab, transform);
                button.GetComponentInChildren<TMP_Text>().text = bd.DisplayName;
                button.GetComponent<Image>().sprite = bd.Sprite;

                BlockButtonTooltipTrigger tooltipTrigger =
                    button.GetComponent<BlockButtonTooltipTrigger>();
                if (tooltipTrigger != null)
                    tooltipTrigger.Initialize(bd, descriptionTooltip);
                else
                    Debug.LogError($"{button.name} prefab has no tooltip trigger.", button);

                button.onClick.AddListener(() => controller.SelectBlock(bd));
            }
        }


    }

}
