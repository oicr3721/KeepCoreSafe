using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "BlockData", menuName = "Keep Core Safe/Block Data")]
    public sealed class BlockData : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        private BlockProperty properties;

        [SerializeField, Min(1)]
        private int maxHP = 100;

        [SerializeField, Min(0)]
        private int cost;

        [SerializeField, Min(0)]
        private int attackValue;

        [SerializeField, Min(0)]
        private int healValue;

        [SerializeField, Min(0.01f)]
        private float actionCooldown = 1f;

        [SerializeField, Min(0f)]
        private float effectRange = 1f;

        [SerializeField, Range(0.1f, 1f)]
        private float cooldownMultiplier = 1f;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private AdjacencyDirection affectedDirections;

        public string DisplayName => displayName;
        public BlockProperty Properties => properties;
        public int MaxHP => maxHP;
        public int Cost => cost;
        public int AttackValue => attackValue;
        public int HealValue => healValue;
        public float ActionCooldown => actionCooldown;
        public float EffectRange => effectRange;
        public float CooldownMultiplier => cooldownMultiplier;
        public Sprite Sprite => sprite;
        public AdjacencyDirection AffectedDirections => affectedDirections;

        public bool AffectsOffset(Vector2Int offset)
        {
            return GridEffectArea.ContainsOffset(offset, affectedDirections, effectRange);
        }

#if UNITY_EDITOR
        public void ConfigurePrototype(
            string name,
            BlockProperty blockProperties,
            int hp,
            int buildCost,
            int attack,
            int heal,
            float cooldown,
            float range,
            float cooldownScale,
            Sprite blockSprite,
            AdjacencyDirection directions)
        {
            displayName = name;
            properties = blockProperties;
            maxHP = hp;
            cost = buildCost;
            attackValue = attack;
            healValue = heal;
            actionCooldown = cooldown;
            effectRange = range;
            cooldownMultiplier = cooldownScale;
            sprite = blockSprite;
            affectedDirections = directions;
        }
#endif
    }
}
