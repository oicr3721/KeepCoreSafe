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
        private readonly struct SearchState : IEquatable<SearchState>
        {
            public SearchState(Vector2Int position, int distance)
            {
                Position = position;
                Distance = distance;
            }

            public Vector2Int Position { get; }
            public int Distance { get; }

            public bool Equals(SearchState other) =>
                Position == other.Position && Distance == other.Distance;

            public override bool Equals(object obj) => obj is SearchState other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Position, Distance);
        }

        private sealed class SearchRecord
        {
            public int BlockingBlockCount;
            public SearchState? Parent;
            public int EqualParentCount = 1;
        }

        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private readonly GridManager gridManager;
        private readonly EnemyData enemyData;
        private readonly System.Random random;
        private readonly Vector2Int[] searchDirections;

        public GridPathfinder(GridManager gridManager, EnemyData enemyData, int navigationSeed = 0)
        {
            this.gridManager = gridManager;
            this.enemyData = enemyData;
            int seed = navigationSeed != 0
                ? navigationSeed
                : UnityEngine.Random.Range(1, int.MaxValue);
            random = new System.Random(seed);
            searchDirections = CreateShuffledDirections(random);
        }

        public bool TryBuildPath(Vector3 origin, Block core, out PathResult result)
        {
            foreach (Vector2Int start in GetStartCandidates(origin))
            {
                if (CanUseStart(start) && TryBuildPath(start, core, out result))
                    return true;
            }

            result = null;
            return false;
        }

        public bool TryBuildPath(Vector3 origin, Vector2Int target, out PathResult result)
        {
            foreach (Vector2Int start in GetStartCandidates(origin))
            {
                if (CanUseStart(start) && TryBuildPathToCell(start, target, out result))
                    return true;
            }

            result = null;
            return false;
        }

        public bool TryBuildPath(Vector2Int start, Block core, out PathResult result)
        {
            result = null;
            if (gridManager == null
                || gridManager.Grid == null
                || core == null
                || !core.HasGridPosition
                || !CanUseStart(start))
            {
                return false;
            }

            HashSet<Vector2Int> goals = GetCoreApproaches(core.GridPosition);
            return TryBuildPath(start, goals, core.GridPosition, out result);
        }

        private bool TryBuildPathToCell(
            Vector2Int start,
            Vector2Int target,
            out PathResult result)
        {
            result = null;
            if (gridManager == null
                || gridManager.Grid == null
                || !gridManager.Grid.IsWithinBounds(target)
                || !CanUseStart(start))
            {
                return false;
            }

            HashSet<Vector2Int> goals = new() { target };
            return TryBuildPath(start, goals, null, out result);
        }

        private bool TryBuildPath(
            Vector2Int start,
            HashSet<Vector2Int> goals,
            Vector2Int? excludedPosition,
            out PathResult result)
        {
            result = null;
            int shortestDistance = GetShortestDistance(start, goals, excludedPosition);
            if (shortestDistance < 0)
                return false;

            int maximumDistance = shortestDistance + Mathf.Max(0, enemyData.PathLengthTolerance);
            Dictionary<SearchState, SearchRecord> records = new();
            SearchState startState = new(start, 0);
            records[startState] = new SearchRecord { BlockingBlockCount = 0 };
            List<SearchState> currentLayer = new() { startState };

            SearchState? bestGoal = null;
            int bestBlockingBlockCount = int.MaxValue;
            int bestDistance = int.MaxValue;
            int equalGoalCount = 0;
            for (int distance = 0; distance <= maximumDistance && currentLayer.Count > 0; distance++)
            {
                List<SearchState> nextLayer = new();
                HashSet<SearchState> queuedNextStates = new();
                foreach (SearchState state in currentLayer)
                {
                    SearchRecord record = records[state];
                    if (goals.Contains(state.Position))
                    {
                        ConsiderGoal(
                            state,
                            record.BlockingBlockCount,
                            ref bestGoal,
                            ref bestBlockingBlockCount,
                            ref bestDistance,
                            ref equalGoalCount);
                    }

                    if (distance >= maximumDistance)
                        continue;

                    foreach (Vector2Int direction in searchDirections)
                    {
                        Vector2Int next = state.Position + direction;
                        if (!gridManager.Grid.IsWithinBounds(next)
                            || excludedPosition.HasValue && next == excludedPosition.Value)
                        {
                            continue;
                        }

                        SearchState nextState = new(next, distance + 1);
                        int nextBlockingCount = record.BlockingBlockCount
                            + (IsDestructibleBlockOccupied(next) ? 1 : 0);
                        if (!records.TryGetValue(nextState, out SearchRecord nextRecord))
                        {
                            nextRecord = new SearchRecord
                            {
                                BlockingBlockCount = nextBlockingCount,
                                Parent = state
                            };
                            records.Add(nextState, nextRecord);
                        }
                        else if (nextBlockingCount < nextRecord.BlockingBlockCount)
                        {
                            nextRecord.BlockingBlockCount = nextBlockingCount;
                            nextRecord.Parent = state;
                            nextRecord.EqualParentCount = 1;
                        }
                        else if (nextBlockingCount == nextRecord.BlockingBlockCount)
                        {
                            nextRecord.EqualParentCount++;
                            if (random.Next(nextRecord.EqualParentCount) == 0)
                                nextRecord.Parent = state;
                        }
                        else
                        {
                            continue;
                        }

                        if (queuedNextStates.Add(nextState))
                            nextLayer.Add(nextState);
                    }
                }

                currentLayer = nextLayer;
            }

            if (!bestGoal.HasValue)
                return false;

            result = new PathResult(
                ReconstructPath(bestGoal.Value, records),
                bestBlockingBlockCount,
                shortestDistance);
            return true;
        }

        private void ConsiderGoal(
            SearchState candidate,
            int blockingBlockCount,
            ref SearchState? bestGoal,
            ref int bestBlockingBlockCount,
            ref int bestDistance,
            ref int equalGoalCount)
        {
            if (blockingBlockCount < bestBlockingBlockCount
                || blockingBlockCount == bestBlockingBlockCount
                && candidate.Distance < bestDistance)
            {
                bestGoal = candidate;
                bestBlockingBlockCount = blockingBlockCount;
                bestDistance = candidate.Distance;
                equalGoalCount = 1;
                return;
            }

            if (blockingBlockCount != bestBlockingBlockCount || candidate.Distance != bestDistance)
                return;

            equalGoalCount++;
            if (random.Next(equalGoalCount) == 0)
                bestGoal = candidate;
        }

        private int GetShortestDistance(
            Vector2Int start,
            HashSet<Vector2Int> goals,
            Vector2Int? excludedPosition)
        {
            bool[,] visited = new bool[gridManager.Width, gridManager.Height];
            Queue<(Vector2Int Position, int Distance)> queue = new();
            queue.Enqueue((start, 0));
            visited[start.x, start.y] = true;

            while (queue.Count > 0)
            {
                (Vector2Int current, int distance) = queue.Dequeue();
                if (goals.Contains(current))
                    return distance;

                foreach (Vector2Int direction in searchDirections)
                {
                    Vector2Int next = current + direction;
                    if (!gridManager.Grid.IsWithinBounds(next)
                        || excludedPosition.HasValue && next == excludedPosition.Value
                        || visited[next.x, next.y])
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue((next, distance + 1));
                }
            }

            return -1;
        }

        private HashSet<Vector2Int> GetCoreApproaches(Vector2Int corePosition)
        {
            HashSet<Vector2Int> goals = new();
            foreach (Vector2Int direction in searchDirections)
            {
                Vector2Int position = corePosition + direction;
                if (gridManager.Grid.IsWithinBounds(position))
                    goals.Add(position);
            }

            return goals;
        }

        private bool CanUseStart(Vector2Int start)
        {
            return gridManager.Grid.IsWithinBounds(start) && !IsAnyBlockOccupied(start);
        }

        private bool IsAnyBlockOccupied(Vector2Int position)
        {
            return gridManager.Grid.TryGetCell(position, out GridCell cell) && cell.IsOccupied;
        }

        private bool IsDestructibleBlockOccupied(Vector2Int position)
        {
            return gridManager.Grid.TryGetCell(position, out GridCell cell)
                && cell.IsOccupied
                && (cell.Occupant.BlockProperty & BlockProperty.Core) == 0;
        }

        private List<Vector2Int> GetStartCandidates(Vector3 origin)
        {
            Vector2Int gridPosition = gridManager.WorldToGrid(origin);
            Vector2Int nearest = new(
                Mathf.Clamp(gridPosition.x, 0, gridManager.Width - 1),
                Mathf.Clamp(gridPosition.y, 0, gridManager.Height - 1));
            List<Vector2Int> candidates = new() { nearest };
            foreach (Vector2Int direction in searchDirections)
            {
                Vector2Int neighbor = nearest + direction;
                if (gridManager.Grid.IsWithinBounds(neighbor) && !candidates.Contains(neighbor))
                    candidates.Add(neighbor);
            }

            candidates.Sort((a, b) => Vector3.Distance(origin, gridManager.GridToWorld(a))
                .CompareTo(Vector3.Distance(origin, gridManager.GridToWorld(b))));
            return candidates;
        }

        private static IReadOnlyList<Vector2Int> ReconstructPath(
            SearchState end,
            IReadOnlyDictionary<SearchState, SearchRecord> records)
        {
            List<Vector2Int> path = new() { end.Position };
            SearchState current = end;
            while (records[current].Parent.HasValue)
            {
                current = records[current].Parent.Value;
                path.Add(current.Position);
            }

            path.Reverse();
            return path;
        }

        private static Vector2Int[] CreateShuffledDirections(System.Random random)
        {
            Vector2Int[] directions = (Vector2Int[])Directions.Clone();
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (directions[i], directions[swapIndex]) = (directions[swapIndex], directions[i]);
            }

            return directions;
        }

        public sealed class PathResult
        {
            public PathResult(
                IReadOnlyList<Vector2Int> cells,
                int blockingBlockCount,
                int shortestDistance)
            {
                Cells = cells;
                BlockingBlockCount = blockingBlockCount;
                ShortestDistance = shortestDistance;
            }

            public IReadOnlyList<Vector2Int> Cells { get; }
            public int BlockingBlockCount { get; }
            public int ShortestDistance { get; }
            public int SelectedDistance => Mathf.Max(0, Cells.Count - 1);
        }
    }
}
