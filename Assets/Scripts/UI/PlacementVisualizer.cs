using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class PlacementVisualizer : MonoBehaviour
    {
        [Header("Direction Renderers")]
        [SerializeField] private SpriteRenderer upRenderer;
        [SerializeField] private SpriteRenderer downRenderer;
        [SerializeField] private SpriteRenderer leftRenderer;
        [SerializeField] private SpriteRenderer rightRenderer;

        [Header("Blink")]
        [SerializeField] private Color effectColor = new Color(0.2f, 0.85f, 1f, 0.45f);
        [SerializeField, Min(0.1f)] private float blinkSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.15f;
        [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.55f;

        public void SetData(BlockData blockData, float cellSize)
        {
            AdjacencyDirection directions = blockData != null
                ? blockData.AffectedDirections
                : AdjacencyDirection.None;

            Configure(upRenderer, AdjacencyDirection.Up, directions, Vector2.up, cellSize);
            Configure(downRenderer, AdjacencyDirection.Down, directions, Vector2.down, cellSize);
            Configure(leftRenderer, AdjacencyDirection.Left, directions, Vector2.left, cellSize);
            Configure(rightRenderer, AdjacencyDirection.Right, directions, Vector2.right, cellSize);
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minimumAlpha, maximumAlpha, wave);

            SetAlpha(upRenderer, alpha);
            SetAlpha(downRenderer, alpha);
            SetAlpha(leftRenderer, alpha);
            SetAlpha(rightRenderer, alpha);
        }

        private void Configure(
            SpriteRenderer renderer,
            AdjacencyDirection flag,
            AdjacencyDirection activeDirections,
            Vector2 offset,
            float cellSize)
        {
            if (renderer == null)
                return;

            renderer.enabled = (activeDirections & flag) != 0;
            renderer.transform.localPosition = offset * cellSize;
            renderer.transform.localScale = new Vector3(cellSize, cellSize, 1f);
            renderer.color = effectColor;
        }

        private void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null || !renderer.enabled)
                return;

            Color color = effectColor;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
