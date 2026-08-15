using KeepCoreSafe.Audio;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "SuicideEnemyData", menuName = "Keep Core Safe/Enemy Data/Suicide")]
    public sealed class SuicideEnemyData : EnemyData
    {
        [Header("Self Destruct")]
        [SerializeField, Range(0.01f, 0.99f)] private float forcedTriggerHealthRatio = 0.3f;

        [Header("Warning Rhythm")]
        [SerializeField, Min(0.05f)] private float initialPulseInterval = 0.38f;
        [SerializeField, Min(0.03f)] private float finalPulseInterval = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float initialScalePulse = 0.05f;
        [SerializeField, Range(0f, 0.8f)] private float finalScalePulse = 0.24f;
        [SerializeField, Range(0f, 0.5f)] private float finalScaleUpPortion = 0.12f;
        [SerializeField, Range(0f, 1f)] private float finalScaleBoost = 0.38f;

        [Header("Self Destruct Audio")]
        [SerializeField] private AudioCue warningSound = new();

        public float ForcedTriggerHealthRatio => forcedTriggerHealthRatio;
        public float SelfDestructPreparationDuration => AttackCooldown;
        public int ExplosionDamage => AttackDamage;
        public float InitialPulseInterval => initialPulseInterval;
        public float FinalPulseInterval => finalPulseInterval;
        public float InitialScalePulse => initialScalePulse;
        public float FinalScalePulse => finalScalePulse;
        public float FinalScaleUpPortion => finalScaleUpPortion;
        public float FinalScaleBoost => finalScaleBoost;
        public AudioCue WarningSound => warningSound;
        public AudioCue ExplosionSound => AttackSound;
    }
}
