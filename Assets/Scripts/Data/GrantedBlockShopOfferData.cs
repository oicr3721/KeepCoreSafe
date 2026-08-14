using KeepCoreSafe.Controllers;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "GrantedBlockOffer", menuName = "Keep Core Safe/Shop/Granted Block Offer")]
    public sealed class GrantedBlockShopOfferData : ShopOfferData
    {
        [SerializeField] private BlockData grantedBlock;
        [SerializeField] private bool playRareAppearance = true;

        public BlockData GrantedBlock => grantedBlock;

        protected override bool CanApply(BlockSupplyController supplyController)
        {
            return grantedBlock != null
                   && supplyController.CanQueueGuaranteedBlockForNextPreparation;
        }

        protected override void Apply(BlockSupplyController supplyController)
        {
            supplyController.QueueGuaranteedBlockForNextPreparation(
                grantedBlock,
                playRareAppearance);
        }
    }
}
