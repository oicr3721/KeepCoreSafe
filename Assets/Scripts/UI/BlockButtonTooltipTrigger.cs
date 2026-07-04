using KeepCoreSafe.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KeepCoreSafe.UI
{
    public sealed class BlockButtonTooltipTrigger : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler
    {
        private BlockData data;
        private BlockDescriptionTooltip tooltip;

        public void Initialize(BlockData blockData, BlockDescriptionTooltip tooltipView)
        {
            data = blockData;
            tooltip = tooltipView;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltip?.Show(this, data, eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            tooltip?.SetPosition(this, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide(this);
        }

        private void OnDisable()
        {
            tooltip?.Hide(this);
        }
    }
}
