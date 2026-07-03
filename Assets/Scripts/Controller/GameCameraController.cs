using DG.Tweening;
using KeepCoreSafe.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KeepCoreSafe.Controllers
{
    [RequireComponent(typeof(Camera))]
    public sealed class GameCameraController : MonoBehaviour
    {
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

        private Camera worldCamera;
        private Vector3 targetPosition;
        private Vector3 panVelocity;
        private Vector3 defaultFocusPosition;
        private float targetZoom;
        private float zoomVelocity;
        private float defaultZoom;
        private bool isReturning;

        private void Awake()
        {
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

        private void HandleInput()
        {
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
            transform.DOMove(targetPosition, returnDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => isReturning = false);
            worldCamera.DOOrthoSize(targetZoom, returnDuration).SetEase(Ease.OutCubic);
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

        private void OnDestroy()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            transform.DOKill();
            if (worldCamera != null) worldCamera.DOKill();
        }
    }
}
