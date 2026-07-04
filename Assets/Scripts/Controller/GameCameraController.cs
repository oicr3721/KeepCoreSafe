using DG.Tweening;
using KeepCoreSafe.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KeepCoreSafe.Controllers
{
    [RequireComponent(typeof(Camera))]
    public sealed class GameCameraController : MonoBehaviour
    {
        public static GameCameraController Instance { get; private set; }

        [Header("Pan")]
        [SerializeField, Min(0.01f)] private float panSensitivity = 1f;
        [SerializeField, Min(0.01f)] private float panSmoothTime = 0.08f;

        [Header("Zoom")]
        [SerializeField, Min(0.1f)] private float minimumZoom = 3f;
        [SerializeField, Min(0.1f)] private float maximumZoom = 10f;
        [SerializeField, Min(0.01f)] private float zoomSensitivity = 1f;
        [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.1f;

        [Header("Return")]
        [SerializeField, Min(0.01f)] private float returnDuration = 0.45f;

        [Header("Impact Shake")]
        [SerializeField, Min(0.01f)] private float impactShakeDuration = 0.18f;
        [SerializeField, Min(0f)] private float impactShakeStrength = 0.12f;
        [SerializeField, Min(1f)] private float impactShakeFrequency = 32f;

        private Camera worldCamera;
        private Vector3 targetPosition;
        private Vector3 panVelocity;
        private Vector3 defaultFocusPosition;
        private float targetZoom;
        private float zoomVelocity;
        private float defaultZoom;
        private bool isReturning;
        private bool isCinematicFocus;
        private float shakeRemaining;
        private float shakeSeed;
        private Vector3 appliedShakeOffset;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            worldCamera = GetComponent<Camera>();
            defaultZoom = worldCamera.orthographicSize;
            targetZoom = defaultZoom;
            targetPosition = transform.position;
        }

        private void Start()
        {
            defaultFocusPosition = GetCoreOrGridCenter();
            targetPosition = WithCameraDepth(defaultFocusPosition);
            transform.position = targetPosition;
            GameManager.PhaseChanged += HandlePhaseChanged;
        }

        private void Update()
        {
            RemoveAppliedShakeOffset();
            HandleInput();
            if (isReturning)
                return;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref panVelocity,
                panSmoothTime);
            worldCamera.orthographicSize = Mathf.SmoothDamp(
                worldCamera.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime);
        }

        private void LateUpdate()
        {
            if (shakeRemaining <= 0f || impactShakeStrength <= 0f)
                return;

            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.deltaTime);
            float envelope = shakeRemaining / impactShakeDuration;
            float phase = (Time.time + shakeSeed) * impactShakeFrequency;
            Vector2 direction = new Vector2(
                Mathf.Sin(phase * 1.17f),
                Mathf.Cos(phase * 0.93f));
            appliedShakeOffset = (Vector3)(direction.normalized * (impactShakeStrength * envelope));
            transform.position += appliedShakeOffset;
        }

        public void PlayImpactShake()
        {
            RemoveAppliedShakeOffset();
            shakeRemaining = impactShakeDuration;
            shakeSeed = Random.value * 10f;
        }

        public void PlayCoreDeathFocus(Transform target, float zoom, float duration)
        {
            PlayCinematicFocus(target, zoom, duration);
        }

        public void PlayCinematicFocus(Transform target, float zoom, float duration)
        {
            if (target == null)
                return;

            RemoveAppliedShakeOffset();
            shakeRemaining = 0f;
            transform.DOKill();
            worldCamera.DOKill();

            isReturning = false;
            isCinematicFocus = true;
            targetPosition = WithCameraDepth(target.position);
            targetZoom = Mathf.Max(0.1f, zoom);
            panVelocity = Vector3.zero;
            zoomVelocity = 0f;

            transform.DOMove(targetPosition, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
            worldCamera.DOOrthoSize(targetZoom, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void HandleInput()
        {
            if (isCinematicFocus)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 dragDelta = mouse.delta.ReadValue();
            if (mouse.middleButton.isPressed && dragDelta.sqrMagnitude > 0f)
            {
                CancelReturn();
                float worldUnitsPerPixel = worldCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                targetPosition -= new Vector3(dragDelta.x, dragDelta.y, 0f)
                    * (worldUnitsPerPixel * panSensitivity);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                CancelReturn();
                targetZoom = Mathf.Clamp(
                    targetZoom - scroll * zoomSensitivity * 0.01f,
                    minimumZoom,
                    maximumZoom);
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Preparation || phase == GamePhase.GameOver)
                ReturnToDefault();
        }

        private void ReturnToDefault()
        {
            ReturnToDefaultView(returnDuration);
        }

        public void ReturnToDefaultView(float duration)
        {
            isCinematicFocus = false;
            RemoveAppliedShakeOffset();
            shakeRemaining = 0f;
            Vector3 currentFocus = GetCoreOrGridCenter();
            if (GridManager.Instance?.Grid?.Core != null)
                defaultFocusPosition = currentFocus;

            targetPosition = WithCameraDepth(defaultFocusPosition);
            targetZoom = Mathf.Clamp(defaultZoom, minimumZoom, maximumZoom);
            panVelocity = Vector3.zero;
            zoomVelocity = 0f;

            transform.DOKill();
            worldCamera.DOKill();
            isReturning = true;
            float safeDuration = Mathf.Max(0f, duration);
            transform.DOMove(targetPosition, safeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => isReturning = false);
            worldCamera.DOOrthoSize(targetZoom, safeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void CancelReturn()
        {
            if (!isReturning)
                return;

            transform.DOKill();
            worldCamera.DOKill();
            isReturning = false;
            targetPosition = transform.position;
            targetZoom = worldCamera.orthographicSize;
        }

        private Vector3 GetCoreOrGridCenter()
        {
            if (GridManager.Instance?.Grid?.Core != null)
                return GridManager.Instance.Grid.Core.transform.position;
            if (GridManager.Instance != null)
                return GridManager.Instance.GridCenter;
            return transform.position;
        }

        private Vector3 WithCameraDepth(Vector3 position)
        {
            position.z = transform.position.z;
            return position;
        }

        private void RemoveAppliedShakeOffset()
        {
            if (appliedShakeOffset == Vector3.zero)
                return;

            transform.position -= appliedShakeOffset;
            appliedShakeOffset = Vector3.zero;
        }

        private void OnDestroy()
        {
            RemoveAppliedShakeOffset();
            if (Instance == this)
                Instance = null;
            GameManager.PhaseChanged -= HandlePhaseChanged;
            transform.DOKill();
            if (worldCamera != null) worldCamera.DOKill();
        }
    }
}
