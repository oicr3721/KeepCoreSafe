using System;
using System.Collections;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KeepCoreSafe.Managers
{
    [RequireComponent(typeof(WaveManager))]
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Start Setting")]
        [SerializeField] private float startPlacePoint;

        [Header("Combat Setting")]
        [SerializeField, Min(0.1f)] private float maxCombatDuration = 30f;

        [Header("Core Destruction Presentation")]
        [SerializeField, Range(0.01f, 1f)] private float coreDeathTimeScale = 0.15f;
        [SerializeField, Min(0f)] private float coreDeathPresentationDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float coreDeathCameraZoom = 2.4f;
        [SerializeField, Min(0f)] private float coreDeathCameraDuration = 0.28f;

        public static GameManager Instance { get; private set; }

        private WaveManager waveManager;
        private float combatElapsedTime;
        private int timeScaleIndex;
        private Coroutine coreDeathRoutine;
        private bool isCoreDestructionPlaying;

        private static readonly float[] TimeScaleOptions = { 1f, 2f, 4f };

        public static GamePhase Phase { get; private set; } = GamePhase.Preparation;
        public static int WaveIndex { get; private set; }
        public static ObservableValue PlacePoint = new();
        public float CombatElapsedTime => combatElapsedTime;
        public float MaxCombatDuration => maxCombatDuration;
        public float CurrentTimeScale => TimeScaleOptions[timeScaleIndex];

        public static event Action<GamePhase> PhaseChanged;
        public static event Action<float> TimeScaleChanged;
        public static event Action<int> WaveStarted;

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
            waveManager = GetComponent<WaveManager>();
            SetTimeScale(1f);
        }

        private void Start()
        {
            waveManager.WaveCompleted += HandleWaveCompleted;
            GridManager.Instance.CoreDestroyed += HandleCoreDestroyed;

            PlacePoint.Initialize(0f, float.MaxValue);
            PlacePoint.SetValue(startPlacePoint);
        }

        private void Update()
        {
            if (Phase != GamePhase.Combat || isCoreDestructionPlaying)
                return;

            combatElapsedTime += Time.deltaTime;
            if (combatElapsedTime >= maxCombatDuration)
            {
                Debug.Log("시간 초과");
                waveManager.StopWave();
                SetPhase(GamePhase.Preparation);
            }
        }

        public bool TryStartCombat()
        {
            if (GridManager.Instance.Grid.Core == null)
            {
                Debug.LogWarning("Combat cannot start without a Core.");
                return false;
            }

            WaveIndex++;
            SetPhase(GamePhase.Combat);
            WaveStarted?.Invoke(WaveIndex);
            waveManager.StartWave(WaveIndex);
            return true;
        }

        private void HandleWaveCompleted()
        {
            if (Phase == GamePhase.Combat && !isCoreDestructionPlaying)
            {
                SetPhase(GamePhase.Preparation);
            }
        }

        private void HandleCoreDestroyed()
        {
            isCoreDestructionPlaying = false;
            waveManager.StopWave();
            SetPhase(GamePhase.GameOver);
        }

        public bool TryPlayCoreDestruction(CoreBlock core, Action onComplete)
        {
            if (core == null || onComplete == null || isCoreDestructionPlaying)
                return false;

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
            if (isCoreDestructionPlaying)
                return;

            timeScaleIndex = (timeScaleIndex + 1) % TimeScaleOptions.Length;
            SetTimeScale(TimeScaleOptions[timeScaleIndex]);
        }

        private void SetPhase(GamePhase phase)
        {
            combatElapsedTime = 0f;
            Phase = phase;
            if (phase == GamePhase.GameOver)
            {
                timeScaleIndex = 0;
                SetTimeScale(1f);
            }

            PhaseChanged?.Invoke(phase);
            Debug.Log($"Game Phase: {phase}");
        }

        private void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            TimeScaleChanged?.Invoke(scale);
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
            Instance = null;
        }
    }
}
