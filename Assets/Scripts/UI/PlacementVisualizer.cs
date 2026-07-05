using System.Collections.Generic;
using KeepCoreSafe.Core;
using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class PlacementVisualizer : MonoBehaviour
    {
        [Header("Effect Cell Pool")]
        [Tooltip("A single-cell visual prefab reused for every affected Grid offset.")]
        [SerializeField] private EffectCellView effectCellPrefab;
        [SerializeField, Min(0)] private int initialPoolSize = 24;
        [SerializeField] private Transform effectCellRoot;

        [Header("Blink")]
        [SerializeField] private Color effectColor = new(0.2f, 0.85f, 1f, 0.45f);
        [SerializeField, Min(0.1f)] private float blinkSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.15f;
        [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.55f;

        private readonly List<EffectCellView> activeCells = new();
        private ComponentPool<EffectCellView> cellPool;
        private VisualRequest placementRequest;
        private VisualRequest hoverRequest;
        private BlockData configuredData;
        private float configuredCellSize;
        private bool hasConfiguration;

        private struct VisualRequest
        {
            public bool Active;
            public BlockData Data;
            public Vector3 Position;
            public float CellSize;
        }

        private void Awake()
        {
            if (effectCellPrefab != null)
            {
                cellPool = new ComponentPool<EffectCellView>(
                    effectCellPrefab,
                    initialPoolSize,
                    effectCellRoot != null ? effectCellRoot : transform);
            }
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

        private void RefreshRequest()
        {
            VisualRequest request = placementRequest.Active && IsRenderable(placementRequest.Data)
                ? placementRequest
                : hoverRequest;
            bool visible = request.Active && IsRenderable(request.Data) && cellPool != null;
            if (!visible)
            {
                ReleaseCells();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            transform.position = request.Position;
            if (!hasConfiguration
                || configuredData != request.Data
                || !Mathf.Approximately(configuredCellSize, request.CellSize))
            {
                Configure(request.Data, request.CellSize);
            }
        }

        private void Configure(BlockData blockData, float cellSize)
        {
            ReleaseCells();
            configuredData = blockData;
            configuredCellSize = cellSize;
            hasConfiguration = true;

            foreach (Vector2Int offset in GridEffectArea.EnumerateOffsets(
                         blockData.AffectedDirections,
                         blockData.EffectRange))
            {
                EffectCellView cell = cellPool.Rent();
                if (cell == null)
                    continue;

                cell.Show(offset, cellSize, effectColor);
                activeCells.Add(cell);
            }
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minimumAlpha, maximumAlpha, wave);
            foreach (EffectCellView cell in activeCells)
                cell?.SetAlpha(alpha, effectColor);
        }

        private void ReleaseCells()
        {
            if (cellPool != null)
            {
                foreach (EffectCellView cell in activeCells)
                    cellPool.Return(cell);
            }

            activeCells.Clear();
            configuredData = null;
            configuredCellSize = 0f;
            hasConfiguration = false;
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

        private void OnDestroy()
        {
            ReleaseCells();
        }
    }
}
