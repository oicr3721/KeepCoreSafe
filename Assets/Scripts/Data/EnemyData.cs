using KeepCoreSafe.Blocks;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Localization;
using UnityEngine;
using UnityEngine.Serialization;

namespace KeepCoreSafe.Data
{
    public class EnemyData : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField, Min(1)]
        private int maxHP = 60;

        [SerializeField, Min(0.1f)]
        private float moveSpeed = 1.6f;

        [SerializeField, Min(1)]
        private int attackDamage = 12;

        [SerializeField, Min(0.1f)]
        private float attackCooldown = 1f;

        [FormerlySerializedAs("maxPreferredPathExtraCells")]
        [SerializeField, Min(0)]
        [Tooltip("Paths up to this many cells longer than the shortest route remain candidates.")]
        private int pathLengthTolerance = 2;

        [SerializeField, Min(0)] private int energyOnDeath = 1;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Enemy prefab;

        [SerializeField]
        private BlockProperty[] targetPriority = { BlockProperty.Core, BlockProperty.Wall };

        [Header("Audio")]
        [Tooltip("Played when this enemy successfully performs an attack.")]
        [SerializeField] private AudioCue attackSound = new();

        public string DisplayName => LocalizationManager.Get(displayName, displayName);
        public string DisplayNameKey => displayName;
        public int MaxHP => maxHP;
        public float MoveSpeed => moveSpeed;
        public int AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public int PathLengthTolerance => pathLengthTolerance;
        public int EnergyOnDeath => energyOnDeath;
        public Sprite Sprite => sprite;
        public Enemy Prefab => prefab;
        public AudioCue AttackSound => attackSound;

        public int GetPriority(BlockProperty property)
        {
            for (int i = 0; i < targetPriority.Length; i++)
            {
                if ((property & targetPriority[i]) != 0)
                    return i;
            }

            return targetPriority.Length;
        }
    }
}
