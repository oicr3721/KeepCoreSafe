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
        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private readonly GridManager gridManager;
        private readonly EnemyData enemyData;
        private readonly int navigationSeed;
        private readonly Vector2Int[] searchDirections;

        public GridPathfinder(GridManager gridManager, EnemyData enemyData, int navigationSeed = 0)
        {
            this.gridManager = gridManager;
            this.enemyData = enemyData;
            this.navigationSeed = navigationSeed != 0
                ? navigationSeed
                : UnityEngine.Random.Range(1, int.MaxValue);
            searchDirections = CreateShuffledDirections(this.navigationSeed);
        }

        public bool TryBuildPath(Vector3 origin, Block core, out PathResult result)
        {
            return TryBuildPath(GetStartCandidates(origin), core, out result);
        }

        public bool TryBuildPath(Vector2Int start, Block core, out PathResult result)
        {
            List<Vector2Int> starts = new List<Vector2Int>();
            if (gridManager.Grid.IsWithinBounds(start)) starts.Add(start);
            return TryBuildPath(starts, core, out result);
        }

        private bool TryBuildPath(List<Vector2Int> starts, Block core, out PathResult result)
        {
            result = null;
            if (core == null || !core.HasGridPosition || starts.Count == 0)
                return false;

            List<Vector2Int> coreGoals = GetAvailableCoreApproaches(core.GridPosition);
            foreach (Vector2Int start in starts)
            {
                if (!CanUseStart(start)
                    || !TryFindPath(start, coreGoals, core.GridPosition, out List<Vector2Int> shortestPath))
                {
                    continue;
                }

                List<Vector2Int> selectedPath = shortestPath;
                List<Vector2Int> preferredGoal = new List<Vector2Int> { coreGoals[0] };
                if (TryFindPath(start, preferredGoal, core.GridPosition, out List<Vector2Int> preferredPath)
                    && preferredPath.Count <= shortestPath.Count + enemyData.MaxPreferredPathExtraCells)
                {
                    selectedPath = preferredPath;
                }

                result = new PathResult(selectedPath, null, true);
                return true;
            }

            PathResult bestFallback = null;
            int bestCoreDistance = int.MaxValue;
            int bestPathLength = int.MaxValue;

            foreach (Vector2Int start in starts)
            {
                if (!CanUseStart(start)
                    || !TryFindClosestReachableCell(
                        start,
                        core.GridPosition,
                        out List<Vector2Int> fallbackPath,
                        out Vector2Int targetCell))
                {
                    continue;
                }

                int coreDistance = GetManhattanDistance(targetCell, core.GridPosition);
                if (coreDistance > bestCoreDistance
                    || coreDistance == bestCoreDistance && fallbackPath.Count >= bestPathLength)
                {
                    continue;
                }

                Block blocker = FindBlockingBlock(targetCell, core.GridPosition);
                bestFallback = new PathResult(fallbackPath, blocker, false);
                bestCoreDistance = coreDistance;
                bestPathLength = fallbackPath.Count;
            }

            result = bestFallback;
            return result != null;
        }

        private bool TryFindPath(
            Vector2Int start,
            List<Vector2Int> goals,
            Vector2Int corePosition,
            out List<Vector2Int> path)
        {
            path = null;
            if (goals.Count == 0)
                return false;

            int width = gridManager.Width;
            int height = gridManager.Height;
            float[,] gScores = CreateScoreMap(width, height);
            bool[,] closed = new bool[width, height];
            Vector2Int?[,] parents = new Vector2Int?[width, height];
            HashSet<Vector2Int> open = new HashSet<Vector2Int> { start };
            gScores[start.x, start.y] = 0f;

            while (open.Count > 0)
            {
                Vector2Int current = FindLowestScore(open, gScores, goals);
                if (goals.Contains(current))
                {
                    path = ReconstructPath(current, parents);
                    return true;
                }

                open.Remove(current);
                closed[current.x, current.y] = true;

                foreach (Vector2Int direction in searchDirections)
                {
                    Vector2Int next = current + direction;
                    if (!gridManager.Grid.IsWithinBounds(next)
                        || next == corePosition
                        || closed[next.x, next.y]
                        || IsBlockOccupied(next))
                    {
                        continue;
                    }

                    float tentativeScore = gScores[current.x, current.y] + 1f;
                    if (tentativeScore >= gScores[next.x, next.y])
                        continue;

                    parents[next.x, next.y] = current;
                    gScores[next.x, next.y] = tentativeScore;
                    open.Add(next);
                }
            }

            return false;
        }

        private bool TryFindClosestReachableCell(
            Vector2Int start,
            Vector2Int corePosition,
            out List<Vector2Int> path,
            out Vector2Int targetCell)
        {
            int width = gridManager.Width;
            int height = gridManager.Height;
            bool[,] visited = new bool[width, height];
            Vector2Int?[,] parents = new Vector2Int?[width, height];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            bool found = false;
            targetCell = start;
            int bestCoreDistance = int.MaxValue;
            int bestTieBreak = int.MaxValue;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current != corePosition)
                {
                    int coreDistance = GetManhattanDistance(current, corePosition);
                    int tieBreak = GetTieBreak(current);
                    if (coreDistance < bestCoreDistance
                        || coreDistance == bestCoreDistance && tieBreak < bestTieBreak)
                    {
                        targetCell = current;
                        bestCoreDistance = coreDistance;
                        bestTieBreak = tieBreak;
                        found = true;
                    }
                }

                foreach (Vector2Int direction in searchDirections)
                {
                    Vector2Int next = current + direction;
                    if (!gridManager.Grid.IsWithinBounds(next)
                        || visited[next.x, next.y]
                        || next == corePosition
                        || IsBlockOccupied(next))
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    parents[next.x, next.y] = current;
                    queue.Enqueue(next);
                }
            }

            path = found ? ReconstructPath(targetCell, parents) : null;
            return found;
        }

        private Block FindBlockingBlock(Vector2Int targetCell, Vector2Int corePosition)
        {
            Block best = null;
            int bestCoreDistance = int.MaxValue;
            int bestPriority = int.MaxValue;
            int bestTieBreak = int.MaxValue;

            foreach (Vector2Int direction in searchDirections)
            {
                Vector2Int position = targetCell + direction;
                if (!gridManager.Grid.TryGetCell(position, out GridCell cell)
                    || !cell.IsOccupied
                    || (cell.Occupant.BlockProperty & BlockProperty.Core) != 0)
                {
                    continue;
                }

                int coreDistance = GetManhattanDistance(position, corePosition);
                int priority = enemyData.GetPriority(cell.Occupant.BlockProperty);
                int tieBreak = GetTieBreak(position);
                if (coreDistance < bestCoreDistance
                    || (coreDistance == bestCoreDistance && priority < bestPriority)
                    || (coreDistance == bestCoreDistance
                        && priority == bestPriority
                        && tieBreak < bestTieBreak))
                {
                    best = cell.Occupant;
                    bestCoreDistance = coreDistance;
                    bestPriority = priority;
                    bestTieBreak = tieBreak;
                }
            }

            return best;
        }

        private bool CanUseStart(Vector2Int start)
        {
            return !IsBlockOccupied(start);
        }

        private List<Vector2Int> GetStartCandidates(Vector3 origin)
        {
            Vector2Int gridPosition = gridManager.WorldToGrid(origin);
            List<Vector2Int> candidates = new List<Vector2Int>();

            if (gridManager.Grid.IsWithinBounds(gridPosition))
            {
                candidates.Add(gridPosition);
                foreach (Vector2Int direction in searchDirections)
                {
                    Vector2Int neighbor = gridPosition + direction;
                    if (gridManager.Grid.IsWithinBounds(neighbor)) AddUnique(candidates, neighbor);
                }
            }
            else
            {
                Vector2Int nearestBoundary = new Vector2Int(
                    Mathf.Clamp(gridPosition.x, 0, gridManager.Width - 1),
                    Mathf.Clamp(gridPosition.y, 0, gridManager.Height - 1));
                candidates.Add(nearestBoundary);
                foreach (Vector2Int direction in searchDirections)
                {
                    Vector2Int neighbor = nearestBoundary + direction;
                    if (gridManager.Grid.IsWithinBounds(neighbor)) AddUnique(candidates, neighbor);
                }
            }

            candidates.Sort((a, b) => Vector3.Distance(origin, gridManager.GridToWorld(a))
                .CompareTo(Vector3.Distance(origin, gridManager.GridToWorld(b))));
            return candidates;
        }

        private List<Vector2Int> GetAvailableCoreApproaches(Vector2Int corePosition)
        {
            List<Vector2Int> goals = new List<Vector2Int>();
            foreach (Vector2Int direction in searchDirections)
            {
                Vector2Int position = corePosition + direction;
                if (gridManager.Grid.IsWithinBounds(position))
                {
                    goals.Add(position);
                }
            }

            return goals;
        }

        private bool IsBlockOccupied(Vector2Int position)
        {
            return gridManager.Grid.TryGetCell(position, out GridCell cell) && cell.IsOccupied;
        }

        private static float[,] CreateScoreMap(int width, int height)
        {
            float[,] scores = new float[width, height];
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                scores[x, y] = float.PositiveInfinity;
            return scores;
        }

        private Vector2Int FindLowestScore(
            HashSet<Vector2Int> open,
            float[,] gScores,
            List<Vector2Int> goals)
        {
            Vector2Int best = default;
            float bestScore = float.PositiveInfinity;
            int bestTieBreak = int.MaxValue;
            foreach (Vector2Int position in open)
            {
                float score = gScores[position.x, position.y] + GetHeuristic(position, goals);
                int tieBreak = GetTieBreak(position);
                if (score < bestScore
                    || (Mathf.Approximately(score, bestScore) && tieBreak < bestTieBreak))
                {
                    best = position;
                    bestScore = score;
                    bestTieBreak = tieBreak;
                }
            }

            return best;
        }

        private int GetTieBreak(Vector2Int position)
        {
            unchecked
            {
                uint hash = (uint)navigationSeed;
                hash ^= (uint)position.x * 0x9E3779B9u;
                hash = hash << 13 | hash >> 19;
                hash ^= (uint)position.y * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private static Vector2Int[] CreateShuffledDirections(int seed)
        {
            Vector2Int[] directions = (Vector2Int[])Directions.Clone();
            System.Random random = new System.Random(seed);
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (directions[i], directions[swapIndex]) = (directions[swapIndex], directions[i]);
            }

            return directions;
        }

        private static int GetHeuristic(Vector2Int position, List<Vector2Int> goals)
        {
            int best = int.MaxValue;
            foreach (Vector2Int goal in goals)
            {
                int distance = GetManhattanDistance(position, goal);
                if (distance < best) best = distance;
            }

            return best;
        }

        private static int GetManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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
            public IReadOnlyList<Vector2Int> Cells { get; }
            public Block BlockingBlock { get; }
            public bool ReachesCore { get; }

            public PathResult(
                IReadOnlyList<Vector2Int> cells,
                Block blockingBlock,
                bool reachesCore)
            {
                Cells = cells;
                BlockingBlock = blockingBlock;
                ReachesCore = reachesCore;
            }
        }
    }
}
