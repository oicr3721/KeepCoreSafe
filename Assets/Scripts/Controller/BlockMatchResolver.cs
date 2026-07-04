using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Controllers
{
    public sealed class BlockMatchResolver
    {
        public readonly struct MatchResult
        {
            public MatchResult(Vector2Int position, BlockData resultBlock, IReadOnlyList<Block> consumedBlocks)
            {
                Position = position;
                ResultBlock = resultBlock;
                ConsumedBlocks = consumedBlocks;
            }

            public Vector2Int Position { get; }
            public BlockData ResultBlock { get; }
            public IReadOnlyList<Block> ConsumedBlocks { get; }
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        private readonly GridManager gridManager;
        private readonly BlockMatchData matchData;

        public BlockMatchResolver(GridManager gridManager, BlockMatchData matchData)
        {
            this.gridManager = gridManager;
            this.matchData = matchData;
        }

        public bool TryResolve(Vector2Int lastPlacedPosition, out MatchResult result)
        {
            result = default;
            if (gridManager == null
                || matchData == null
                || !gridManager.TryGetBlock(lastPlacedPosition, out Block start)
                || start.Data is not BasicBlockData basicData
                || basicData.Color == null
                || !matchData.TryGetRule(basicData.Color, out BlockMatchData.Rule rule))
            {
                return false;
            }

            List<Block> selected = new(rule.RequiredCount);
            Queue<Block> frontier = new();
            HashSet<Block> visited = new();
            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Count > 0 && selected.Count < rule.RequiredCount)
            {
                Block current = frontier.Dequeue();
                selected.Add(current);

                foreach (Vector2Int direction in CardinalDirections)
                {
                    if (!gridManager.TryGetBlock(current.GridPosition + direction, out Block neighbor)
                        || visited.Contains(neighbor)
                        || neighbor.Data is not BasicBlockData neighborData
                        || neighborData.Color != basicData.Color)
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            if (selected.Count < rule.RequiredCount)
                return false;

            result = new MatchResult(lastPlacedPosition, rule.ResultBlock, selected);
            return true;
        }
    }
}
