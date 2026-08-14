using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "ShopEventData", menuName = "Keep Core Safe/Shop/Event Schedule")]
    public sealed class ShopEventData : ScriptableObject
    {
        [Header("Supply Event Schedule")]
        [SerializeField, Range(0f, 1f)] private float appearanceChance = 0.35f;
        [SerializeField, Min(1)] private int minimumWaveInterval = 2;
        [SerializeField, Min(0)] private int maximumWaveInterval = 5;

        [Header("Supply Hunters")]
        [SerializeField, Range(0f, 1f)] private float supplyHunterRatio = 0.2f;
        [SerializeField, Min(1)] private int minimumSupplyHunters = 1;

        [Header("Offers")]
        [SerializeField, Min(1)] private int offersPerEvent = 3;
        [SerializeField] private List<ShopOfferData> offers = new();

        public int OffersPerEvent => offersPerEvent;
        public IReadOnlyList<ShopOfferData> Offers => offers;
        public float SupplyHunterRatio => supplyHunterRatio;
        public int MinimumSupplyHunters => minimumSupplyHunters;

        public bool ShouldStartAfterWave(int completedWave, int lastEventWave)
        {
            if (completedWave <= 0 || completedWave - lastEventWave < minimumWaveInterval)
                return false;

            if (maximumWaveInterval > 0 && completedWave - lastEventWave >= maximumWaveInterval)
                return true;

            return Random.value < appearanceChance;
        }
    }
}
