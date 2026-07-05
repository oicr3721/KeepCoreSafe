using DG.Tweening;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Tutorial
{
    public sealed class TutorialGridHighlight : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private Color color = new(0.35f, 1f, 0.55f, 0.55f);
        [SerializeField, Min(0f)] private float pulseScale = 0.12f;
        [SerializeField, Min(0.1f)] private float pulseDuration = 0.55f;

        public void Show(Vector2Int cell)
        {
            if (GridManager.Instance == null || highlightRenderer == null)
                return;

            gameObject.SetActive(true);
            transform.position = GridManager.Instance.GridToWorld(cell);
            float cellSize = GridManager.Instance.CellSize;
            Vector2 spriteSize = highlightRenderer.sprite != null
                ? highlightRenderer.sprite.bounds.size
                : Vector2.one;
            transform.localScale = new Vector3(
                cellSize * 0.9f / Mathf.Max(0.001f, spriteSize.x),
                cellSize * 0.9f / Mathf.Max(0.001f, spriteSize.y),
                1f);
            highlightRenderer.color = color;
            transform.DOKill(false);
            transform.DOPunchScale(transform.localScale * pulseScale, pulseDuration, 4, 0.4f)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        public void Hide()
        {
            transform.DOKill(false);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            transform.DOKill(false);
        }
    }
}
