using System;

namespace KeepCoreSafe.Blocks
{
    [Flags]
    public enum BlockProperty
    {
        None = 0,
        Core = 1 << 0,
        Wall = 1 << 1,
        Healer = 1 << 2,
        Mechanical = 1 << 3,
        Attack = 1 << 4,
        Support = 1 << 5,
        Supply = 1 << 6
    }
}
