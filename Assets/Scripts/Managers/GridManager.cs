using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.GridSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using RuntimeGrid = KeepCoreSafe.GridSystem.Grid;

namespace KeepCoreSafe.Managers
{
    public sealed class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField, Min(1)]
        private int width = 10;

        [SerializeField, Min(1)]
        private int height = 10;

        [SerializeField, Min(0.1f)]
        private float cellSize = 1f;

        private readonly Dictionary<Block, Vector2Int> blockPositions = new();

        public RuntimeGrid Grid { get; private set; }
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        public event Action CoreDestroyed;

        private Vector3 GridOrigin => transform.position - GridOffset;

        private Vector3 GridOffset =>
            new Vector3(
                (width - 1) * cellSize * 0.5f,
                (height - 1) * cellSize * 0.5f,
                0f);

        public Vector3 GridCenter => transform.position;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);

            Grid = new RuntimeGrid(width, height);
        }

        public bool IsCellEmpty(Vector2Int position)
        {
            if (!Grid.TryGetCell(position, out GridCell cell))
                return false;

            return !cell.IsOccupied;
        }

        public bool TryPlaceBlock(Block block, Vector2Int position)
        {
            if (Grid == null || !Grid.TryPlaceBlock(block, position))
            {
                return false;
            }

            block.transform.position = GridToWorld(position);
            block.SetGridPosition(position);
            blockPositions[block] = position;
            block.Died += HandleBlockDied;
            return true;
        }

        public bool TryRemoveBlock(Vector2Int position, out Block block)
        {
            block = null;
            if (Grid == null || !Grid.TryRemoveBlock(position, out block))
            {
                return false;
            }

            UnregisterBlock(block);
            return true;
        }

        public IEnumerable<Block> GetAdjacentBlocks(Vector2Int position)
        {
            return Grid.GetAdjacentBlocks(position);
        }

        public IEnumerable<Block> GetAdjacentBlocks(Vector2Int position, AdjacencyDirection directions)
        {
            return Grid.GetAdjacentBlocks(position, directions);
        }

        private void HandleBlockDied(Block block)
        {
            bool wasCore = (block.BlockProperty & BlockProperty.Core) != 0;
            if (blockPositions.TryGetValue(block, out Vector2Int position))
            {
                Grid.TryClearBlock(position, block);
            }

            UnregisterBlock(block);
            if (wasCore)
            {
                CoreDestroyed?.Invoke();
            }
        }

        private void UnregisterBlock(Block block)
        {
            block.Died -= HandleBlockDied;
            block.ClearGridPosition();
            blockPositions.Remove(block);
        }

        public Vector3 GridToWorld(Vector2Int position)
        {
            return GridOrigin +
                   new Vector3(position.x * cellSize,
                               position.y * cellSize,
                               0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - GridOrigin;

            return new Vector2Int(
                Mathf.RoundToInt(local.x / cellSize),
                Mathf.RoundToInt(local.y / cellSize));
        }
    }
}
