using KeepCoreSafe.Controllers;
using KeepCoreSafe.Localization;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    public abstract class ShopOfferData : ScriptableObject
    {
        [SerializeField] private Sprite displayImage;
        [SerializeField, Tooltip("Stable analytics identifier. Falls back to the asset name when empty.")]
        private string analyticsId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;

        public Sprite DisplayImage => displayImage;
        public string DisplayName => LocalizationManager.Get(displayName, displayName);
        public string Description => LocalizationManager.Get(description, description);
        public string DisplayNameKey => displayName;
        public string DescriptionKey => description;
        public string AnalyticsId => string.IsNullOrWhiteSpace(analyticsId) ? name : analyticsId;

        public bool CanSelect(BlockSupplyController supplyController)
        {
            return supplyController != null && CanApply(supplyController);
        }

        public bool TryApply(BlockSupplyController supplyController)
        {
            if (!CanSelect(supplyController))
                return false;

            Apply(supplyController);
            return true;
        }

        protected abstract bool CanApply(BlockSupplyController supplyController);
        protected abstract void Apply(BlockSupplyController supplyController);
    }
}
