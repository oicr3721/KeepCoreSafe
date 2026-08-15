using System.Collections.Generic;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Localization;
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
        [SerializeField] private string rerollAvailableKey = "ui.reroll.available";

        [Header("Confirm")]
        [SerializeField] private Button confirmButton;

        [Header("Start Wave")]
        [SerializeField] private Button startWaveButton;
        [FormerlySerializedAs("startWaveButtonUI")]
        [SerializeField] private UIShowHide startWaveButtonVisibility;

        private readonly List<SupplyBlockButtonView> buttonPool = new();
        private readonly List<Button> activeButtons = new();
        private readonly List<bool> activeRareFlags = new();
        private bool startWaveAllowed = true;

        private void Start()
        {
            if (inventoryRoot == null)
                inventoryRoot = transform;
            if (supplyController != null)
                supplyController.SupplyChanged += Refresh;
            if (placementController != null)
                placementController.SelectionChanged += HandleSelectionChanged;
            if (rerollButton != null)
                rerollButton.onClick.AddListener(HandleReroll);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(HandleStartWave);
            GameManager.PhaseChanged += HandlePhaseChanged;
            LocalizationManager.LanguageChanged += HandleLanguageChanged;

            placementController?.SetPlacementInputEnabled(false);
            startWaveButtonVisibility?.Hide(true);

            Refresh(true);
        }

        private void OnDestroy()
        {
            if (supplyController != null)
                supplyController.SupplyChanged -= Refresh;
            if (placementController != null)
                placementController.SelectionChanged -= HandleSelectionChanged;
            if (rerollButton != null)
                rerollButton.onClick.RemoveListener(HandleReroll);
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirm);
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(HandleStartWave);
            GameManager.PhaseChanged -= HandlePhaseChanged;
            LocalizationManager.LanguageChanged -= HandleLanguageChanged;
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
                SupplyBlockButtonView buttonView = buttonPool[i];
                bool active = i < granted.Count;
                buttonView.gameObject.SetActive(active);
                if (!active)
                    continue;

                activeButtons.Add(buttonView.Button);

                int supplyIndex = i;
                BlockSupplyController.GrantedBlock item = granted[i];
                activeRareFlags.Add(item.IsRare);
                buttonView.Bind(
                    item.Data,
                    descriptionTooltip,
                    () => placementController.SelectGrantedBlock(supplyIndex));
            }

            HandleSelectionChanged(placementController != null
                ? placementController.SelectedSupplyIndex
                : -1);

            if (supplyPresentation != null)
            {
                if (playAppearance && !supplyPresentation.IsDocked)
                {
                    placementController?.SetPlacementInputEnabled(false);
                    startWaveButtonVisibility?.Hide(true);
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
            if (blockButtonPrefab == null)
                return;

            while (buttonPool.Count < count)
            {
                Button button = Instantiate(blockButtonPrefab, inventoryRoot);
                button.transform.SetSiblingIndex(buttonPool.Count);
                if (!button.TryGetComponent(out SupplyBlockButtonView view))
                {
                    Debug.LogError(
                        $"{nameof(PreparationUI)} requires {nameof(blockButtonPrefab)} to contain a preconfigured {nameof(SupplyBlockButtonView)} component.",
                        blockButtonPrefab);
                    Destroy(button.gameObject);
                    break;
                }

                buttonPool.Add(view);
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
                    startWaveButtonVisibility?.Show();
                return;
            }

            supplyPresentation.PlayConfirm(activeButtons, () =>
            {
                placementController?.SetPlacementInputEnabled(true);
                if (startWaveAllowed)
                    startWaveButtonVisibility?.Show();
                RefreshReroll();
            });
        }

        private void HandleStartWave()
        {
            if (!startWaveAllowed || GameManager.Phase != GamePhase.Preparation)
                return;

            startWaveButtonVisibility?.Hide();
            placementController?.Confirm();
        }

        public void SetStartWaveAllowed(bool allowed)
        {
            startWaveAllowed = allowed;
            if (allowed && supplyPresentation != null && supplyPresentation.IsDocked)
                startWaveButtonVisibility?.Show();
            else if (!allowed)
                startWaveButtonVisibility?.Hide(true);
        }

        private void HandleLanguageChanged()
        {
            Refresh(false);
        }

        private void HandleSelectionChanged(int selectedIndex)
        {
            for (int i = 0; i < buttonPool.Count; i++)
            {
                SupplyBlockButtonView view = buttonPool[i];
                view.SetSelected(view.gameObject.activeSelf && i == selectedIndex);
            }
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
                rerollLabel.text =
                    $"{LocalizationManager.Get(rerollAvailableKey)} ({supplyController.NextRerollCost})";
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Preparation)
            {
                placementController?.SetPlacementInputEnabled(false);
                startWaveButtonVisibility?.Hide(true);
                return;
            }

            placementController?.SetPlacementInputEnabled(false);
            startWaveButtonVisibility?.Hide();
            supplyPresentation?.Hide();
        }
    }
}
