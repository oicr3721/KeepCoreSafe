using System.Collections.Generic;
using KeepCoreSafe.Core;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class SupportBlock : Block
    {
        [Header("Electric Link Pool")]
        [Tooltip("Prefab containing ElectricLine and a configured LineRenderer.")]
        [SerializeField] private ElectricLine electricLinePrefab;
        [SerializeField, Min(0)] private int initialPoolSize = 4;
        [SerializeField] private Transform electricLineRoot;
        [SerializeField] private Transform electricLineAttachPoint;
        [SerializeField, Min(0.02f)] private float targetRefreshInterval = 0.15f;

        private readonly Dictionary<Block, ElectricLine> activeLines = new();
        private readonly HashSet<Block> desiredTargets = new();
        private readonly List<Block> removedTargets = new();
        private ComponentPool<ElectricLine> linePool;
        private float refreshRemaining;

        private SupportBlockData SupportData => Data as SupportBlockData;

        protected override void Awake()
        {
            base.Awake();
            if (electricLinePrefab != null)
            {
                linePool = new ComponentPool<ElectricLine>(
                    electricLinePrefab,
                    initialPoolSize,
                    electricLineRoot != null ? electricLineRoot : transform);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (GameManager.Phase != GamePhase.Combat && activeLines.Count > 0)
                ReleaseAllLines();
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (SupportData == null || linePool == null || GridManager.Instance == null || !HasGridPosition)
                return;

            refreshRemaining -= deltaTime;
            if (refreshRemaining > 0f)
                return;

            refreshRemaining = targetRefreshInterval;
            RefreshTargets();
        }

        private void RefreshTargets()
        {
            desiredTargets.Clear();
            foreach (Block block in GridManager.Instance.GetBlocksInEffectArea(
                         GridPosition,
                         SupportData.AffectedDirections,
                         SupportData.EffectRange))
            {
                if (block != null 
                    && block != this 
                    && block.HP.CurrentValue > 0f
                    && block.Data is TimedAreaBlockData)
                    desiredTargets.Add(block);
            }

            removedTargets.Clear();
            foreach (KeyValuePair<Block, ElectricLine> pair in activeLines)
            {
                if (pair.Key == null || !desiredTargets.Contains(pair.Key))
                    removedTargets.Add(pair.Key);
            }

            foreach (Block block in removedTargets)
            {
                if (activeLines.Remove(block, out ElectricLine line))
                    line.Release();
            }

            foreach (Block block in desiredTargets)
            {
                if (activeLines.ContainsKey(block))
                    continue;

                ElectricLine line = linePool.Rent();
                if (line == null)
                    continue;

                line.Play(electricLineAttachPoint, block.transform, linePool.Return);
                activeLines.Add(block, line);
            }
        }

        private void ReleaseAllLines()
        {
            foreach (ElectricLine line in activeLines.Values)
                line?.Release();
            activeLines.Clear();
            desiredTargets.Clear();
        }

        protected override void OnDestroy()
        {
            ReleaseAllLines();
            base.OnDestroy();
        }
    }
}
