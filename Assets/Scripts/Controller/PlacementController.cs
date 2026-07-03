using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlacementController : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private SpriteRenderer previewRenderer;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color invalidColor;
    [SerializeField] private PlacementVisualizer effectVisualizer;

    [Header("Core")]
    [SerializeField] private BlockData coreBlockData;

    private BlockData selectedBlock;
    private ObservableValue placePoint;

    void Start()
    {
        GameManager.PhaseChanged += OnPhaseChanged;
        placePoint = GameManager.PlacePoint;
        PlaceCoreAtCenter();
    }

    private void OnDestroy()
    {
        GameManager.PhaseChanged -= OnPhaseChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (selectedBlock == null) return;

        Vector2Int pos = GridManager.Instance.WorldToGrid(GetMousePos());

        if (!GridManager.Instance.Grid.IsWithinBounds(pos))
        {
            previewRenderer.gameObject.SetActive(false);
            return;
        }

        HandleInput(pos);

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
            gameObject.SetActive(false);
    }

    private void HandleInput(Vector2Int pos)
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
            }
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (GridManager.Instance.TryRemoveBlock(pos, out Block block))
            {
                placePoint.AddValue(block.Cost);
                Destroy(block.gameObject);
            }
        }
    }

    private Block CreateBlock(BlockData data)
    {
        GameObject go = new GameObject(data.name);

        Block block = null;

        if ((data.Properties & BlockProperty.Attack) != 0)
        {
            block = go.AddComponent<AttackBlock>();
        }
        else if ((data.Properties & BlockProperty.Healer) != 0)
        {
            block = go.AddComponent<HealerBlock>();
        }
        else if ((data.Properties & BlockProperty.Support) != 0)
        {
            block = go.AddComponent<SupportBlock>();
        }
        else if ((data.Properties & BlockProperty.Core) != 0)
        {
            block = go.AddComponent<CoreBlock>();
        }
        else if((data.Properties & BlockProperty.Wall) != 0)
        {
            block = go.AddComponent<WallBlock>();
        }

        block?.Initialize(data);
        return block;
    }

    private void PlaceCoreAtCenter()
    {
        if (GridManager.Instance.Grid.Core != null)
            return;

        if (coreBlockData == null)
            coreBlockData = Resources.Load<BlockData>("Data/Block/CoreData");

        if (coreBlockData == null)
        {
            Debug.LogError("CoreData is not assigned or available in Resources/Data.");
            return;
        }

        Vector2Int center = new Vector2Int(
            GridManager.Instance.Width / 2,
            GridManager.Instance.Height / 2);
        Block core = CreateBlock(coreBlockData);

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
        GameManager.Instance.TryStartCombat();
    }
}
