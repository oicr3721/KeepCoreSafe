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

        private Tween pulseTween;
        private Vector2Int currentCell;
        private bool hasCurrentCell;

        public void Show(Vector2Int cell)
        {
            if (GridManager.Instance == null || highlightRenderer == null)
                return;

            if (gameObject.activeSelf
                && hasCurrentCell
                && currentCell == cell
                && pulseTween != null
                && pulseTween.IsActive()
                && pulseTween.IsPlaying())
            {
                return;
            }

            KillPulse();
            gameObject.SetActive(true);
            currentCell = cell;
            hasCurrentCell = true;
            transform.position = GridManager.Instance.GridToWorld(cell);
            float cellSize = GridManager.Instance.CellSize;
            Vector2 spriteSize = highlightRenderer.sprite != null
                ? highlightRenderer.sprite.bounds.size
                : Vector2.one;
            Vector3 baseScale = new Vector3(
                cellSize * 0.9f / Mathf.Max(0.001f, spriteSize.x),
                cellSize * 0.9f / Mathf.Max(0.001f, spriteSize.y),
                1f);
            transform.localScale = baseScale;
            highlightRenderer.color = color;

            pulseTween = transform.DOPunchScale(baseScale * pulseScale, pulseDuration, 4, 0.4f)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetTarget(this);
        }

        public void Hide()
        {
            KillPulse();
            hasCurrentCell = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            KillPulse();
        }

        private void KillPulse()
        {
            pulseTween?.Kill(false);
            pulseTween = null;
        }
    }
}
