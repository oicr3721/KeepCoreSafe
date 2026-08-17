using System;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Localization;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PlacementController : MonoBehaviour
{
    [Serializable]
    private struct StartingBlock
    {
        public Vector2Int offset;
        public BasicBlockData data;
    }

    [Header("Preview")]
    [SerializeField] private SpriteRenderer previewRenderer;
    [SerializeField] private Color normalColor = new(1f, 1f, 1f, 0.55f);
    [SerializeField, HideInInspector] private Color invalidColor = new(1f, 0.2f, 0.2f, 0.55f);
    [SerializeField, Range(0f, 1f)] private float invalidBlinkMinimumAlpha = 0.12f;
    [SerializeField, Min(0.05f), Tooltip("Seconds for one full invalid-preview alpha pulse.")]
    private float invalidBlinkDuration = 0.6f;
    [SerializeField] private PlacementVisualizer effectVisualizer;

    [Header("Granted Blocks")]
    [SerializeField] private BlockSupplyController supplyController;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private BlockMatchData matchData;

    [Header("Merge Presentation")]
    [SerializeField] private MergePresentationController mergePresentation;

    [Header("Core")]
    [SerializeField] private CoreBlockData coreBlockData;
    [SerializeField] private bool useScriptedStartingBlocks;
    [SerializeField] private StartingBlock[] scriptedStartingBlocks = Array.Empty<StartingBlock>();

    [Header("Audio")]
    [Tooltip("Played after a player block is successfully placed on the Grid.")]
    [SerializeField] private AudioCue placementSound = new();
    [Tooltip("Played after the dismantle animation completes.")]
    [SerializeField] private AudioCue dismantleSound = new();

    private BlockSupplyController.GrantedBlock selectedGrant;
    private int selectedSupplyIndex = -1;
    private BlockMatchResolver matchResolver;
    private bool placementInputEnabled;
    private bool previewWasInvalid;
    private float invalidBlinkStartTime;

    public bool PlacementInputEnabled => placementInputEnabled;
    public int SelectedSupplyIndex => selectedSupplyIndex;
    public event Action<int> SelectionChanged;
    public event Func<int, BlockSupplyController.GrantedBlock, bool> GrantedBlockSelectionRequested;
    public event Func<BlockData, Vector2Int, bool> BlockPlacementValidationRequested;
    public event Action<BlockData, Vector2Int> BlockPlacementRejected;
    public event Action<Block, Vector2Int> BlockPlaced;
    public event Func<Block, Vector2Int, bool> BlockDismantleRequested;
    public event Action<Block, Vector2Int> BlockDismantled;
    public event Action<Block, Vector2Int> SkillBlockCreated;

    private void Start()
    {
        GameManager.PhaseChanged += OnPhaseChanged;
        if (supplyController != null)
            supplyController.SupplyChanged += HandleSupplyChanged;

        matchResolver = new BlockMatchResolver(GridManager.Instance, matchData);
        PlaceCoreAndStartingBlocks();
        HidePreview();
    }

    private void OnDestroy()
    {
        GameManager.PhaseChanged -= OnPhaseChanged;
        if (supplyController != null)
            supplyController.SupplyChanged -= HandleSupplyChanged;
    }

    private void Update()
    {
        if (Mouse.current == null || Camera.main == null || GridManager.Instance == null)
            return;

        if (!placementInputEnabled)
        {
            HidePreview();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HidePreview();
            return;
        }

        Vector2Int position = GridManager.Instance.WorldToGrid(GetMousePosition());
        if (!GridManager.Instance.Grid.IsWithinBounds(position))
        {
            HidePreview();
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryDismantle(position);
            return;
        }

        if (selectedSupplyIndex < 0 || selectedGrant.Data == null)
        {
            HidePreview();
            return;
        }

        UpdatePlacementPreview(position);
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceSelectedBlock(position);
    }

    public void SelectGrantedBlock(int supplyIndex)
    {
        if (!placementInputEnabled
            || supplyController == null
            || !supplyController.TryGet(supplyIndex, out BlockSupplyController.GrantedBlock grant))
        {
            ClearSelection();
            return;
        }

        if (!CanSelectGrantedBlock(supplyIndex, grant))
            return;

        selectedSupplyIndex = supplyIndex;
        selectedGrant = grant;
        previewRenderer.sprite = grant.Data.Sprite;
        previewRenderer.color = grant.Data.VisualColor;
        SelectionChanged?.Invoke(selectedSupplyIndex);
    }

    private bool CanSelectGrantedBlock(
        int supplyIndex,
        BlockSupplyController.GrantedBlock grant)
    {
        if (GrantedBlockSelectionRequested == null)
            return true;

        foreach (Delegate listener in GrantedBlockSelectionRequested.GetInvocationList())
        {
            if (listener is Func<int, BlockSupplyController.GrantedBlock, bool> validator
                && !validator(supplyIndex, grant))
            {
                return false;
            }
        }

        return true;
    }

    public void ClearSelection()
    {
        bool hadSelection = selectedSupplyIndex >= 0;
        selectedSupplyIndex = -1;
        selectedGrant = default;
        HidePreview();
        if (hadSelection)
            SelectionChanged?.Invoke(-1);
    }

    public void Confirm()
    {
        if (!placementInputEnabled)
            return;

        ClearSelection();
        GameManager.Instance.TryStartCombat();
    }

    public void SetPlacementInputEnabled(bool enabled)
    {
        placementInputEnabled = enabled && GameManager.Phase == GamePhase.Preparation;
        if (!placementInputEnabled)
        {
            ClearSelection();
        }
    }

    private void TryPlaceSelectedBlock(Vector2Int position)
    {
        if (!CanPlaceSelectedBlock(position))
        {
            BlockPlacementRejected?.Invoke(selectedGrant.Data, position);
            return;
        }

        Block block = CreateBlock(selectedGrant.Data);
        if (block == null)
            return;

        if (!GridManager.Instance.TryPlaceBlock(block, position))
        {
            Destroy(block.gameObject);
            return;
        }

        bool wasRare = selectedGrant.IsRare;
        if (supplyController == null
            || !supplyController.TryConsume(selectedSupplyIndex, out _))
        {
            GridManager.Instance.TryRemoveBlock(position, out _);
            Destroy(block.gameObject);
            ClearSelection();
            return;
        }

        block.PlayPlacementAnimation();
        AudioManager.PlayAt(placementSound, block.transform.position);
        if (wasRare)
            block.PlayRareAppearance();

        BlockPlaced?.Invoke(block, position);
        ClearSelection();
        ResolveMatch(position);
    }

    private void ResolveMatch(Vector2Int lastPlacedPosition)
    {
        if (matchResolver == null
            || !matchResolver.TryResolve(lastPlacedPosition, out BlockMatchResolver.MatchResult match))
        {
            return;
        }

        List<MergePresentationController.SourceVisual> sourceVisuals = new(match.ConsumedBlocks.Count);
        List<Vector2Int> lockedPositions = new(match.ConsumedBlocks.Count + 1);
        float mergedHealthRatio = CalculateAverageHealthRatio(match.ConsumedBlocks);
        foreach (Block consumedBlock in match.ConsumedBlocks)
        {
            if (consumedBlock == null)
                continue;

            sourceVisuals.Add(new MergePresentationController.SourceVisual(consumedBlock.VisualRenderer));
            lockedPositions.Add(consumedBlock.GridPosition);
        }

        lockedPositions.Add(match.Position);
        GridManager.InteractionLock interactionLock =
            GridManager.Instance.AcquireInteractionLock(lockedPositions);

        foreach (Block consumedBlock in match.ConsumedBlocks)
        {
            if (consumedBlock != null
                && GridManager.Instance.TryRemoveBlock(consumedBlock.GridPosition, out Block removed))
            {
                Destroy(removed.gameObject);
            }
        }

        Block resultBlock = CreateBlock(match.ResultBlock);
        if (resultBlock == null
            || !GridManager.Instance.TryPlaceBlock(resultBlock, match.Position))
        {
            if (resultBlock != null)
                Destroy(resultBlock.gameObject);
            interactionLock.Dispose();
            Debug.LogError("Failed to place the matched skill block.", this);
            return;
        }

        resultBlock.HP.SetValue(resultBlock.HP.MaxValue * mergedHealthRatio);
        SkillBlockCreated?.Invoke(resultBlock, match.Position);
        bool presentationStarted = mergePresentation != null
            && mergePresentation.Play(
                sourceVisuals,
                resultBlock.transform.position,
                resultBlock,
                interactionLock,
                () =>
                {
                    if (resultBlock != null)
                        AudioManager.PlayAt(placementSound, resultBlock.transform.position);
                });
        if (presentationStarted)
            return;

        interactionLock.Dispose();
        resultBlock.PlayPlacementAnimation();
        AudioManager.PlayAt(placementSound, resultBlock.transform.position);
        resultBlock.PlayRareAppearance();
    }

    private static float CalculateAverageHealthRatio(IReadOnlyList<Block> sourceBlocks)
    {
        if (sourceBlocks == null || sourceBlocks.Count == 0)
            return 1f;

        float ratioSum = 0f;
        int validBlockCount = 0;
        foreach (Block block in sourceBlocks)
        {
            if (block == null || block.HP.MaxValue <= 0f)
                continue;

            ratioSum += Mathf.Clamp01(block.HP.CurrentValue / block.HP.MaxValue);
            validBlockCount++;
        }

        return validBlockCount > 0
            ? ratioSum / validBlockCount
            : 1f;
    }

    private void TryDismantle(Vector2Int position)
    {
        if (GridManager.Instance.IsInteractionLocked(position)
            || !GridManager.Instance.TryGetBlock(position, out Block block)
            || !CanDismantle(block, position)
            || !GridManager.Instance.TryRemoveBlock(position, out block))
        {
            return;
        }

        BlockDismantled?.Invoke(block, position);
        block.PlayDismantleAnimation(() =>
        {
            AudioManager.PlayAt(dismantleSound, block.transform.position);
            Destroy(block.gameObject);
        });
    }

    private bool CanDismantle(Block block, Vector2Int position)
    {
        if (BlockDismantleRequested == null)
            return true;

        foreach (Delegate listener in BlockDismantleRequested.GetInvocationList())
        {
            if (listener is Func<Block, Vector2Int, bool> validator
                && !validator(block, position))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePlacementPreview(Vector2Int position)
    {
        Vector3 worldPosition = GridManager.Instance.GridToWorld(position);
        previewRenderer.gameObject.SetActive(true);
        bool canPlace = CanPlaceSelectedBlock(position);
        if (!canPlace && !previewWasInvalid)
            invalidBlinkStartTime = Time.unscaledTime;
        previewWasInvalid = !canPlace;
        float previewAlpha = canPlace
            ? normalColor.a
            : GetInvalidPreviewAlpha();
        previewRenderer.color = WithPreviewAlpha(
            selectedGrant.Data.VisualColor,
            previewAlpha);
        previewRenderer.transform.position = worldPosition;
        if (canPlace)
        {
            effectVisualizer?.ShowPlacement(
                selectedGrant.Data,
                worldPosition,
                GridManager.Instance.CellSize);
        }
        else
        {
            effectVisualizer?.HidePlacement();
        }
    }

    private bool CanPlaceSelectedBlock(Vector2Int position)
    {
        if (selectedGrant.Data == null
            || GridManager.Instance.IsInteractionLocked(position)
            || IsNextWaveSpawnCell(position)
            || !GridManager.Instance.IsCellEmpty(position))
        {
            return false;
        }

        if (BlockPlacementValidationRequested == null)
            return true;

        foreach (Delegate listener in BlockPlacementValidationRequested.GetInvocationList())
        {
            if (listener is Func<BlockData, Vector2Int, bool> validator
                && !validator(selectedGrant.Data, position))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsNextWaveSpawnCell(Vector2Int position)
    {
        return waveManager != null && waveManager.IsSpawnCellReserved(position);
    }

    private Vector3 GetMousePosition()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Preparation)
        {
            gameObject.SetActive(true);
            placementInputEnabled = false;
        }
        else
        {
            placementInputEnabled = false;
            ClearSelection();
            gameObject.SetActive(false);
        }
    }

    private void HandleSupplyChanged(bool _)
    {
        ClearSelection();
    }

    private void HidePreview()
    {
        previewWasInvalid = false;
        if (previewRenderer != null)
            previewRenderer.gameObject.SetActive(false);
        effectVisualizer?.HidePlacement();
    }

    private Block CreateBlock(BlockData data)
    {
        if (data == null || data.Prefab == null)
        {
            Debug.LogError($"{data?.name ?? "BlockData"} has no Block prefab assigned.", data);
            return null;
        }

        Block block = Instantiate(data.Prefab);
        block.name = data.DisplayName;
        block.Initialize(data);
        return block;
    }

    private void PlaceCoreAndStartingBlocks()
    {
        if (GridManager.Instance.Grid.Core != null)
            return;

        if (coreBlockData == null)
            coreBlockData = Resources.Load<CoreBlockData>("Data/Block/CoreData");
        if (coreBlockData == null)
        {
            Debug.LogError("CoreData is not assigned or available in Resources/Data.");
            return;
        }

        Vector2Int center = new(
            GridManager.Instance.Width / 2,
            GridManager.Instance.Height / 2);
        Block core = CreateBlock(coreBlockData);
        if (core == null)
            return;

        if (!GridManager.Instance.TryPlaceBlock(core, center))
        {
            Destroy(core.gameObject);
            Debug.LogError("Failed to place the Core at the Grid center.");
            return;
        }

        PlaceStartingBasicBlocks(center);
    }

    private void PlaceStartingBasicBlocks(Vector2Int corePosition)
    {
        if (useScriptedStartingBlocks)
        {
            foreach (StartingBlock entry in scriptedStartingBlocks)
                PlaceStartingBlock(corePosition + entry.offset, entry.data);
            return;
        }

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = corePosition + direction;
            BlockData data = supplyController?.GetRandomBasicBlock();
            if (data is not BasicBlockData
                || !GridManager.Instance.Grid.IsWithinBounds(position)
                || !GridManager.Instance.IsCellEmpty(position))
            {
                continue;
            }

            Block block = CreateBlock(data);
            if (block != null && !GridManager.Instance.TryPlaceBlock(block, position))
                Destroy(block.gameObject);
        }
    }

    private void PlaceStartingBlock(Vector2Int position, BlockData data)
    {
        if (data == null
            || !GridManager.Instance.Grid.IsWithinBounds(position)
            || !GridManager.Instance.IsCellEmpty(position))
        {
            return;
        }

        Block block = CreateBlock(data);
        if (block != null && !GridManager.Instance.TryPlaceBlock(block, position))
            Destroy(block.gameObject);
    }

    private static Color WithPreviewAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private float GetInvalidPreviewAlpha()
    {
        float maximumAlpha = normalColor.a;
        float minimumAlpha = Mathf.Min(invalidBlinkMinimumAlpha, maximumAlpha);
        float duration = Mathf.Max(0.05f, invalidBlinkDuration);
        float elapsed = Mathf.Max(0f, Time.unscaledTime - invalidBlinkStartTime);
        float pingPong = Mathf.PingPong(elapsed * 2f / duration, 1f);
        return Mathf.Lerp(maximumAlpha, minimumAlpha, pingPong);
    }
}
