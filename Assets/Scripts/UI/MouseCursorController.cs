using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public interface IMouseCursorInteractionSource
    {
        bool IsPointerInteractionAvailable(Vector2 screenPosition);
    }

    [DefaultExecutionOrder(10000)]
    public sealed class MouseCursorController : MonoBehaviour
    {
        public static MouseCursorController instance { get; private set; }

        private enum CursorState
        {
            Unset,
            Default,
            Clickable
        }

        private enum UiPointerState
        {
            None,
            Blocking,
            Clickable
        }

        [Header("Cursor")]
        [SerializeField]
        private Image cursorImage;

        [SerializeField]
        private Sprite defaultCursorSprite;

        [SerializeField]
        private Sprite clickableCursorSprite;

        private readonly List<RaycastResult> uiRaycastResults = new(16);
        private readonly List<IMouseCursorInteractionSource> worldSources = new(4);

        private Canvas cursorCanvas;
        private RectTransform cursorRectTransform;

        private EventSystem pointerEventSystem;
        private PointerEventData pointerEventData;

        private CursorState currentState = CursorState.Unset;
        private bool hasApplicationFocus;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RebuildWorldSources();
        }
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (cursorImage == null)
            {
                Debug.LogError(
                    "MouseCursorController requires a Cursor Image reference.",
                    this);

                enabled = false;
                return;
            }

            cursorCanvas = cursorImage.canvas;
            cursorRectTransform = cursorImage.rectTransform;

            if (cursorCanvas == null)
            {
                Debug.LogError(
                    "MouseCursorController's Cursor Image must be placed under a Canvas.",
                    this);

                enabled = false;
                return;
            }

            cursorImage.raycastTarget = false;

            Cursor.lockState = CursorLockMode.None;

            hasApplicationFocus = Application.isFocused;

            SetCustomCursorVisible(false);
            Cursor.visible = true;

            ApplyState(CursorState.Default, true);

            RebuildWorldSources();
        }

        private void Update()
        {
            if (Mouse.current == null)
            {
                SetCustomCursorVisible(false);
                Cursor.visible = true;
                return;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();

            bool pointerInsideWindow =
                IsPointerInsideGameWindow(screenPosition);

            // The application is not focused.
            // Let the OS cursor remain visible.
            if (!hasApplicationFocus)
            {
                Cursor.visible = true;
                SetCustomCursorVisible(false);
                return;
            }

            // The mouse is outside the game window.
            // Show the OS cursor and hide the custom cursor.
            if (!pointerInsideWindow)
            {
                Cursor.visible = true;
                SetCustomCursorVisible(false);
                return;
            }

            // The mouse is inside the focused game window.
            // Hide the OS cursor and show the custom cursor.
            Cursor.visible = false;
            SetCustomCursorVisible(true);

            UpdateCursorPosition(screenPosition);

            UiPointerState uiState = EvaluateUi(screenPosition);

            if (uiState != UiPointerState.None)
            {
                ApplyState(
                    uiState == UiPointerState.Clickable
                        ? CursorState.Clickable
                        : CursorState.Default);

                return;
            }

            bool worldClickable = false;

            for (int i = worldSources.Count - 1; i >= 0; i--)
            {
                IMouseCursorInteractionSource source = worldSources[i];

                if (source is not Behaviour behaviour || behaviour == null)
                {
                    worldSources.RemoveAt(i);
                    continue;
                }

                if (!behaviour.isActiveAndEnabled)
                    continue;

                if (source.IsPointerInteractionAvailable(screenPosition))
                {
                    worldClickable = true;
                    break;
                }
            }

            ApplyState(
                worldClickable
                    ? CursorState.Clickable
                    : CursorState.Default);
        }

        private bool IsPointerInsideGameWindow(Vector2 screenPosition)
        {
            return screenPosition.x >= 0f
                   && screenPosition.x <= Screen.width
                   && screenPosition.y >= 0f
                   && screenPosition.y <= Screen.height;
        }

        private void UpdateCursorPosition(Vector2 screenPosition)
        {
            if (cursorCanvas == null || cursorRectTransform == null)
                return;

            Camera eventCamera = null;

            if (cursorCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = cursorCanvas.worldCamera;

            RectTransform canvasRectTransform =
                cursorCanvas.transform as RectTransform;

            if (canvasRectTransform == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
            {
                cursorRectTransform.localPosition = localPosition;
            }
        }

        private UiPointerState EvaluateUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
                return UiPointerState.None;

            if (pointerEventSystem != eventSystem || pointerEventData == null)
            {
                pointerEventSystem = eventSystem;
                pointerEventData = new PointerEventData(eventSystem);
            }

            pointerEventData.Reset();
            pointerEventData.position = screenPosition;

            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                RaycastResult result = uiRaycastResults[i];

                if (result.module is not GraphicRaycaster)
                    continue;

                if (cursorImage != null
                    && result.gameObject == cursorImage.gameObject)
                {
                    continue;
                }

                return IsClickableUi(result.gameObject)
                    ? UiPointerState.Clickable
                    : UiPointerState.Blocking;
            }

            return UiPointerState.None;
        }

        private static bool IsClickableUi(GameObject hitObject)
        {
            if (hitObject == null)
                return false;

            Selectable selectable =
                hitObject.GetComponentInParent<Selectable>();

            if (selectable != null)
            {
                return selectable.IsActive()
                       && selectable.IsInteractable();
            }

            return HasActiveHandler<IPointerClickHandler>(hitObject)
                   || HasActiveHandler<IPointerDownHandler>(hitObject);
        }

        private static bool HasActiveHandler<THandler>(GameObject hitObject)
            where THandler : IEventSystemHandler
        {
            GameObject handlerObject =
                ExecuteEvents.GetEventHandler<THandler>(hitObject);

            if (handlerObject == null)
                return false;

            MonoBehaviour[] behaviours =
                handlerObject.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is THandler
                    && behaviour.isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildWorldSources()
        {
            worldSources.Clear();

            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IMouseCursorInteractionSource source)
                    worldSources.Add(source);
            }
        }

        private void ApplyState(CursorState state, bool force = false)
        {
            if (!force && currentState == state)
                return;

            currentState = state;

            if (cursorImage == null)
                return;

            Sprite sprite = state == CursorState.Clickable
                ? clickableCursorSprite
                : defaultCursorSprite;

            if (sprite != null)
                cursorImage.sprite = sprite;
        }

        private void SetCustomCursorVisible(bool visible)
        {
            if (cursorImage != null)
                cursorImage.enabled = visible;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            hasApplicationFocus = hasFocus;

            if (hasFocus)
            {
                Cursor.lockState = CursorLockMode.None;

                // Actual OS cursor visibility is resolved in Update()
                // based on whether the pointer is inside the game window.
            }
            else
            {
                Cursor.visible = true;
                SetCustomCursorVisible(false);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}