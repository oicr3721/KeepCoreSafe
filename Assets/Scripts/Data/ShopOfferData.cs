using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    public abstract class ShopOfferData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField, Min(0f)] private float cost;

        public string DisplayName => displayName;
        public string Description => description;
        public float Cost => cost;

        public bool TryPurchase(BlockSupplyController supplyController)
        {
            if (supplyController == null
                || GameManager.PlacePoint.CurrentValue < cost
                || !CanApply(supplyController))
            {
                return false;
            }

            GameManager.PlacePoint.SubtractValue(cost);
            Apply(supplyController);
            return true;
        }

        protected abstract bool CanApply(BlockSupplyController supplyController);
        protected abstract void Apply(BlockSupplyController supplyController);
    }
}
