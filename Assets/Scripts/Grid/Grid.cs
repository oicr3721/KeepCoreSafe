using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.GridSystem
{
    public sealed class Grid
    {
        private readonly GridCell[,] cells;

        public int Width { get; }
        public int Height { get; }
        public Block Core { get; private set; }

        public Grid(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            cells = new GridCell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells[x, y] = new GridCell(new Vector2Int(x, y));
                }
            }
        }

        public bool IsWithinBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width
                && position.y >= 0 && position.y < Height;
        }

        public bool TryGetCell(Vector2Int position, out GridCell cell)
        {
            if (!IsWithinBounds(position))
            {
                cell = null;
                return false;
            }

            cell = cells[position.x, position.y];
            return true;
        }

        public bool TryPlaceBlock(Block block, Vector2Int position)
        {
            if (block == null || !TryGetCell(position, out GridCell cell))
            {
                return false;
            }

            bool isCore = (block.BlockProperty & BlockProperty.Core) != 0;
            if ((isCore && Core != null) || !cell.TryOccupy(block))
            {
                return false;
            }

            if (isCore)
            {
                Core = block;
            }

            return true;
        }

        public bool TryRemoveBlock(Vector2Int position, out Block block)
        {
            block = null;
            if (!TryGetCell(position, out GridCell cell) || !cell.IsOccupied)
            {
                return false;
            }

            bool isCore = (cell.Occupant.BlockProperty & BlockProperty.Core) != 0;
            return !isCore && cell.TryClear(cell.Occupant, out block);
        }

        public bool TryClearBlock(Vector2Int position, Block expectedBlock)
        {
            if (!TryGetCell(position, out GridCell cell)
                || !cell.TryClear(expectedBlock, out Block clearedBlock))
            {
                return false;
            }

            if (Core == clearedBlock)
            {
                Core = null;
            }

            return true;
        }

        public IEnumerable<Block> GetAdjacentBlocks(Vector2Int position)
        {
            return GetAdjacentBlocks(position, AdjacencyDirection.All);
        }

        public IEnumerable<Block> GetAdjacentBlocks(Vector2Int position, AdjacencyDirection directions)
        {
            return GetBlocksInEffectArea(position, directions, 1f);
        }

        public IEnumerable<Block> GetBlocksInEffectArea(
            Vector2Int origin,
            AdjacencyDirection directions,
            float effectRange)
        {
            int range = GridEffectArea.GetCellRange(effectRange);
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    Vector2Int offset = new Vector2Int(x, y);
                    if (GridEffectArea.ContainsOffset(offset, directions, effectRange)
                        && TryGetCell(origin + offset, out GridCell cell)
                        && cell.IsOccupied)
                    {
                        yield return cell.Occupant;
                    }
                }
            }
        }

        public IEnumerable<Block> GetBlocks()
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (cells[x, y].IsOccupied)
                    yield return cells[x, y].Occupant;
            }
        }
    }
}
