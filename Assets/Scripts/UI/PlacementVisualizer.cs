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
        [SerializeField] private SpriteRenderer upLeftRenderer;
        [SerializeField] private SpriteRenderer upRightRenderer;
        [SerializeField] private SpriteRenderer downLeftRenderer;
        [SerializeField] private SpriteRenderer downRightRenderer;
        [SerializeField] private SpriteRenderer everythingRenderer;

        [Header("Blink")]
        [SerializeField] private Color effectColor = new Color(0.2f, 0.85f, 1f, 0.45f);
        [SerializeField, Min(0.1f)] private float blinkSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.15f;
        [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.55f;

        private SpriteRenderer[] renderers;
        private VisualRequest placementRequest;
        private VisualRequest hoverRequest;

        private struct VisualRequest
        {
            public bool Active;
            public BlockData Data;
            public Vector3 Position;
            public float CellSize;
        }

        private void Awake()
        {
            CacheRenderers();
        }

        public void ShowPlacement(BlockData blockData, Vector3 position, float cellSize)
        {
            placementRequest = CreateRequest(blockData, position, cellSize);
            RefreshRequest();
        }

        public void HidePlacement()
        {
            placementRequest.Active = false;
            RefreshRequest();
        }

        public void ShowHover(BlockData blockData, Vector3 position, float cellSize)
        {
            hoverRequest = CreateRequest(blockData, position, cellSize);
            RefreshRequest();
        }

        public void HideHover()
        {
            hoverRequest.Active = false;
            RefreshRequest();
        }

        private void Configure(BlockData blockData, float cellSize)
        {
            AdjacencyDirection directions = blockData != null
                ? blockData.AffectedDirections
                : AdjacencyDirection.None;
            int range = GridEffectArea.GetCellRange(blockData != null ? blockData.EffectRange : 1f);
            bool everything = (directions & AdjacencyDirection.Everything) != 0;

            ConfigureEverything(everythingRenderer, everything, range, cellSize);
            ConfigureCardinal(upRenderer, AdjacencyDirection.Up, directions, Vector2.up, range, cellSize, everything);
            ConfigureCardinal(downRenderer, AdjacencyDirection.Down, directions, Vector2.down, range, cellSize, everything);
            ConfigureCardinal(leftRenderer, AdjacencyDirection.Left, directions, Vector2.left, range, cellSize, everything);
            ConfigureCardinal(rightRenderer, AdjacencyDirection.Right, directions, Vector2.right, range, cellSize, everything);
            ConfigureDiagonal(upLeftRenderer, AdjacencyDirection.UpLeft, directions, new Vector2(-1f, 1f), range, cellSize, everything);
            ConfigureDiagonal(upRightRenderer, AdjacencyDirection.UpRight, directions, new Vector2(1f, 1f), range, cellSize, everything);
            ConfigureDiagonal(downLeftRenderer, AdjacencyDirection.DownLeft, directions, new Vector2(-1f, -1f), range, cellSize, everything);
            ConfigureDiagonal(downRightRenderer, AdjacencyDirection.DownRight, directions, new Vector2(1f, -1f), range, cellSize, everything);
            CacheRenderers();
        }

        private void RefreshRequest()
        {
            VisualRequest request = IsRenderable(placementRequest.Data) && placementRequest.Active
                ? placementRequest
                : hoverRequest;
            bool visible = request.Active && IsRenderable(request.Data);
            gameObject.SetActive(visible);
            if (!visible)
                return;

            transform.position = request.Position;
            Configure(request.Data, request.CellSize);
        }

        private static VisualRequest CreateRequest(BlockData data, Vector3 position, float cellSize)
        {
            return new VisualRequest
            {
                Active = true,
                Data = data,
                Position = position,
                CellSize = cellSize
            };
        }

        private static bool IsRenderable(BlockData data)
        {
            return data != null
                   && data.EffectRange > 0f
                   && data.AffectedDirections != AdjacencyDirection.None;
        }

        private void Update()
        {
            if (renderers == null) CacheRenderers();
            float wave = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minimumAlpha, maximumAlpha, wave);

            foreach (SpriteRenderer renderer in renderers)
                SetAlpha(renderer, alpha);
        }

        private void ConfigureCardinal(
            SpriteRenderer renderer,
            AdjacencyDirection flag,
            AdjacencyDirection activeDirections,
            Vector2 direction,
            int range,
            float cellSize,
            bool everything)
        {
            if (renderer == null) return;
            renderer.enabled = !everything && (activeDirections & flag) != 0;
            renderer.transform.localPosition = direction * (((range + 1f) * 0.5f) * cellSize);
            bool vertical = Mathf.Abs(direction.y) > 0f;
            renderer.transform.localScale = vertical
                ? new Vector3(cellSize, range * cellSize, 1f)
                : new Vector3(range * cellSize, cellSize, 1f);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.color = effectColor;
        }

        private void ConfigureDiagonal(
            SpriteRenderer renderer,
            AdjacencyDirection flag,
            AdjacencyDirection activeDirections,
            Vector2 direction,
            int range,
            float cellSize,
            bool everything)
        {
            if (renderer == null) return;
            renderer.enabled = !everything && (activeDirections & flag) != 0;
            renderer.transform.localPosition = direction * (((range + 1f) * 0.5f) * cellSize);
            renderer.transform.localScale = new Vector3(cellSize, range * 1.4142f * cellSize, 1f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
            renderer.color = effectColor;
        }

        private void ConfigureEverything(SpriteRenderer renderer, bool enabled, int range, float cellSize)
        {
            if (renderer == null) return;
            renderer.enabled = enabled;
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            float size = (range * 2f + 1f) * cellSize;
            renderer.transform.localScale = new Vector3(size, size, 1f);
            renderer.color = effectColor;
        }

        private void CacheRenderers()
        {
            renderers = new[]
            {
                upRenderer, downRenderer, leftRenderer, rightRenderer,
                upLeftRenderer, upRightRenderer, downLeftRenderer, downRightRenderer,
                everythingRenderer
            };
        }

        private void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null || !renderer.enabled) return;
            Color color = effectColor;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
