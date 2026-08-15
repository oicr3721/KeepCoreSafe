using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "CoreEnergyOffer", menuName = "Keep Core Safe/Shop/Core Energy Offer")]
    public sealed class CoreEnergyShopOfferData : ShopOfferData
    {
        [SerializeField, Min(1)] private int energyAmount = 10;

        public int EnergyAmount => energyAmount;

        protected override bool CanApply(BlockSupplyController supplyController)
        {
            return GameManager.Instance != null
                   && GameManager.Instance.CanAddPreparationEnergy(energyAmount);
        }

        protected override void Apply(BlockSupplyController supplyController)
        {
            GameManager.Instance?.TryAddPreparationEnergy(energyAmount);
        }
    }
}
