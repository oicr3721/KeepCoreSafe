using System.Collections.Generic;
using KeepCoreSafe.Core;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class SupportBlock : CombatBlock
    {
        [Header("Electric Link Pool")]
        [Tooltip("Prefab containing ElectricLine and a configured LineRenderer.")]
        [SerializeField] private ElectricLine electricLinePrefab;
        [SerializeField, Min(0)] private int initialPoolSize = 4;
        [SerializeField] private Transform electricLineAttachPoint;

        private readonly Dictionary<Block, ElectricLine> activeLines = new();
        private readonly HashSet<Block> desiredTargets = new();
        private readonly List<Block> removedTargets = new();
        private ComponentPool<ElectricLine> linePool;
        private bool targetsDirty = true;
        private bool isGridSubscribed;

        private SupportBlockData SupportData => Data as SupportBlockData;

        protected override void Awake()
        {
            base.Awake();
            if (electricLinePrefab != null)
            {
                linePool = new ComponentPool<ElectricLine>(
                    electricLinePrefab,
                    initialPoolSize,
                    transform);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (GameManager.Phase != GamePhase.Combat && activeLines.Count > 0)
                ReleaseAllLines();
        }

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            TrySubscribeGridChanged();
            targetsDirty = true;
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            UnsubscribeGridChanged();
            ReleaseAllLines();
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            TrySubscribeGridChanged();
            if (targetsDirty)
                RefreshTargets();
        }

        private void RefreshTargets()
        {
            if (SupportData == null || linePool == null || GridManager.Instance == null || !HasGridPosition)
                return;

            targetsDirty = false;
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

        private void HandleGridChanged()
        {
            targetsDirty = true;
            if (GameManager.Phase == GamePhase.Combat)
                RefreshTargets();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Combat)
            {
                targetsDirty = true;
                RefreshTargets();
            }
            else
            {
                ReleaseAllLines();
            }
        }

        private void TrySubscribeGridChanged()
        {
            if (isGridSubscribed || GridManager.Instance == null)
                return;

            GridManager.Instance.GridChanged += HandleGridChanged;
            isGridSubscribed = true;
        }

        private void UnsubscribeGridChanged()
        {
            if (!isGridSubscribed)
                return;

            if (GridManager.Instance != null)
                GridManager.Instance.GridChanged -= HandleGridChanged;
            isGridSubscribed = false;
        }

        protected override void OnDestroy()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            UnsubscribeGridChanged();
            ReleaseAllLines();
            base.OnDestroy();
        }
    }
}
