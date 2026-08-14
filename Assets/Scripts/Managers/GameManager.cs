using System;
using System.Collections;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KeepCoreSafe.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Game Systems")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private WaveDifficultyController difficultyController;
        [SerializeField] private CoreEnergyController coreEnergyController;
        [SerializeField] private ShopEventController shopEventController;

        [Header("Combat Setting")]
        [SerializeField] private StageClearPresentationController stageClearPresentation;

        [Header("Core Destruction Presentation")]
        [SerializeField, Range(0.01f, 1f)] private float coreDeathTimeScale = 0.15f;
        [SerializeField, Min(0f)] private float coreDeathPresentationDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float coreDeathCameraZoom = 2.4f;
        [SerializeField, Min(0f)] private float coreDeathCameraDuration = 0.28f;

        public static GameManager Instance { get; private set; }

        private int timeScaleIndex;
        private Coroutine coreDeathRoutine;
        private bool isCoreDestructionPlaying;
        private bool isStageClearPlaying;
        private bool isPostWaveTransitionPlaying;
        private WaveDifficultySnapshot preparedDifficulty;
        private int preparedWaveIndex = -1;

        private static readonly float[] TimeScaleOptions = { 1f, 2f, 4f };

        public static GamePhase Phase { get; private set; } = GamePhase.Preparation;
        public static int WaveIndex { get; private set; }
        public ObservableInt CoreEnergy => coreEnergyController?.Energy;
        public float CurrentTimeScale => TimeScaleOptions[timeScaleIndex];

        public static event Action<GamePhase> PhaseChanged;
        public static event Action<float> TimeScaleChanged;
        public static event Action<int> WaveStarted;
        public static event Action<int, ClearType> StageCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Phase = GamePhase.Preparation;
            WaveIndex = 0;
            if (waveManager == null || difficultyController == null || coreEnergyController == null)
            {
                Debug.LogError("GameManager system references are incomplete.", this);
                enabled = false;
                return;
            }
            SetTimeScale(1f);
            RollNextWaveDifficulty(WaveIndex + 1);
        }

        private void Start()
        {
            waveManager.WaveCompleted += HandleWaveCompleted;
            GridManager.Instance.CoreDestroyed += HandleCoreDestroyed;

            PrepareWaveSpawnData();
        }

        public bool TryStartCombat()
        {
            if (isStageClearPlaying || isCoreDestructionPlaying || isPostWaveTransitionPlaying)
                return false;

            if (GridManager.Instance.Grid.Core == null)
            {
                Debug.LogWarning("Combat cannot start without a Core.");
                return false;
            }

            WaveIndex++;
            if (preparedWaveIndex != WaveIndex)
                RollNextWaveDifficulty(WaveIndex);
            if (!waveManager.HasPreparedWave(WaveIndex))
                PrepareWaveSpawnData();

            WaveDifficultySnapshot difficulty = preparedDifficulty;
            coreEnergyController?.BeginWave(difficulty.RequiredEnergy);
            SetPhase(GamePhase.Combat);
            WaveStarted?.Invoke(WaveIndex);
            waveManager.StartWave(WaveIndex, difficulty);
            preparedWaveIndex = -1;
            return true;
        }

        private void HandleWaveCompleted()
        {
            if (Phase == GamePhase.Combat && !isCoreDestructionPlaying && !isStageClearPlaying)
                BeginPostWaveTransition(ClearType.KillAllEnemies);
        }

        private void HandleCoreDestroyed()
        {
            if (isStageClearPlaying)
            {
                stageClearPresentation?.Cancel();
                isStageClearPlaying = false;
            }

            isCoreDestructionPlaying = false;
            waveManager.StopWave();
            SetPhase(GamePhase.GameOver);
        }

        public bool TryPlayCoreDestruction(CoreBlock core, Action onComplete)
        {
            if (core == null || onComplete == null || isCoreDestructionPlaying)
                return false;

            if (isStageClearPlaying)
            {
                stageClearPresentation?.Cancel();
                isStageClearPlaying = false;
                RestoreNormalTimeScale();
            }

            isCoreDestructionPlaying = true;
            coreDeathRoutine = StartCoroutine(PlayCoreDestruction(core, onComplete));
            return true;
        }

        private IEnumerator PlayCoreDestruction(CoreBlock core, Action onComplete)
        {
            Time.timeScale = coreDeathTimeScale;
            GameCameraController.Instance?.PlayCoreDeathFocus(
                core.transform,
                coreDeathCameraZoom,
                coreDeathCameraDuration);

            yield return new WaitForSecondsRealtime(coreDeathPresentationDuration);
            coreDeathRoutine = null;
            onComplete.Invoke();
        }

        public void CycleTimeScale()
        {
            if (isCoreDestructionPlaying || isStageClearPlaying)
                return;

            timeScaleIndex = (timeScaleIndex + 1) % TimeScaleOptions.Length;
            SetTimeScale(TimeScaleOptions[timeScaleIndex]);
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            if (phase == GamePhase.Preparation)
                RollNextWaveDifficulty(WaveIndex + 1);
            if (phase == GamePhase.GameOver)
            {
                timeScaleIndex = 0;
                SetTimeScale(1f);
            }

            PhaseChanged?.Invoke(phase);
            // Supply events are resolved/created by preparation listeners first so the
            // prepared spawn list can include its additional hunters and fixed routes.
            if (phase == GamePhase.Preparation)
                PrepareWaveSpawnData();
            Debug.Log($"Game Phase: {phase}");
        }

        public void RefreshPreparedWave()
        {
            if (Phase != GamePhase.Preparation || preparedWaveIndex < 1)
                return;

            waveManager.PrepareWave(preparedWaveIndex, preparedDifficulty);
        }

        private void RollNextWaveDifficulty(int waveIndex)
        {
            preparedDifficulty = difficultyController != null
                ? difficultyController.RollForWave(waveIndex)
                : default;
            preparedWaveIndex = waveIndex;
            coreEnergyController?.BeginPreparation(preparedDifficulty.RequiredEnergy);
        }

        private void PrepareWaveSpawnData()
        {
            if (preparedWaveIndex < 1)
                return;

            waveManager.PrepareWave(preparedWaveIndex, preparedDifficulty);
        }

        private void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            TimeScaleChanged?.Invoke(scale);
        }

        internal void SetPresentationTimeScale(float scale)
        {
            SetTimeScale(Mathf.Max(0.01f, scale));
        }

        internal void RestoreNormalTimeScale()
        {
            timeScaleIndex = 0;
            SetTimeScale(1f);
        }

        public void AwardEnemyEnergy(Vector3 origin, int amount)
        {
            coreEnergyController?.AwardEnemyEnergy(origin, amount);
        }

        public bool CanApplyRerollCost(int cost)
        {
            return coreEnergyController != null && coreEnergyController.CanApplyRerollCost(cost);
        }

        public bool TryApplyRerollCost(int cost)
        {
            return coreEnergyController != null && coreEnergyController.TryApplyRerollCost(cost);
        }

        public void ResetCoreEnergy()
        {
            coreEnergyController?.ResetEnergy();
        }

        public void TriggerEnergyShockwave()
        {
            if (Phase == GamePhase.Combat && !isCoreDestructionPlaying && !isStageClearPlaying)
                BeginStageClearPresentation();
        }

        private void BeginStageClearPresentation()
        {
            if (isStageClearPlaying)
                return;

            CoreBlock core = GridManager.Instance?.Grid?.Core as CoreBlock;
            if (core == null || stageClearPresentation == null)
            {
                Debug.LogWarning(
                    "Stage clear presentation is not configured. Falling back to Preparation.",
                    this);
                waveManager.StopWave();
                BeginPostWaveTransition(ClearType.ShockWave);
                return;
            }

            isStageClearPlaying = true;
            waveManager.StopSpawning();
            if (!stageClearPresentation.Play(core, waveManager.ActiveEnemies, CompleteStageClear))
            {
                isStageClearPlaying = false;
                waveManager.StopWave();
                BeginPostWaveTransition(ClearType.ShockWave);
            }
        }

        private void CompleteStageClear()
        {
            RestoreNormalTimeScale();
            waveManager.StopWave();
            isStageClearPlaying = false;
            BeginPostWaveTransition(ClearType.ShockWave);
        }

        private void BeginPostWaveTransition(ClearType clearType)
        {
            if (isPostWaveTransitionPlaying || Phase != GamePhase.Combat)
                return;

            isPostWaveTransitionPlaying = true;
            StageCleared?.Invoke(WaveIndex, clearType);
            if (shopEventController != null
                && shopEventController.TryStartPostWaveSupplySequence(WaveIndex, EnterPreparation))
            {
                return;
            }

            EnterPreparation();
        }

        private void EnterPreparation()
        {
            if (!isPostWaveTransitionPlaying)
                return;

            isPostWaveTransitionPlaying = false;
            SetPhase(GamePhase.Preparation);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            waveManager.WaveCompleted -= HandleWaveCompleted;
            if (GridManager.Instance != null)
                GridManager.Instance.CoreDestroyed -= HandleCoreDestroyed;

            Time.timeScale = 1f;
            if (coreDeathRoutine != null)
                StopCoroutine(coreDeathRoutine);
            stageClearPresentation?.Cancel();
            Instance = null;
        }
    }
}
