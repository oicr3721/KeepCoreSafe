using KeepCoreSafe.Blocks;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace KeepCoreSafe.Controllers
{
    public sealed class WorldBlockHoverController : MonoBehaviour
    {
        [SerializeField] private BlockDescriptionTooltip tooltip;
        [SerializeField] private PlacementVisualizer effectVisualizer;

        private Block hoveredBlock;
        private bool hasHover;

        private void Update()
        {
            if (Mouse.current == null
                || Camera.main == null
                || GridManager.Instance == null
                || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearHover();
                return;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;
            Vector2Int cell = GridManager.Instance.WorldToGrid(worldPosition);
            if (!GridManager.Instance.Grid.IsWithinBounds(cell)
                || !GridManager.Instance.TryGetBlock(cell, out Block block))
            {
                ClearHover();
                return;
            }

            if (hoveredBlock != block)
            {
                hoveredBlock = block;
                hasHover = true;
                tooltip?.Show(this, block.Data, screenPosition);
                effectVisualizer?.ShowHover(
                    block.Data,
                    block.transform.position,
                    GridManager.Instance.CellSize);
            }
            else
            {
                tooltip?.SetPosition(this, screenPosition);
            }
        }

        private void ClearHover()
        {
            if (!hasHover)
                return;

            hasHover = false;
            hoveredBlock = null;
            tooltip?.Hide(this);
            effectVisualizer?.HideHover();
        }

        private void OnDisable()
        {
            ClearHover();
        }
    }
}
