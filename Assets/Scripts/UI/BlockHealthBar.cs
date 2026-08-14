using KeepCoreSafe.Blocks;
using KeepCoreSafe.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class BlockHealthBar : SliderUI
    {
        [Header("References")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Image fill;
        [SerializeField] private UIFollowTarget uiFollowTarget;

        [Header("Visibility")]
        [SerializeField, Min(0f)] private float combatVisibleDuration = 1.25f;

        [Header("Health Colors")]
        [SerializeField] private Color healthyColor = new Color(0.55f, 1f, 0.35f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.12f, 0.08f, 1f);
        [SerializeField, Range(0f, 1f)] private float warningThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float criticalThreshold = 0.2f;

        private float combatVisibilityRemaining;

        public void Initialize(Block owner)
        {
            Initialize(owner.HP);

            GameManager.PhaseChanged -= HandlePhaseChanged;
            GameManager.PhaseChanged += HandlePhaseChanged;

            uiFollowTarget.SetTarget(owner.transform);
        }

        private void Update()
        {
            if (GameManager.Phase != GamePhase.Combat
                || combatVisibilityRemaining <= 0f)
            {
                return;
            }

            combatVisibilityRemaining = Mathf.Max(
                0f,
                combatVisibilityRemaining - Time.unscaledDeltaTime);
            if (combatVisibilityRemaining <= 0f)
                SetVisible(false);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            combatVisibilityRemaining = GameManager.Phase == GamePhase.Preparation
                ? 0f
                : combatVisibleDuration;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (source == null)
            {
                SetVisible(false);
                return;
            }

            bool visible = (GameManager.Phase == GamePhase.Preparation
                           && source.CurrentValue < source.MaxValue);
            SetVisible(visible);
        }

        protected override void OnRefresh()
        {
            if (source == null) return;

            if (GameManager.Phase == GamePhase.Combat)
            {
                combatVisibilityRemaining = combatVisibleDuration;
                SetVisible(true);
            }
            else
            {
                RefreshVisibility();
            }
        }

        public void UpdateHealthVisual(float healthRatio)
        {
            if (fill == null)
                return;

            float ratio = Mathf.Clamp01(healthRatio);
            fill.color = ratio <= criticalThreshold
                ? criticalColor
                : ratio <= warningThreshold ? warningColor : healthyColor;
        }

        private void SetVisible(bool visible)
        {
            if (visualRoot != null && visualRoot.activeSelf != visible)
                visualRoot.SetActive(visible);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }
    }
}
