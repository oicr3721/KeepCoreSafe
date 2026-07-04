using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "ShopEventData", menuName = "Keep Core Safe/Shop/Event Schedule")]
    public sealed class ShopEventData : ScriptableObject
    {
        [Header("Schedule")]
        [SerializeField, Min(1)] private int firstWave = 3;
        [SerializeField, Min(0)] private int waveInterval = 3;
        [SerializeField] private List<int> additionalWaves = new();

        [Header("Offers")]
        [SerializeField, Min(1)] private int offersPerEvent = 3;
        [SerializeField] private List<ShopOfferData> offers = new();

        public int OffersPerEvent => offersPerEvent;
        public IReadOnlyList<ShopOfferData> Offers => offers;

        public bool ShouldOpenAfterWave(int completedWave)
        {
            if (additionalWaves.Contains(completedWave))
                return true;
            return waveInterval > 0
                   && completedWave >= firstWave
                   && (completedWave - firstWave) % waveInterval == 0;
        }
    }
}
