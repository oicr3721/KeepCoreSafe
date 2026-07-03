using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.GridSystem
{
    [RequireComponent(typeof(GridManager))]
    public sealed class GridVisualizer : MonoBehaviour
    {
        [SerializeField]
        private Color lineColor = new Color(0.35f, 0.55f, 0.7f, 0.8f);

        [SerializeField, Min(0.005f)]
        private float lineWidth = 0.03f;

        private GridManager gridManager;
        private Material lineMaterial;

        private void Start()
        {
            gridManager = GetComponent<GridManager>();
            BuildRuntimeGrid();
        }

        private void BuildRuntimeGrid()
        {
            Shader shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);

            float halfWidth = gridManager.Width * 0.5f;
            float halfHeight = gridManager.Height * 0.5f;

            for (int x = 0; x <= gridManager.Width; x++)
            {
                float gridX = x - halfWidth;
                CreateLine($"Vertical {x}", gridX, -halfHeight, gridX, halfHeight);
            }

            for (int y = 0; y <= gridManager.Height; y++)
            {
                float gridY = y - halfHeight;
                CreateLine($"Horizontal {y}", -halfWidth, gridY, halfWidth, gridY);
            }
        }

        private void CreateLine(string lineName, float x1, float y1, float x2, float y2)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, GridPointToWorld(x1, y1));
            line.SetPosition(1, GridPointToWorld(x2, y2));
            line.sortingOrder = -1;
        }

        private Vector3 GridPointToWorld(float x, float y)
        {
            return transform.position +
                   new Vector3(x * gridManager.CellSize,
                               y * gridManager.CellSize,
                               0f);
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }
        }
    }
}
