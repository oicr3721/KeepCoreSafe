using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.GridSystem;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class GridPathfinder
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private readonly GridManager gridManager;
        private readonly EnemyData enemyData;

        public GridPathfinder(GridManager gridManager, EnemyData enemyData)
        {
            this.gridManager = gridManager;
            this.enemyData = enemyData;
        }

        public bool TryBuildPath(Vector3 origin, Block core, out PathResult result)
        {
            result = null;
            if (core == null || !core.HasGridPosition)
            {
                return false;
            }

            List<Vector2Int> goals = GetCoreApproaches(core.GridPosition);
            List<Vector2Int> starts = GetStartCandidates(origin);
            if (goals.Count == 0 || starts.Count == 0)
            {
                return false;
            }

            foreach (Vector2Int start in starts)
            {
                if (IsOccupied(start) || !TryFindPath(start, goals, false, core.GridPosition, out List<Vector2Int> path))
                {
                    continue;
                }

                result = CreateResult(path);
                return true;
            }

            if (TryFindPath(starts[0], goals, true, core.GridPosition, out List<Vector2Int> breakingPath))
            {
                result = CreateResult(breakingPath);
                return true;
            }

            return false;
        }

        private bool TryFindPath(
            Vector2Int start,
            List<Vector2Int> goals,
            bool allowBlocks,
            Vector2Int corePosition,
            out List<Vector2Int> path)
        {
            int width = gridManager.Width;
            int height = gridManager.Height;
            float[,] costs = CreateCostMap(width, height);
            bool[,] visited = new bool[width, height];
            Vector2Int?[,] parents = new Vector2Int?[width, height];
            costs[start.x, start.y] = 0f;

            for (int i = 0; i < width * height; i++)
            {
                Vector2Int current = FindLowestCostCell(costs, visited);
                if (current.x < 0)
                {
                    break;
                }

                if (goals.Contains(current))
                {
                    path = ReconstructPath(current, parents);
                    return true;
                }

                visited[current.x, current.y] = true;
                VisitNeighbors(current, corePosition, allowBlocks, costs, visited, parents);
            }

            path = null;
            return false;
        }

        private void VisitNeighbors(
            Vector2Int current,
            Vector2Int corePosition,
            bool allowBlocks,
            float[,] costs,
            bool[,] visited,
            Vector2Int?[,] parents)
        {
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = current + direction;
                if (!gridManager.Grid.IsWithinBounds(next) || next == corePosition || visited[next.x, next.y])
                {
                    continue;
                }

                bool occupied = IsOccupied(next);
                if (occupied && !allowBlocks)
                {
                    continue;
                }

                float newCost = costs[current.x, current.y] + 1f + GetObstacleCost(next, occupied);
                if (newCost < costs[next.x, next.y])
                {
                    costs[next.x, next.y] = newCost;
                    parents[next.x, next.y] = current;
                }
            }
        }

        private float GetObstacleCost(Vector2Int position, bool occupied)
        {
            if (!occupied)
            {
                return 0f;
            }

            gridManager.Grid.TryGetCell(position, out GridCell cell);
            return 100f + enemyData.GetPriority(cell.Occupant.BlockProperty);
        }

        private List<Vector2Int> GetStartCandidates(Vector3 origin)
        {
            Vector2Int gridPosition = gridManager.WorldToGrid(origin);
            List<Vector2Int> candidates = new List<Vector2Int>();

            if (gridManager.Grid.IsWithinBounds(gridPosition))
            {
                candidates.Add(gridPosition);
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighbor = gridPosition + direction;
                    if (gridManager.Grid.IsWithinBounds(neighbor)) candidates.Add(neighbor);
                }
            }
            else
            {
                AddBoundaryCells(candidates);
            }

            candidates.Sort((a, b) => Vector3.Distance(origin, gridManager.GridToWorld(a))
                .CompareTo(Vector3.Distance(origin, gridManager.GridToWorld(b))));
            return candidates;
        }

        private void AddBoundaryCells(List<Vector2Int> cells)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                AddUnique(cells, new Vector2Int(x, 0));
                AddUnique(cells, new Vector2Int(x, gridManager.Height - 1));
            }

            for (int y = 0; y < gridManager.Height; y++)
            {
                AddUnique(cells, new Vector2Int(0, y));
                AddUnique(cells, new Vector2Int(gridManager.Width - 1, y));
            }
        }

        private List<Vector2Int> GetCoreApproaches(Vector2Int corePosition)
        {
            List<Vector2Int> goals = new List<Vector2Int>();
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int position = corePosition + direction;
                if (gridManager.Grid.IsWithinBounds(position)) goals.Add(position);
            }

            return goals;
        }

        private PathResult CreateResult(List<Vector2Int> path)
        {
            List<Vector3> waypoints = new List<Vector3>();
            Block blocker = null;

            foreach (Vector2Int position in path)
            {
                gridManager.Grid.TryGetCell(position, out GridCell cell);
                if (cell.IsOccupied)
                {
                    blocker = cell.Occupant;
                    break;
                }

                waypoints.Add(gridManager.GridToWorld(position));
            }

            return new PathResult(waypoints, blocker);
        }

        private bool IsOccupied(Vector2Int position)
        {
            return gridManager.Grid.TryGetCell(position, out GridCell cell) && cell.IsOccupied;
        }

        private static float[,] CreateCostMap(int width, int height)
        {
            float[,] costs = new float[width, height];
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                costs[x, y] = float.PositiveInfinity;
            return costs;
        }

        private static Vector2Int FindLowestCostCell(float[,] costs, bool[,] visited)
        {
            Vector2Int best = new Vector2Int(-1, -1);
            float bestCost = float.PositiveInfinity;

            for (int x = 0; x < costs.GetLength(0); x++)
            for (int y = 0; y < costs.GetLength(1); y++)
            {
                if (!visited[x, y] && costs[x, y] < bestCost)
                {
                    best = new Vector2Int(x, y);
                    bestCost = costs[x, y];
                }
            }

            return best;
        }

        private static List<Vector2Int> ReconstructPath(Vector2Int end, Vector2Int?[,] parents)
        {
            List<Vector2Int> path = new List<Vector2Int> { end };
            Vector2Int current = end;

            while (parents[current.x, current.y].HasValue)
            {
                current = parents[current.x, current.y].Value;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static void AddUnique(List<Vector2Int> cells, Vector2Int position)
        {
            if (!cells.Contains(position)) cells.Add(position);
        }

        public sealed class PathResult
        {
            public IReadOnlyList<Vector3> Waypoints { get; }
            public Block BlockingBlock { get; }

            public PathResult(IReadOnlyList<Vector3> waypoints, Block blockingBlock)
            {
                Waypoints = waypoints;
                BlockingBlock = blockingBlock;
            }
        }
    }
}
