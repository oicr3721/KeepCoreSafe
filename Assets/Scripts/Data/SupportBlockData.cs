using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "SupportBlockData", menuName = "Keep Core Safe/Block Data/Support")]
    public sealed class SupportBlockData : AreaBlockData
    {
        [SerializeField, Range(0.1f, 1f)]
        private float cooldownMultiplier = 0.75f;

        public float CooldownMultiplier => cooldownMultiplier;
        public override BlockProperty Properties => base.Properties | BlockProperty.Support;
    }
}
