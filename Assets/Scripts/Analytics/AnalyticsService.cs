using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Analytics
{
    public static class AnalyticsService
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyFields =
            new Dictionary<string, object>();

        private static IAnalyticsBackend backend;
        private static bool lifecycleInstalled;
        private static bool consentListenerInstalled;
        private static bool tutorialActive;
        private static bool tutorialCompleted;
        private static bool prologueActive;
        private static bool prologueCompleted;
        private static bool gameActive;
        private static bool gameOverSent;
        private static int activeWave;
        private static bool activeWaveCompleted;
        private static int currentPreparationRerolls;
        private static readonly HashSet<string> completedTutorialSteps = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeRuntime()
        {
            ResetState();
            // EditMode tests can inject a recorder. A real Player/Play Mode run must always
            // restore the production transport before any scene starts.
            backend = new GameAnalyticsBackend();
            if (!consentListenerInstalled)
            {
                AnalyticsConsentSettings.ConsentChanged += HandleConsentChanged;
                consentListenerInstalled = true;
            }
            if (!lifecycleInstalled)
            {
                Application.quitting += HandleApplicationQuitting;
                lifecycleInstalled = true;
            }
            backend.SetEnabled(AnalyticsConsentSettings.IsGranted);
        }

        public static void TutorialStarted()
        {
            if (tutorialActive && !tutorialCompleted)
                return;
            tutorialActive = true;
            tutorialCompleted = false;
            Design(AnalyticsEventIds.TutorialStarted);
        }

        public static void TutorialStepCompleted(string stepId)
        {
            if (!tutorialActive
                || tutorialCompleted
                || string.IsNullOrWhiteSpace(stepId)
                || !completedTutorialSteps.Add(stepId))
                return;
            Progression(AnalyticsProgressionStatus.Complete, AnalyticsProgressions.Tutorial, stepId);
        }

        public static void TutorialExceptionOccurred(string stepId, string exceptionType)
        {
            if (!tutorialActive || tutorialCompleted)
                return;
            Design(AnalyticsEventIds.TutorialException, null, new Dictionary<string, object>
            {
                [AnalyticsFields.StepId] = stepId,
                [AnalyticsFields.ExceptionType] = exceptionType
            });
        }

        public static void TutorialCompleted()
        {
            if (!tutorialActive || tutorialCompleted)
                return;
            tutorialCompleted = true;
            Design(AnalyticsEventIds.TutorialCompleted);
        }

        public static void PrologueStarted()
        {
            if (prologueActive && !prologueCompleted)
                return;
            prologueActive = true;
            prologueCompleted = false;
            Design(AnalyticsEventIds.PrologueStarted);
        }

        public static void PrologueCompleted()
        {
            if (!prologueActive || prologueCompleted)
                return;
            prologueCompleted = true;
            Design(AnalyticsEventIds.PrologueCompleted);
        }

        public static void GameStarted()
        {
            if (gameActive)
                return;
            gameActive = true;
            gameOverSent = false;
            activeWave = 0;
            activeWaveCompleted = false;
            currentPreparationRerolls = 0;
            Design(AnalyticsEventIds.GameStarted);
        }

        public static void WaveStarted(GameManager.GameplayState state)
        {
            if (!gameActive || state.WaveIndex <= 0 || activeWave == state.WaveIndex)
                return;
            activeWave = state.WaveIndex;
            activeWaveCompleted = false;
            Progression(
                AnalyticsProgressionStatus.Start,
                AnalyticsProgressions.Wave,
                WaveId(state.WaveIndex),
                WaveType(state),
                WaveFields(state, false));
        }

        public static void WaveCompleted(GameManager.GameplayState state, string clearType)
        {
            if (!gameActive || activeWave != state.WaveIndex || activeWaveCompleted)
                return;
            activeWaveCompleted = true;
            Dictionary<string, object> fields = WaveFields(state, true);
            fields[AnalyticsFields.ClearType] = clearType;
            Progression(
                AnalyticsProgressionStatus.Complete,
                AnalyticsProgressions.Wave,
                WaveId(state.WaveIndex),
                WaveType(state),
                fields);
            currentPreparationRerolls = 0;
        }

        public static void GameOver(GameManager.GameplayState state, string gameOverType)
        {
            if (!gameActive || gameOverSent)
                return;
            gameOverSent = true;
            Dictionary<string, object> fields = WaveFields(state, true);
            fields[AnalyticsFields.GameOverType] = gameOverType;
            Design(AnalyticsEventIds.GameOver, state.WaveIndex, fields);
            if (activeWave == state.WaveIndex && !activeWaveCompleted)
            {
                activeWaveCompleted = true;
                Progression(
                    AnalyticsProgressionStatus.Fail,
                    AnalyticsProgressions.Wave,
                    WaveId(state.WaveIndex),
                    WaveType(state),
                    fields);
            }
            gameActive = false;
        }

        public static void RerollUsed(int waveNumber, int rerollCount, int paidCost)
        {
            if (!gameActive)
                return;
            currentPreparationRerolls = Mathf.Max(currentPreparationRerolls, rerollCount);
            Design(AnalyticsEventIds.RerollUsed, paidCost, new Dictionary<string, object>
            {
                [AnalyticsFields.WaveNumber] = waveNumber,
                [AnalyticsFields.RerollCount] = rerollCount,
                [AnalyticsFields.RerollCost] = paidCost
            });
        }

        public static void GameAbandoned()
        {
            if (!gameActive || gameOverSent)
                return;

            GameManager.GameplayState? state = GameManager.Instance?.CaptureGameplayState();
            Dictionary<string, object> fields = state.HasValue
                ? WaveFields(state.Value, true)
                : new Dictionary<string, object>();
            if (state.HasValue)
                fields[AnalyticsFields.Phase] = state.Value.Phase.ToString().ToLowerInvariant();
            Design(AnalyticsEventIds.GameAbandoned, state?.WaveIndex, fields);
            gameActive = false;
        }

        public static void OfferSelected(ShopOfferData offer, int waveNumber)
        {
            if (!gameActive || offer == null)
                return;
            Design(AnalyticsEventIds.OfferSelected, null, new Dictionary<string, object>
            {
                [AnalyticsFields.WaveNumber] = waveNumber,
                [AnalyticsFields.OfferId] = offer.AnalyticsId
            });
        }

        public static void MergePerformed(BlockData result, int sourceBlockCount)
        {
            if (!gameActive || result == null)
                return;
            Design(AnalyticsEventIds.MergePerformed, sourceBlockCount, new Dictionary<string, object>
            {
                [AnalyticsFields.WaveNumber] = GameManager.WaveIndex + 1,
                [AnalyticsFields.BlockId] = result.AnalyticsId,
                [AnalyticsFields.SourceBlockCount] = sourceBlockCount
            });
        }

        public static void SetBackendForTests(IAnalyticsBackend replacement)
        {
            backend = replacement;
            ResetState();
        }

        public static void ResetForTests()
        {
            ResetState();
        }

        private static void HandleConsentChanged(AnalyticsConsentDecision decision)
        {
            EnsureBackend();
            backend.SetEnabled(decision == AnalyticsConsentDecision.Granted);
        }

        private static void HandleApplicationQuitting()
        {
            if (!gameActive || gameOverSent)
                return;
            GameManager.GameplayState? state = GameManager.Instance?.CaptureGameplayState();
            Dictionary<string, object> fields = state.HasValue
                ? WaveFields(state.Value, true)
                : new Dictionary<string, object>();
            if (state.HasValue)
                fields[AnalyticsFields.Phase] = state.Value.Phase.ToString().ToLowerInvariant();
            Design(AnalyticsEventIds.GracefulExit, state?.WaveIndex, fields);
            gameActive = false;
        }

        private static Dictionary<string, object> WaveFields(
            GameManager.GameplayState state,
            bool includeBoard)
        {
            Dictionary<string, object> fields = new()
            {
                [AnalyticsFields.WaveNumber] = state.WaveIndex,
                [AnalyticsFields.WaveId] = state.WaveData != null ? state.WaveData.name : "unknown",
                [AnalyticsFields.WaveType] = WaveType(state),
                [AnalyticsFields.EnemyCount] = state.ActiveEnemyCount,
                [AnalyticsFields.PlannedEnemyCount] = state.PlannedEnemyCount,
                [AnalyticsFields.RequiredEnergy] = state.RequiredEnergy,
                [AnalyticsFields.CurrentEnergy] = state.CurrentEnergy,
                [AnalyticsFields.RerollCount] = currentPreparationRerolls,
                [AnalyticsFields.CoreHealthRatio] = state.CoreMaximumHealth > 0f
                    ? Math.Round(Mathf.Clamp01(state.CoreHealth / state.CoreMaximumHealth), 3)
                    : 0d
            };
            if (includeBoard)
                AddBoardSummary(fields);
            return fields;
        }

        private static void AddBoardSummary(Dictionary<string, object> fields)
        {
            int total = 0;
            int basic = 0;
            int skill = 0;
            if (GridManager.Instance != null)
            {
                foreach (Block block in GridManager.Instance.GetBlocks())
                {
                    if (block == null || block is CoreBlock || block is SupplyBlock)
                        continue;
                    total++;
                    if (block.Data is BasicBlockData)
                        basic++;
                    else
                        skill++;
                }
            }
            fields[AnalyticsFields.BoardBlockCount] = total;
            fields[AnalyticsFields.BoardBasicCount] = basic;
            fields[AnalyticsFields.BoardSkillCount] = skill;
        }

        private static void Design(
            string eventId,
            float? value = null,
            IReadOnlyDictionary<string, object> fields = null)
        {
            EnsureBackend();
            backend.SendDesign(eventId, value, fields ?? EmptyFields);
        }

        private static void Progression(
            AnalyticsProgressionStatus status,
            string progression01,
            string progression02,
            string progression03 = null,
            IReadOnlyDictionary<string, object> fields = null)
        {
            EnsureBackend();
            backend.SendProgression(
                status, progression01, progression02, progression03, fields ?? EmptyFields);
        }

        private static string WaveId(int waveNumber) => $"wave_{waveNumber:0000}";
        private static string WaveType(GameManager.GameplayState state) =>
            state.IsSpecialWave ? "special" : "normal";

        private static void EnsureBackend()
        {
            backend ??= new GameAnalyticsBackend();
        }

        private static void ResetState()
        {
            tutorialActive = false;
            tutorialCompleted = false;
            prologueActive = false;
            prologueCompleted = false;
            gameActive = false;
            gameOverSent = false;
            activeWave = 0;
            activeWaveCompleted = false;
            currentPreparationRerolls = 0;
            completedTutorialSteps.Clear();
        }
    }
}
