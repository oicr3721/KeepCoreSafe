using System.Collections.Generic;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class PreparationUI : MonoBehaviour
    {
        [Header("Controllers")]
        [FormerlySerializedAs("controller")]
        [SerializeField] private PlacementController placementController;
        [SerializeField] private BlockSupplyController supplyController;

        [Header("Granted Block List")]
        [SerializeField] private Button blockButtonPrefab;
        [SerializeField] private Transform inventoryRoot;
        [SerializeField] private BlockDescriptionTooltip descriptionTooltip;
        [SerializeField] private SupplyPresentationUI supplyPresentation;

        [Header("Reroll")]
        [SerializeField] private Button rerollButton;
        [SerializeField] private TMP_Text rerollLabel;
        [SerializeField] private string rerollFormat = "Reroll {0:0}";
        [SerializeField] private string rerollLockedText = "Reroll Locked";

        [Header("Confirm")]
        [SerializeField] private Button confirmButton;

        [Header("Start Wave")]
        [SerializeField] private Button startWaveButton;
        [SerializeField] private StartWaveButtonUI startWaveButtonUI;

        private readonly List<Button> buttonPool = new();
        private readonly List<Button> activeButtons = new();
        private readonly List<bool> activeRareFlags = new();
        private bool startWaveAllowed = true;

        private void Start()
        {
            if (inventoryRoot == null)
                inventoryRoot = transform;
            if (supplyController != null)
                supplyController.SupplyChanged += Refresh;
            if (rerollButton != null)
                rerollButton.onClick.AddListener(HandleReroll);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(HandleStartWave);
            GameManager.PlacePoint.OnValueChanged += HandlePointsChanged;
            GameManager.PhaseChanged += HandlePhaseChanged;

            placementController?.SetPlacementInputEnabled(false);
            startWaveButtonUI?.Hide(true);

            Refresh(true);
        }

        private void OnDestroy()
        {
            if (supplyController != null)
                supplyController.SupplyChanged -= Refresh;
            if (rerollButton != null)
                rerollButton.onClick.RemoveListener(HandleReroll);
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirm);
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(HandleStartWave);
            GameManager.PlacePoint.OnValueChanged -= HandlePointsChanged;
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }

        private void Refresh(bool playAppearance)
        {
            if (supplyController == null || blockButtonPrefab == null)
                return;

            IReadOnlyList<BlockSupplyController.GrantedBlock> granted =
                supplyController.GrantedBlocks;
            EnsureButtonCount(granted.Count);
            activeButtons.Clear();
            activeRareFlags.Clear();

            for (int i = 0; i < buttonPool.Count; i++)
            {
                Button button = buttonPool[i];
                bool active = i < granted.Count;
                button.gameObject.SetActive(active);
                if (!active)
                    continue;

                activeButtons.Add(button);

                int supplyIndex = i;
                BlockSupplyController.GrantedBlock item = granted[i];
                activeRareFlags.Add(item.IsRare);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                Image image = button.GetComponent<Image>();
                label.text = item.Data.DisplayName;
                image.sprite = item.Data.Sprite;
                image.color = item.Data.VisualColor;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => placementController.SelectGrantedBlock(supplyIndex));

                BlockButtonTooltipTrigger tooltip =
                    button.GetComponent<BlockButtonTooltipTrigger>();
                tooltip?.Initialize(item.Data, descriptionTooltip);

            }

            if (supplyPresentation != null)
            {
                if (playAppearance && !supplyPresentation.IsDocked)
                {
                    placementController?.SetPlacementInputEnabled(false);
                    startWaveButtonUI?.Hide(true);
                    supplyPresentation.PlayDeal(activeButtons, activeRareFlags, RefreshReroll);
                }
                else
                {
                    supplyPresentation.RefreshDockedLayout(activeButtons);
                }
            }

            RefreshReroll();
        }

        private void EnsureButtonCount(int count)
        {
            while (buttonPool.Count < count)
            {
                Button button = Instantiate(blockButtonPrefab, inventoryRoot);
                button.transform.SetSiblingIndex(buttonPool.Count);
                buttonPool.Add(button);
            }
        }

        private void HandleReroll()
        {
            if (supplyController == null)
                return;

            if (supplyPresentation == null)
            {
                supplyController.TryReroll();
                RefreshReroll();
                return;
            }

            supplyPresentation.PlayRerollOut(activeButtons, () =>
            {
                if (!supplyController.TryReroll())
                    Refresh(true);
            });
        }

        private void HandleConfirm()
        {
            if (supplyPresentation == null)
            {
                placementController?.SetPlacementInputEnabled(true);
                if (startWaveAllowed)
                    startWaveButtonUI?.Show();
                return;
            }

            supplyPresentation.PlayConfirm(activeButtons, () =>
            {
                placementController?.SetPlacementInputEnabled(true);
                if (startWaveAllowed)
                    startWaveButtonUI?.Show();
                RefreshReroll();
            });
        }

        private void HandleStartWave()
        {
            if (!startWaveAllowed || GameManager.Phase != GamePhase.Preparation)
                return;

            startWaveButtonUI?.Hide();
            placementController?.Confirm();
        }

        public void SetStartWaveAllowed(bool allowed)
        {
            startWaveAllowed = allowed;
            if (allowed && supplyPresentation != null && supplyPresentation.IsDocked)
                startWaveButtonUI?.Show();
            else if (!allowed)
                startWaveButtonUI?.Hide(true);
        }

        private void HandlePointsChanged(float _, float __)
        {
            RefreshReroll();
        }

        private void RefreshReroll()
        {
            if (supplyController == null)
                return;

            if (rerollButton != null)
            {
                rerollButton.interactable = supplyController.CanReroll
                                            && (supplyPresentation == null
                                                || supplyPresentation.CanReroll);
            }
            if (rerollLabel != null)
            {
                rerollLabel.text = supplyController.CanReroll
                    ? string.Format(rerollFormat, supplyController.CurrentRerollCost)
                    : rerollLockedText;
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Preparation)
            {
                placementController?.SetPlacementInputEnabled(false);
                startWaveButtonUI?.Hide(true);
                return;
            }

            placementController?.SetPlacementInputEnabled(false);
            startWaveButtonUI?.Hide();
            supplyPresentation?.Hide();
        }
    }
}
