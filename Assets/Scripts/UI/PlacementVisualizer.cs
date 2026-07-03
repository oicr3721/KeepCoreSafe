using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class PlacementVisualizer : MonoBehaviour
    {
        private readonly Dictionary<AdjacencyDirection, LineRenderer> directionLines = new();
        private LineRenderer rangeLine;
        private Material lineMaterial;

        private void Awake()
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            rangeLine = CreateLine("Effect Range", new Color(1f, 0.75f, 0.2f), 30);
            directionLines[AdjacencyDirection.Up] = CreateLine("Affects Up", Color.cyan, 31);
            directionLines[AdjacencyDirection.Down] = CreateLine("Affects Down", Color.cyan, 31);
            directionLines[AdjacencyDirection.Left] = CreateLine("Affects Left", Color.cyan, 31);
            directionLines[AdjacencyDirection.Right] = CreateLine("Affects Right", Color.cyan, 31);
            Hide();
        }

        public void Show(BlockData data, Vector3 center, float cellSize)
        {
            transform.position = center;
            DrawDirections(data.AffectedDirections, cellSize);
            DrawRange(data, cellSize);
        }

        public void Hide()
        {
            if (rangeLine != null) rangeLine.enabled = false;
            foreach (LineRenderer line in directionLines.Values) line.enabled = false;
        }

        private void DrawDirections(AdjacencyDirection directions, float cellSize)
        {
            SetDirection(AdjacencyDirection.Up, Vector2.up, directions, cellSize);
            SetDirection(AdjacencyDirection.Down, Vector2.down, directions, cellSize);
            SetDirection(AdjacencyDirection.Left, Vector2.left, directions, cellSize);
            SetDirection(AdjacencyDirection.Right, Vector2.right, directions, cellSize);
        }

        private void SetDirection(
            AdjacencyDirection flag,
            Vector2 direction,
            AdjacencyDirection activeDirections,
            float cellSize)
        {
            LineRenderer line = directionLines[flag];
            line.enabled = (activeDirections & flag) != 0;
            if (!line.enabled) return;

            line.positionCount = 2;
            line.SetPosition(0, direction * cellSize * 0.25f);
            line.SetPosition(1, direction * cellSize * 0.85f);
        }

        private void DrawRange(BlockData data, float cellSize)
        {
            bool hasRange = (data.Properties & (BlockProperty.Attack | BlockProperty.Healer | BlockProperty.Support)) != 0;
            rangeLine.enabled = hasRange && data.EffectRange > 0f;
            if (!rangeLine.enabled) return;

            float radius = (data.Properties & BlockProperty.Attack) != 0
                ? data.EffectRange
                : data.EffectRange * cellSize;
            const int segments = 48;
            rangeLine.positionCount = segments + 1;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                rangeLine.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private LineRenderer CreateLine(string lineName, Color color, int sortingOrder)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = 0.045f;
            line.endWidth = 0.045f;
            line.useWorldSpace = false;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
