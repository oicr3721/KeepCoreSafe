using System;
using System.Collections;
using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class StageClearPresentationController : MonoBehaviour
    {
        [Header("Pre-created Presentation Views")]
        [Tooltip("A disabled scene instance reused for every stage-clear charge.")]
        [SerializeField] private CoreEnergyPulseView energyPulse;
        [Tooltip("A disabled scene instance reused for every stage-clear release.")]
        [SerializeField] private ShockwaveRingView shockwave;

        [Header("Anticipation")]
        [SerializeField, Range(0.05f, 1f)] private float slowMotionScale = 0.2f;
        [SerializeField, Min(0f)] private float anticipationDuration = 0.22f;
        [SerializeField, Min(0.1f)] private float cameraZoom = 3.1f;
        [SerializeField, Min(0f)] private float cameraFocusDuration = 0.18f;

        [Header("Energy Pulse")]
        [SerializeField, Range(1, 6)] private int pulseCount = 3;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.3f;
        [SerializeField, Min(0f)] private float pulseMinimumScale = 0.15f;
        [SerializeField, Min(0f)] private float pulseMaximumScale = 1.15f;

        [Header("Shockwave")]
        [SerializeField, Min(0.01f)] private float shockwaveDuration = 2f;
        [SerializeField, Min(0f)] private float shockwaveMapPadding = 2f;
        [SerializeField, Min(0.01f)] private float enemySilhouetteDuration = 0.055f;

        [Header("Finish")]
        [SerializeField, Min(0f)] private float cameraReturnDuration = 0.18f;

        [Header("Audio")]
        [Tooltip("Played when the stage-clear charging presentation begins.")]
        [SerializeField] private AudioCue stageClearSound = new();
        [Tooltip("Played exactly when the circular shockwave is released.")]
        [SerializeField] private AudioCue shockwaveSound = new();

        private Coroutine presentationRoutine;
        private Action completion;

        public bool IsPlaying => presentationRoutine != null;

        private void Awake()
        {
            CleanupEffects();
        }

        public bool Play(CoreBlock core, IReadOnlyCollection<Enemy> enemies, Action onComplete)
        {
            if (core == null || IsPlaying)
                return false;

            completion = onComplete;
            presentationRoutine = StartCoroutine(PlayRoutine(core, CopyAliveEnemies(enemies)));
            return true;
        }

        public void Cancel()
        {
            if (presentationRoutine != null)
                StopCoroutine(presentationRoutine);

            presentationRoutine = null;
            completion = null;
            CleanupEffects();
        }

        private IEnumerator PlayRoutine(CoreBlock core, List<Enemy> enemies)
        {
            GameManager.Instance?.SetPresentationTimeScale(slowMotionScale);
            GameCameraController.Instance?.PlayCinematicFocus(
                core.transform,
                cameraZoom,
                cameraFocusDuration);

            yield return new WaitForSecondsRealtime(anticipationDuration);

            if (energyPulse != null)
            {
                energyPulse.transform.position = core.transform.position;
                energyPulse.gameObject.SetActive(true);
                energyPulse.Play(pulseDuration, pulseCount, pulseMinimumScale, pulseMaximumScale);
            }
            yield return new WaitForSecondsRealtime(pulseDuration);
            if (energyPulse != null)
                energyPulse.gameObject.SetActive(false);

            float radius = CalculateShockwaveRadius(core.transform.position, enemies);
            AudioManager.PlayAt(shockwaveSound, core.transform.position);
            if (shockwave != null)
            {
                shockwave.transform.position = core.transform.position;
                shockwave.gameObject.SetActive(true);
                GameManager.Instance?.ResetCoreEnergy();
                shockwave.Play(shockwaveDuration, radius * 2f);
            }

            foreach (Enemy enemy in enemies)
            {
                if (enemy == null || enemy.IsDead)
                    continue;

                float distance = Vector2.Distance(core.transform.position, enemy.transform.position);
                float delay = shockwaveDuration * Mathf.Clamp01(distance / Mathf.Max(0.01f, radius));
                StartCoroutine(EliminateWhenReached(enemy, delay));
            }

            yield return new WaitForSecondsRealtime(shockwaveDuration + enemySilhouetteDuration);

            GameManager.Instance?.RestoreNormalTimeScale();
            GameCameraController.Instance?.ReturnToDefaultView(cameraReturnDuration);
            yield return new WaitForSecondsRealtime(cameraReturnDuration);

            AudioManager.PlayAt(stageClearSound, core.transform.position);

            CleanupEffects();
            presentationRoutine = null;
            Action callback = completion;
            completion = null;
            callback?.Invoke();
        }

        private IEnumerator EliminateWhenReached(Enemy enemy, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            if (enemy != null && !enemy.IsDead)
                enemy.EliminateByShockwave(enemySilhouetteDuration);
        }

        private float CalculateShockwaveRadius(Vector3 center, IReadOnlyList<Enemy> enemies)
        {
            float radius = shockwaveMapPadding;
            if (GridManager.Instance != null)
            {
                float width = GridManager.Instance.Width * GridManager.Instance.CellSize;
                float height = GridManager.Instance.Height * GridManager.Instance.CellSize;
                radius = Mathf.Max(radius, Mathf.Sqrt(width * width + height * height) * 0.55f);
            }

            foreach (Enemy enemy in enemies)
            {
                if (enemy != null)
                    radius = Mathf.Max(radius, Vector2.Distance(center, enemy.transform.position));
            }

            return radius + shockwaveMapPadding;
        }

        private static List<Enemy> CopyAliveEnemies(IReadOnlyCollection<Enemy> enemies)
        {
            List<Enemy> result = new();
            if (enemies == null)
                return result;

            foreach (Enemy enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                    result.Add(enemy);
            }

            return result;
        }

        private void CleanupEffects()
        {
            if (energyPulse != null)
                energyPulse.gameObject.SetActive(false);
            if (shockwave != null)
                shockwave.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}
