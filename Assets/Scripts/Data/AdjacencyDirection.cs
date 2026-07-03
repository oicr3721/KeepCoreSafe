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
        All = Up | Down | Left | Right
    }
}
