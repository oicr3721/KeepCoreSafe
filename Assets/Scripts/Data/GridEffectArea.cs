using UnityEngine;

namespace KeepCoreSafe.Data
{
    public static class GridEffectArea
    {
        public static int GetCellRange(float effectRange)
        {
            return Mathf.Max(1, Mathf.RoundToInt(effectRange));
        }

        public static bool ContainsOffset(
            Vector2Int offset,
            AdjacencyDirection directions,
            float effectRange)
        {
            if (offset == Vector2Int.zero)
                return false;

            int range = GetCellRange(effectRange);
            if (Mathf.Abs(offset.x) > range || Mathf.Abs(offset.y) > range)
                return false;

            if ((directions & AdjacencyDirection.Everything) != 0)
                return true;

            Vector2Int normalized = new Vector2Int(
                offset.x == 0 ? 0 : (int)Mathf.Sign(offset.x),
                offset.y == 0 ? 0 : (int)Mathf.Sign(offset.y));

            if (normalized == Vector2Int.up) return Has(directions, AdjacencyDirection.Up);
            if (normalized == Vector2Int.down) return Has(directions, AdjacencyDirection.Down);
            if (normalized == Vector2Int.left) return Has(directions, AdjacencyDirection.Left);
            if (normalized == Vector2Int.right) return Has(directions, AdjacencyDirection.Right);
            bool exactDiagonal = Mathf.Abs(offset.x) == Mathf.Abs(offset.y);
            if (!exactDiagonal) return false;
            if (normalized == new Vector2Int(-1, 1)) return Has(directions, AdjacencyDirection.UpLeft);
            if (normalized == new Vector2Int(1, 1)) return Has(directions, AdjacencyDirection.UpRight);
            if (normalized == new Vector2Int(-1, -1)) return Has(directions, AdjacencyDirection.DownLeft);
            if (normalized == new Vector2Int(1, -1)) return Has(directions, AdjacencyDirection.DownRight);
            return false;
        }

        private static bool Has(AdjacencyDirection directions, AdjacencyDirection flag)
        {
            return (directions & flag) != 0;
        }
    }
}
