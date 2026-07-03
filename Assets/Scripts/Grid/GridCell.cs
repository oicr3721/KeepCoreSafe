using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.GridSystem
{
    public sealed class GridCell
    {
        public Vector2Int Position { get; }
        public Block Occupant { get; private set; }
        public bool IsOccupied => Occupant != null;

        public GridCell(Vector2Int position)
        {
            Position = position;
        }

        internal bool TryOccupy(Block block)
        {
            if (block == null || IsOccupied)
            {
                return false;
            }

            Occupant = block;
            return true;
        }

        internal bool TryClear(Block expectedBlock, out Block block)
        {
            block = Occupant;
            if (!IsOccupied || Occupant != expectedBlock)
            {
                return false;
            }

            Occupant = null;
            return true;
        }
    }
}
