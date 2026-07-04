using KeepCoreSafe.Blocks;
using KeepCoreSafe.Audio;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "HealerBlockData", menuName = "Keep Core Safe/Block Data/Healer")]
    public sealed class HealerBlockData : TimedAreaBlockData
    {
        [SerializeField, Min(0)]
        private int healValue;

        [Header("Audio")]
        [Tooltip("Played when a heal projectile reaches a damaged block and restores HP.")]
        [SerializeField] private AudioCue healSound = new();

        public int HealValue => healValue;
        public AudioCue HealSound => healSound;
        public override BlockProperty Properties => base.Properties | BlockProperty.Healer;
    }
}
