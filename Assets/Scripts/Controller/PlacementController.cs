using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PlacementController : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private SpriteRenderer previewRenderer;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color invalidColor;
    [SerializeField] private PlacementVisualizer effectVisualizer;

    [Header("Dismantle Preview")]
    [SerializeField] private RectTransform placementControlText;
    [SerializeField] private TMP_Text dismantleRefundText;
    [SerializeField] private TMP_Text placementCostText;
    [SerializeField, Range(0f, 1f)] private float dismantleRefundRate = 0.5f;
    [SerializeField] private Vector2 dismantlePreviewOffset = new Vector2(22f, 26f);

    [Header("Core")]
    [SerializeField] private CoreBlockData coreBlockData;

    private BlockData selectedBlock;
    private ObservableValue placePoint;

    void Start()
    {
        GameManager.PhaseChanged += OnPhaseChanged;
        placePoint = GameManager.PlacePoint;
        PlaceCoreAtCenter();
        SetRefundPreviewVisible(false);
    }

    private void OnDestroy()
    {
        GameManager.PhaseChanged -= OnPhaseChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current == null || Camera.main == null || GridManager.Instance == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            previewRenderer.gameObject.SetActive(false);
            SetRefundPreviewVisible(false);
            return;
        }

        Vector3 mouseWorld = GetMousePos();
        Vector2Int pos = GridManager.Instance.WorldToGrid(mouseWorld);

        if (!GridManager.Instance.Grid.IsWithinBounds(pos))
        {
            previewRenderer.gameObject.SetActive(false);
            SetRefundPreviewVisible(false);
            return;
        }

        UpdateControlPreview(pos);
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryDismantle(pos);
            return;
        }

        if (selectedBlock == null)
        {
            previewRenderer.gameObject.SetActive(false);
            return;
        }

        TryPlaceSelectedBlock(pos);

        previewRenderer.gameObject.SetActive(true);

        previewRenderer.color = GridManager.Instance.IsCellEmpty(pos)
                                ? normalColor
                                : invalidColor;

        previewRenderer.transform.position = GridManager.Instance.GridToWorld(pos);
    }

    Vector3 GetMousePos()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 rawMouseWorld = Camera.main.ScreenToWorldPoint(mousePos);
        rawMouseWorld.z = 0f;

        return rawMouseWorld;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        //Placement Controller의 자식으로 Placement Visual도 전부 넣어놓고 한 번에 껐다 켰다 되게끔 할 것
        if (phase == GamePhase.Preparation)
            gameObject.SetActive(true);
        else
        {
            SetRefundPreviewVisible(false);
            gameObject.SetActive(false);
        }
    }

    private void TryPlaceSelectedBlock(Vector2Int pos)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (selectedBlock == null)
                return;

            if (placePoint.CurrentValue < selectedBlock.Cost)
                return;

            if (!GridManager.Instance.IsCellEmpty(pos))
                return;

            Block block = CreateBlock(selectedBlock);
            if (GridManager.Instance.TryPlaceBlock(block, pos))
            {
                placePoint.SubtractValue(selectedBlock.Cost);
                block.PlayPlacementAnimation();
            }
        }
    }

    private void TryDismantle(Vector2Int pos)
    {
        if (GridManager.Instance.TryRemoveBlock(pos, out Block block))
        {
            placePoint.AddValue(CalculateDismantleRefund(block, dismantleRefundRate));
            block.PlayDismantleAnimation(() => Destroy(block.gameObject));
            SetRefundPreviewVisible(false);
        }
    }

    private void UpdateControlPreview(Vector2Int pos)
    {
        if (dismantleRefundText == null
            || !GridManager.Instance.TryGetBlock(pos, out Block block))
        {
            SetRefundPreviewVisible(false);
        }
        else
        {
            bool isCore = (block.BlockProperty & BlockProperty.Core) != 0;
            dismantleRefundText.text = isCore
                ? "Core cannot be dismantled"
                : $"Refund +{CalculateDismantleRefund(block, dismantleRefundRate):0.#}";
            SetRefundPreviewVisible(true);
        }

        if (placementCostText == null || selectedBlock == null)
        {
            SetCostPreviewVisible(false);
        }
        else
        {
            placementCostText.text = $"Cost -{selectedBlock.Cost:0.#}";

            SetCostPreviewVisible(true);
        }

        placementControlText.position = Mouse.current.position.ReadValue() + dismantlePreviewOffset;
    }

    private void SetRefundPreviewVisible(bool visible)
    {
        if (dismantleRefundText != null
            && dismantleRefundText.gameObject.activeSelf != visible)
        {
            dismantleRefundText.gameObject.SetActive(visible);
        }
    }

    private void SetCostPreviewVisible(bool visible)
    {
        if (placementCostText != null
            && placementCostText.gameObject.activeSelf != visible)
        {
            placementCostText.gameObject.SetActive(visible);
        }
    }

    public static float CalculateDismantleRefund(Block block, float refundRate = 0.5f)
    {
        if (block == null || block.HP.MaxValue <= 0)
            return 0f;

        return Mathf.FloorToInt(block.Cost * refundRate * ((float)block.HP.CurrentValue / block.HP.MaxValue));
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

    private void PlaceCoreAtCenter()
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

        Vector2Int center = new Vector2Int(
            GridManager.Instance.Width / 2,
            GridManager.Instance.Height / 2);
        Block core = CreateBlock(coreBlockData);
        if (core == null)
            return;

        if (!GridManager.Instance.TryPlaceBlock(core, center))
        {
            Destroy(core.gameObject);
            Debug.LogError("Failed to place the Core at the Grid center.");
        }
    }


    /// <summary>
    /// UI 측에서 호출
    /// </summary>
    /// <param name="block"></param>
    public void SelectBlock(BlockData block)
    {
        selectedBlock = block;

        previewRenderer.sprite = block.Sprite;
        effectVisualizer?.SetData(block, GridManager.Instance.CellSize);
    }

    public void ClearBlock()
    {
        selectedBlock = null;
        previewRenderer.gameObject.SetActive(false);
    }

    /// <summary>
    /// 배치 완료 버튼 클릭시
    /// </summary>
    public void Confirm()
    {
        selectedBlock = null;
        SetRefundPreviewVisible(false);
        SetCostPreviewVisible(false);
        GameManager.Instance.TryStartCombat();
    }
}
