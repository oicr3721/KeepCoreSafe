using System;

namespace KeepCoreSafe.Data
{
    [Flags]
    public enum AdjacencyDirection
    {
        None = 0,
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        UpLeft = 1 << 4,
        UpRight = 1 << 5,
        DownLeft = 1 << 6,
        DownRight = 1 << 7,

        Cardinal = Up | Down | Left | Right,
        Diagonal = UpLeft | UpRight | DownLeft | DownRight,

        // A separate mode: every cell inside the square EffectRange.
        Everything = 1 << 8,

        // Kept as an alias so existing code/data that used All remains cardinal-only.
        All = Cardinal
    }
}
