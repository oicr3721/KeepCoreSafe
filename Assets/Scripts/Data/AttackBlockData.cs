using KeepCoreSafe.Blocks;
using KeepCoreSafe.Audio;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "AttackBlockData", menuName = "Keep Core Safe/Block Data/Attack")]
    public sealed class AttackBlockData : TimedAreaBlockData
    {
        [SerializeField, Min(0)]
        private int attackValue;

        [Header("Audio")]
        [Tooltip("Played after this block successfully damages an enemy.")]
        [SerializeField] private AudioCue attackSound = new();

        public int AttackValue => attackValue;
        public AudioCue AttackSound => attackSound;
        public override BlockProperty Properties => base.Properties | BlockProperty.Attack;
    }
}
