using System;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
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
    [SerializeField] private Color invalidColor = new(1f, 0.2f, 0.2f, 0.55f);
    [SerializeField] private PlacementVisualizer effectVisualizer;

    [Header("Granted Blocks")]
    [SerializeField] private BlockSupplyController supplyController;
    [SerializeField] private BlockMatchData matchData;

    [Header("Dismantle Preview")]
    [SerializeField] private RectTransform placementControlText;
    [SerializeField] private TMP_Text dismantleRefundText;
    [SerializeField, Range(0f, 1f)] private float dismantleRefundRate = 0.5f;
    [SerializeField] private Vector2 dismantlePreviewOffset = new(22f, 26f);

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

    public bool PlacementInputEnabled => placementInputEnabled;
    public event Action<Block, Vector2Int> BlockPlaced;
    public event Action<Block, Vector2Int> BlockDismantled;
    public event Action<Block, Vector2Int> SkillBlockCreated;

    private void Start()
    {
        GameManager.PhaseChanged += OnPhaseChanged;
        if (supplyController != null)
            supplyController.SupplyChanged += HandleSupplyChanged;

        matchResolver = new BlockMatchResolver(GridManager.Instance, matchData);
        PlaceCoreAndStartingBlocks();
        SetRefundPreviewVisible(false);
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
            SetRefundPreviewVisible(false);
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HidePreview();
            SetRefundPreviewVisible(false);
            return;
        }

        Vector2Int position = GridManager.Instance.WorldToGrid(GetMousePosition());
        if (!GridManager.Instance.Grid.IsWithinBounds(position))
        {
            HidePreview();
            SetRefundPreviewVisible(false);
            return;
        }

        UpdateDismantlePreview(position);
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

        selectedSupplyIndex = supplyIndex;
        selectedGrant = grant;
        previewRenderer.sprite = grant.Data.Sprite;
        previewRenderer.color = grant.Data.VisualColor;
    }

    public void ClearSelection()
    {
        selectedSupplyIndex = -1;
        selectedGrant = default;
        HidePreview();
    }

    public void Confirm()
    {
        if (!placementInputEnabled)
            return;

        ClearSelection();
        SetRefundPreviewVisible(false);
        GameManager.Instance.TryStartCombat();
    }

    public void SetPlacementInputEnabled(bool enabled)
    {
        placementInputEnabled = enabled && GameManager.Phase == GamePhase.Preparation;
        if (!placementInputEnabled)
        {
            ClearSelection();
            SetRefundPreviewVisible(false);
        }
    }

    public static float CalculateDismantleRefund(Block block, float refundRate = 0.5f)
    {
        if (block == null || block.HP.MaxValue <= 0f)
            return 0f;

        float healthRatio = block.HP.CurrentValue / block.HP.MaxValue;
        return Mathf.FloorToInt(block.DismantleValue * refundRate * healthRatio);
    }

    private void TryPlaceSelectedBlock(Vector2Int position)
    {
        if (!GridManager.Instance.IsCellEmpty(position))
            return;

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
            Debug.LogError("Failed to place the matched skill block.", this);
            return;
        }

        resultBlock.PlayPlacementAnimation();
        AudioManager.PlayAt(placementSound, resultBlock.transform.position);
        resultBlock.PlayRareAppearance();
        SkillBlockCreated?.Invoke(resultBlock, match.Position);
    }

    private void TryDismantle(Vector2Int position)
    {
        if (!GridManager.Instance.TryRemoveBlock(position, out Block block))
            return;

        GameManager.PlacePoint.AddValue(CalculateDismantleRefund(block, dismantleRefundRate));
        BlockDismantled?.Invoke(block, position);
        block.PlayDismantleAnimation(() =>
        {
            AudioManager.PlayAt(dismantleSound, block.transform.position);
            Destroy(block.gameObject);
        });
        SetRefundPreviewVisible(false);
    }

    private void UpdatePlacementPreview(Vector2Int position)
    {
        Vector3 worldPosition = GridManager.Instance.GridToWorld(position);
        previewRenderer.gameObject.SetActive(true);
        previewRenderer.color = GridManager.Instance.IsCellEmpty(position)
            ? WithPreviewAlpha(selectedGrant.Data.VisualColor, normalColor.a)
            : invalidColor;
        previewRenderer.transform.position = worldPosition;
        effectVisualizer?.ShowPlacement(
            selectedGrant.Data,
            worldPosition,
            GridManager.Instance.CellSize);
    }

    private void UpdateDismantlePreview(Vector2Int position)
    {
        if (dismantleRefundText == null
            || !GridManager.Instance.TryGetBlock(position, out Block block))
        {
            SetRefundPreviewVisible(false);
            return;
        }

        bool isCore = (block.BlockProperty & BlockProperty.Core) != 0;
        dismantleRefundText.text = isCore
            ? "철거 불가"
            : $"철거 +{CalculateDismantleRefund(block, dismantleRefundRate):0.#}";
        SetRefundPreviewVisible(true);
        if (placementControlText != null)
            placementControlText.position = Mouse.current.position.ReadValue() + dismantlePreviewOffset;
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
            SetRefundPreviewVisible(false);
            gameObject.SetActive(false);
        }
    }

    private void HandleSupplyChanged(bool _)
    {
        ClearSelection();
    }

    private void HidePreview()
    {
        if (previewRenderer != null)
            previewRenderer.gameObject.SetActive(false);
        effectVisualizer?.HidePlacement();
    }

    private void SetRefundPreviewVisible(bool visible)
    {
        if (dismantleRefundText != null
            && dismantleRefundText.gameObject.activeSelf != visible)
        {
            dismantleRefundText.gameObject.SetActive(visible);
        }
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
}
