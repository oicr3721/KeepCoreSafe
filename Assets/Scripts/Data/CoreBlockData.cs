using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "CoreBlockData", menuName = "Keep Core Safe/Block Data/Core")]
    public sealed class CoreBlockData : BlockData
    {
        public override BlockProperty Properties => base.Properties | BlockProperty.Core;
    }
}
