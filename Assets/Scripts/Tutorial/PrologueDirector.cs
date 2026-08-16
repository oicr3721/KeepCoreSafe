using System.Collections;
using DG.Tweening;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Tutorial
{
    [DefaultExecutionOrder(100)]
    public sealed class PrologueDirector : MonoBehaviour
    {
        private static readonly int IdleTrigger = Animator.StringToHash("Idle");
        private static readonly int ComaTrigger = Animator.StringToHash("Coma");

        [Header("World")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Transform coreTransform;
        [SerializeField] private SpriteRenderer coreRenderer;
        [SerializeField] private CoreBlockData tutorialCoreData;
        [SerializeField] private CoreBlockData completedCoreData;
        [SerializeField] private Transform lilyTransform;
        [SerializeField] private SpriteRenderer lilyRenderer;
        [SerializeField] private Animator lilyAnimator;
        [SerializeField] private SpriteRenderer placementPreview;
        [SerializeField] private TutorialGridHighlight gridHighlight;
        [SerializeField] private GameCameraController cameraController;

        [Header("Camera")]
        [SerializeField] private Vector2 cameraOffset;

        [Header("UI")]
        [SerializeField] private CanvasGroup objectiveGroup;
        [SerializeField] private PrologueThreatOverlay threatOverlay;
        [SerializeField] private Image atmosphereOverlay;

        [Header("Reusable VFX")]
        [SerializeField] private CoreEnergyPulseView energyPulsePrefab;
        [SerializeField] private ShockwaveRingView shockwavePrefab;
        [SerializeField] private ParticleSystem burstParticlesPrefab;
        [SerializeField] private Transform effectRoot;
        [SerializeField] private AudioClip fusionSound;

        [Header("Interaction Audio")]
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip placementSound;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float sceneRecognitionDelay = 1.1f;
        [SerializeField, Min(0f)] private float objectiveFadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float cameraFocusDuration = 0.45f;
        [SerializeField, Min(0f)] private float lilyLiftDuration = 0.7f;
        [SerializeField, Min(0f)] private float fusionHoldDuration = 0.3f;
        [SerializeField, Min(0f)] private float threatRejectionDuration = 0.42f;
        [SerializeField, Min(0f)] private float postThreatSilence = 0.08f;

        private Vector2Int coreCell;
        private Vector2Int lilyCell;
        private bool inputEnabled;
        private bool lilyPlaced;
        private bool lilySelected;
        private bool fusionStarted;
        private Tween atmosphereTween;

        private void Start()
        {
            if (!ValidateReferences())
                return;

            coreCell = new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);
            lilyCell = coreCell + Vector2Int.down;
            coreTransform.position = gridManager.GridToWorld(coreCell);
            lilyTransform.position = gridManager.GridToWorld(lilyCell);
            cameraController.SetDefaultViewCenter(
                coreTransform.position + (Vector3)cameraOffset);
            coreRenderer.sprite = tutorialCoreData.Sprite;
            lilyAnimator.ResetTrigger(IdleTrigger);
            lilyAnimator.SetTrigger(ComaTrigger);
            placementPreview.gameObject.SetActive(false);
            objectiveGroup.alpha = 0f;
            lilyPlaced = true;
            StartAtmosphereFlicker();
            StartCoroutine(BeginInteractivePrologue());
        }

        private IEnumerator BeginInteractivePrologue()
        {
            yield return new WaitForSecondsRealtime(sceneRecognitionDelay);
            gridHighlight.Show(lilyCell);
            threatOverlay.Begin();
            yield return objectiveGroup.DOFade(1f, objectiveFadeDuration).SetUpdate(true)
                .WaitForCompletion();
            inputEnabled = true;
        }

        private void Update()
        {
            if (!inputEnabled || fusionStarted || Mouse.current == null || Camera.main == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (lilySelected)
                    placementPreview.gameObject.SetActive(false);
                return;
            }

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = 0f;
            Vector2Int hoveredCell = gridManager.WorldToGrid(mouseWorld);
            bool insideGrid = gridManager.Grid.IsWithinBounds(hoveredCell);

            if (lilySelected)
            {
                placementPreview.gameObject.SetActive(insideGrid);
                if (insideGrid)
                    placementPreview.transform.position = gridManager.GridToWorld(hoveredCell);
                if (insideGrid && Mouse.current.leftButton.wasPressedThisFrame)
                    PlaceLily(hoveredCell);
                return;
            }

            if (lilyPlaced
                && insideGrid
                && hoveredCell == lilyCell
                && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SelectPlacedLily();
            }
        }

        private void SelectPlacedLily()
        {
            if (!inputEnabled || fusionStarted || !lilyPlaced || lilySelected)
                return;

            lilyPlaced = false;
            lilySelected = true;
            lilyRenderer.enabled = false;
            placementPreview.sprite = lilyRenderer.sprite;
            placementPreview.color = new Color(1f, 1f, 1f, 0.62f);
            placementPreview.gameObject.SetActive(false);
            gridHighlight.Show(coreCell);
            AudioManager.Play(pickupSound);
        }

        private void PlaceLily(Vector2Int cell)
        {
            lilyCell = cell;
            lilyTransform.position = gridManager.GridToWorld(cell);
            lilySelected = false;
            lilyPlaced = true;
            lilyRenderer.enabled = true;
            placementPreview.gameObject.SetActive(false);
            AudioManager.Play(placementSound);

            if (cell == coreCell)
            {
                fusionStarted = true;
                inputEnabled = false;
                gridHighlight.Hide();
                StartCoroutine(PlayFusion());
            }
            else
            {
                gridHighlight.Show(lilyCell);
            }
        }

        private IEnumerator PlayFusion()
        {
            objectiveGroup.DOFade(0f, objectiveFadeDuration).SetUpdate(true);

            cameraController.ReturnToDefaultView(cameraFocusDuration);
            AudioManager.Play(fusionSound);

            Vector3 corePosition = coreTransform.position;
            Vector3 liftedPosition = corePosition + Vector3.up * 0.55f;
            SpawnPulse(corePosition, Mathf.Max(0.1f, lilyLiftDuration), 3, 0.1f, 1.45f, 1.3f);
            yield return DOTween.Sequence()
                .SetUpdate(true)
                .Append(lilyTransform.DOMove(liftedPosition, lilyLiftDuration).SetEase(Ease.OutCubic))
                .Join(lilyTransform.DOScale(1.18f, lilyLiftDuration).SetEase(Ease.OutBack))
                .Join(coreTransform.DOScale(1.08f, lilyLiftDuration).SetEase(Ease.InOutSine))
                .WaitForCompletion();

            SpawnPulse(liftedPosition, fusionHoldDuration + 0.2f, 2, 0.08f, 1.1f, 1.65f);
            yield return new WaitForSecondsRealtime(fusionHoldDuration);

            coreRenderer.sprite = completedCoreData.Sprite;
            PlayBurst(corePosition);
            yield return lilyRenderer.DOFade(0f, 0.18f).SetUpdate(true).WaitForCompletion();
            lilyRenderer.enabled = false;
            coreTransform.localScale = Vector3.one;

            yield return threatOverlay.RejectAndClear(threatRejectionDuration);
            yield return new WaitForSecondsRealtime(postThreatSilence);
            atmosphereTween?.Kill(false);

            if (SceneTransition.Instance != null)
                SceneLoader.Load(SceneType.Game);
            else
                SceneManager.LoadScene("GameScene");
        }

        private void PlayBurst(Vector3 position)
        {
            SpawnPulse(position, 0.28f, 1, 0.05f, 2.2f, 2f);
            if (shockwavePrefab != null)
            {
                ShockwaveRingView ring = Instantiate(shockwavePrefab, position, Quaternion.identity, effectRoot);
                ring.Play(0.32f, 2.6f);
            }
            if (burstParticlesPrefab != null)
            {
                ParticleSystem particles = Instantiate(burstParticlesPrefab, position, Quaternion.identity, effectRoot);
                particles.Emit(24);
            }
            cameraController?.PlayImpactShake(0.14f, 0.18f);
        }

        private void SpawnPulse(
            Vector3 position,
            float duration,
            int pulses,
            float minimumScale,
            float maximumScale,
            float intensity)
        {
            if (energyPulsePrefab == null)
                return;

            CoreEnergyPulseView pulse = Instantiate(
                energyPulsePrefab,
                position,
                Quaternion.identity,
                effectRoot);
            pulse.Play(duration, pulses, minimumScale, maximumScale, intensity);
        }

        private void StartAtmosphereFlicker()
        {
            if (atmosphereOverlay == null)
                return;

            Color color = atmosphereOverlay.color;
            color.a = 0.16f;
            atmosphereOverlay.color = color;
            atmosphereTween = atmosphereOverlay.DOFade(0.55f, 1.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private bool ValidateReferences()
        {
            bool valid = gridManager != null
                         && coreTransform != null
                         && coreRenderer != null
                         && tutorialCoreData != null
                         && completedCoreData != null
                         && lilyTransform != null
                         && lilyRenderer != null
                         && lilyAnimator != null
                         && placementPreview != null
                         && gridHighlight != null
                         && cameraController != null
                         && objectiveGroup != null
                         && threatOverlay != null;
            if (!valid)
                Debug.LogError("PrologueDirector has incomplete scene references.", this);
            return valid;
        }

        private void OnDestroy()
        {
            atmosphereTween?.Kill(false);
            transform.DOKill(false);
            if (lilyTransform != null)
                lilyTransform.DOKill(false);
            if (coreTransform != null)
                coreTransform.DOKill(false);
            objectiveGroup?.DOKill(false);
        }
    }
}
