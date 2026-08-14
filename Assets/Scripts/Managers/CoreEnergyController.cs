using KeepCoreSafe.Blocks;
using KeepCoreSafe.Core;
using KeepCoreSafe.Presentation;
using DG.Tweening;
using UnityEngine;

namespace KeepCoreSafe.Managers
{
    public sealed class CoreEnergyController : MonoBehaviour
    {
        [Header("Automatic Charge")]
        [SerializeField, Min(0)] private int automaticEnergyPerSecond = 1;

        [Header("Energy Pickup")]
        [SerializeField] private CoreEnergyPickupView pickupPrefab;
        [SerializeField] private Transform pickupRoot;
        [SerializeField, Min(0)] private int initialPickupPoolSize = 12;

        [Header("Core Absorption")]
        [SerializeField] private CoreEnergyPulseView absorptionPulsePrefab;
        [SerializeField] private Transform pulseRoot;
        [SerializeField, Min(0.01f)] private float absorptionPulseDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float absorptionPulseScale = 0.65f;

        private ComponentPool<CoreEnergyPickupView> pickupPool;
        private ComponentPool<CoreEnergyPulseView> pulsePool;
        private float automaticChargeAccumulator;
        private bool thresholdTriggered;

        public ObservableInt Energy { get; } = new();

        private void Awake()
        {
            if (pickupPrefab != null)
                pickupPool = new ComponentPool<CoreEnergyPickupView>(pickupPrefab, initialPickupPoolSize, pickupRoot);
            if (absorptionPulsePrefab != null)
                pulsePool = new ComponentPool<CoreEnergyPulseView>(absorptionPulsePrefab, 2, pulseRoot);
        }

        private void Update()
        {
            if (GameManager.Phase != GamePhase.Combat || thresholdTriggered || automaticEnergyPerSecond <= 0)
                return;

            automaticChargeAccumulator += Time.deltaTime * automaticEnergyPerSecond;
            int wholeEnergy = Mathf.FloorToInt(automaticChargeAccumulator);
            if (wholeEnergy <= 0)
                return;

            automaticChargeAccumulator -= wholeEnergy;
            AddEnergy(wholeEnergy);
        }

        public void BeginPreparation(int requiredEnergy)
        {
            int maximum = Mathf.Max(1, requiredEnergy);
            automaticChargeAccumulator = 0f;
            thresholdTriggered = false;
            Energy.Initialize(0, maximum, -maximum);
        }

        public bool CanApplyRerollCost(int cost)
        {
            return cost > 0 && Energy.CurrentValue - cost >= Energy.MinValue;
        }

        public bool TryApplyRerollCost(int cost)
        {
            if (GameManager.Phase != GamePhase.Preparation || !CanApplyRerollCost(cost))
                return false;

            Energy.AddValue(-cost);
            return true;
        }

        public void BeginWave(int requiredEnergy)
        {
            automaticChargeAccumulator = 0f;
            thresholdTriggered = false;
            int maximum = Mathf.Max(1, requiredEnergy);
            Energy.Initialize(Energy.CurrentValue, maximum, -maximum);
        }

        public void ResetEnergy()
        {
            Energy.SetValue(0);
        }

        public void AwardEnemyEnergy(Vector3 origin, int amount)
        {
            if (amount <= 0 || GameManager.Phase != GamePhase.Combat || thresholdTriggered)
                return;

            CoreBlock core = GridManager.Instance?.Grid?.Core as CoreBlock;
            CoreEnergyPickupView pickup = core != null ? pickupPool?.Rent() : null;
            if (pickup == null)
            {
                AddEnergy(amount);
                PlayAbsorptionPulse(core);
                return;
            }

            pickup.Play(origin, core.transform, () =>
            {
                pickupPool.Return(pickup);
                if (core == null || GameManager.Phase != GamePhase.Combat)
                    return;

                AddEnergy(amount);
                PlayAbsorptionPulse(core);
            });
        }

        private void AddEnergy(int amount)
        {
            if (thresholdTriggered || amount <= 0)
                return;

            Energy.AddValue(amount);
            if (Energy.CurrentValue < Energy.MaxValue)
                return;

            thresholdTriggered = true;
            GameManager.Instance?.TriggerEnergyShockwave();
        }

        private void PlayAbsorptionPulse(CoreBlock core)
        {
            if (core == null)
                return;

            CoreEnergyPulseView pulse = pulsePool?.Rent();
            if (pulse == null)
                return;

            pulse.transform.position = core.transform.position;
            pulse.Play(absorptionPulseDuration, 1, 0.15f, absorptionPulseScale, 0.75f);
            DOVirtual.DelayedCall(absorptionPulseDuration, () => pulsePool.Return(pulse), true)
                .SetTarget(pulse);
        }
    }
}
