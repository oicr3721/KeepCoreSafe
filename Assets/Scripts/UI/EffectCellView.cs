using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class EffectCellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer cellRenderer;
        [SerializeField, Range(0.5f, 1f)] private float cellFillRatio = 0.92f;

        public void Show(Vector2Int offset, float cellSize, Color color)
        {
            if (cellRenderer == null)
                return;

            transform.localPosition = new Vector3(offset.x * cellSize, offset.y * cellSize, 0f);
            transform.localRotation = Quaternion.identity;

            Vector2 spriteSize = cellRenderer.sprite != null
                ? cellRenderer.sprite.bounds.size
                : Vector2.one;
            float width = Mathf.Max(0.0001f, spriteSize.x);
            float height = Mathf.Max(0.0001f, spriteSize.y);
            transform.localScale = new Vector3(
                cellSize * cellFillRatio / width,
                cellSize * cellFillRatio / height,
                1f);
            cellRenderer.color = color;
            cellRenderer.enabled = true;
        }

        public void SetAlpha(float alpha, Color baseColor)
        {
            if (cellRenderer == null)
                return;

            baseColor.a = alpha;
            cellRenderer.color = baseColor;
        }

        private void OnDisable()
        {
            if (cellRenderer != null)
                cellRenderer.enabled = false;
        }
    }
}
