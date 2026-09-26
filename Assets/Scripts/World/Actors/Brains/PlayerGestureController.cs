using System;
using System.Collections.Generic;
using Cameras;
using Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using World;
using World.Actors.Player;
using World.Anchors;
using World.Interactables;

namespace World.Actors.Brains{
    /// Processes pointer inputs, hover, scrolling, hotkeys, and gestures into game events.
    [DisallowMultipleComponent]
    public class PlayerGestureController : MonoBehaviour{
        private static PlayerGestureController _instance;

        [SerializeField] private InputActionReference commandActionRef, primarySelectActionRef, pointActionRef, scrollActionRef, modifierAppendActionRef, modifierAltActionRef;
        [SerializeField] private InputActionReference selectAllActionRef, cycleLeaderActionRef, deselectActionRef, stopActionRef, slot1ActionRef, slot2ActionRef, slot3ActionRef, slot4ActionRef;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask characterLayer, groundLayer = ~0;
        [SerializeField] private float dragThreshold = 10f, holdThreshold = 0.35f, continuousRepathInterval = 0.08f;
        [SerializeField] private Color boxBorderColor = new(0.2f, 0.8f, 0.2f, 0.9f), boxFillColor = new(0.2f, 0.8f, 0.2f, 0.2f);

        private readonly List<RaycastResult> _uiRaycastResults = new();
        private readonly List<BoundAction>   _boundActions     = new();
        private Vector2 _pressStartPos, _currentScreenPos;
        private float   _pressStartTime, _lastContinuousCommandTime;
        private bool    _isPressed, _isDragging, _holdFired, _isCommandHeld, _controlsEnabled = true;

        public static PlayerGestureController Instance => _instance;
        public static bool IsAppendPressed => _instance.modifierAppendActionRef.action.IsPressed();
        public static bool IsAltPressed    => _instance.modifierAltActionRef && _instance.modifierAltActionRef.action.IsPressed();

        public static event Action                           OnPointerPressed;
        public static event Action<Character, bool>          OnSelectCharacter;
        public static event Action<Vector2, Vector2, bool>   OnMarqueeSelect;
        public static event Action                           OnDeselect, OnSelectAll, OnCycleLeader, OnStop;
        public static event Action<int>                      OnSelectSlot;
        public static event Action<IUsable>                  OnCommandInteractable;
        public static event Action<Character, bool>          OnCommandCharacter;
        public static event Action<Vector3>                  OnCommandMove, OnContinuousCommandMove;
        public static event Action<Vector2>                  OnContextMenuRequested;
        public static event Action<float>                    OnCameraZoomRequested;
        public static event Action<bool>                     OnAltModifierChanged;

        public Camera ActiveCam => CameraStackCoordinator.ActiveBaseCamera ? CameraStackCoordinator.ActiveBaseCamera : (targetCamera ? targetCamera : Camera.main);

        /// Initializes singleton instance and hotkey bindings.
        private void Awake(){
            if (!_instance) _instance = this;
            else if (_instance != this){ Destroy(gameObject); return; }
            Bind(selectAllActionRef, _ => OnSelectAll?.Invoke());
            Bind(cycleLeaderActionRef, _ => OnCycleLeader?.Invoke());
            Bind(deselectActionRef, _ => OnDeselect?.Invoke());
            Bind(stopActionRef, _ => OnStop?.Invoke());
            Bind(slot1ActionRef, _ => OnSelectSlot?.Invoke(0));
            Bind(slot2ActionRef, _ => OnSelectSlot?.Invoke(1));
            Bind(slot3ActionRef, _ => OnSelectSlot?.Invoke(2));
            Bind(slot4ActionRef, _ => OnSelectSlot?.Invoke(3));
            Bind(scrollActionRef, HandleScroll);
            if (modifierAltActionRef) _boundActions.Add(new BoundAction(modifierAltActionRef, _ => OnAltModifierChanged?.Invoke(true), _ => OnAltModifierChanged?.Invoke(false)));
        }

        private void Bind(InputActionReference actionRef, Action<InputAction.CallbackContext> performed){
            if (actionRef) _boundActions.Add(new BoundAction(actionRef, performed));
        }

        /// Cleans up singleton instance on destroy.
        private void OnDestroy(){
            if (_instance == this) _instance = null;
        }

        /// Subscribes input callbacks and enables action maps.
        private void OnEnable(){
            commandActionRef.action.started += HandleCommandStarted;
            commandActionRef.action.canceled += HandleCommandCanceled;
            primarySelectActionRef.action.started += HandleSelectStarted;
            primarySelectActionRef.action.canceled += HandleSelectCanceled;
            ToggleActions(true);
        }

        /// Unsubscribes input callbacks and disables action maps.
        private void OnDisable(){
            commandActionRef.action.started -= HandleCommandStarted;
            commandActionRef.action.canceled -= HandleCommandCanceled;
            primarySelectActionRef.action.started -= HandleSelectStarted;
            primarySelectActionRef.action.canceled -= HandleSelectCanceled;
            ToggleActions(false);
            ResetState();
        }

        /// Toggles all registered input actions active or inactive.
        private void ToggleActions(bool enable){
            Action<InputAction> toggle = enable ? a => a.Enable() : a => a.Disable();
            toggle(commandActionRef.action);
            toggle(primarySelectActionRef.action);
            toggle(pointActionRef.action);
            toggle(modifierAppendActionRef.action);
            foreach (BoundAction b in _boundActions) (enable ? (Action)b.Enable : b.Disable)();
        }

        /// Updates pointer tracking, hover state, drag/hold gestures, and continuous steering.
        private void Update(){
            _currentScreenPos = pointActionRef.action.ReadValue<Vector2>();
            if (!_controlsEnabled) return;
            UpdateHover();

            if (_isPressed && !_holdFired){
                if (Vector2.Distance(_pressStartPos, _currentScreenPos) >= dragThreshold) _isDragging = true;
                else if (Time.unscaledTime - _pressStartTime >= holdThreshold){
                    _holdFired = true;
                    OnContextMenuRequested?.Invoke(_currentScreenPos);
                }
            }

            if (_isCommandHeld && Time.unscaledTime - _lastContinuousCommandTime >= continuousRepathInterval){
                _lastContinuousCommandTime = Time.unscaledTime;
                if (SelectionScanner.IsPointerInsideViewport(_currentScreenPos) && !IsPointerOverUI(_currentScreenPos) && Physics.Raycast(ActiveCam.ScreenPointToRay(_currentScreenPos), out RaycastHit hit, 300f, groundLayer))
                    OnContinuousCommandMove?.Invoke(hit.point);
            }
        }

        /// Evaluates 3D interactable hover state under the pointer.
        private void UpdateHover(){
            if (!SelectionScanner.IsPointerInsideViewport(_currentScreenPos) || IsPointerOverUI(_currentScreenPos)){
                InteractableHighlight.ClearHover();
                return;
            }
            if (Physics.Raycast(ActiveCam.ScreenPointToRay(_currentScreenPos), out RaycastHit hit, 300f, groundLayer))
                InteractableHighlight.SetHoveredInstance(hit.collider.GetComponentInParent<InteractableHighlight>());
            else
                InteractableHighlight.ClearHover();
        }

        /// Initiates selection gesture on select button down.
        private void HandleSelectStarted(InputAction.CallbackContext _){
            OnPointerPressed?.Invoke();
            if (!_controlsEnabled) return;
            Vector2 startPos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(startPos) || IsPointerOverUI(startPos)) return;
            _isPressed = true;
            _isDragging = _holdFired = false;
            _pressStartPos = startPos;
            _pressStartTime = Time.unscaledTime;
        }

        /// Resolves tap unit selection, marquee drag, or deselect on button release.
        private void HandleSelectCanceled(InputAction.CallbackContext _){
            if (!_isPressed || !_controlsEnabled) return;
            Vector2 releasePos = pointActionRef.action.ReadValue<Vector2>();
            _isPressed = false;
            bool isAppend = IsAppendPressed;

            if (_isDragging){
                _isDragging = false;
                OnMarqueeSelect?.Invoke(_pressStartPos, releasePos, isAppend);
                return;
            }
            if (_holdFired) return;

            Character hit = SelectionScanner.RaycastCharacter(ActiveCam, releasePos, characterLayer);
            if (hit) OnSelectCharacter?.Invoke(hit, isAppend);
            else if (!isAppend) OnDeselect?.Invoke();
        }

        /// Evaluates interactables, characters, or ground movement orders on command press.
        private void HandleCommandStarted(InputAction.CallbackContext _){
            OnPointerPressed?.Invoke();
            if (!_controlsEnabled) return;
            Vector2 pos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(pos) || IsPointerOverUI(pos)) return;

            Character hit = SelectionScanner.RaycastCharacter(ActiveCam, pos, characterLayer);
            if (hit){ OnCommandCharacter?.Invoke(hit, IsAppendPressed); return; }

            if (!Physics.Raycast(ActiveCam.ScreenPointToRay(pos), out RaycastHit rayHit, 300f, groundLayer)) return;
            IUsable usable = rayHit.collider.GetComponentInParent<IUsable>();
            if (usable != null){ OnCommandInteractable?.Invoke(usable); return; }

            _isCommandHeld = true;
            _lastContinuousCommandTime = Time.unscaledTime;
            OnCommandMove?.Invoke(rayHit.point);
        }

        /// Resets command button state on release.
        private void HandleCommandCanceled(InputAction.CallbackContext _) => _isCommandHeld = false;

        /// Routes scroll input to an IScrollable or emits camera zoom event.
        private void HandleScroll(InputAction.CallbackContext ctx){
            if (!_controlsEnabled) return;
            float delta = ctx.ReadValue<Vector2>().y;
            if (Mathf.Abs(delta) < 0.01f) return;

            Vector2 pos = pointActionRef.action.ReadValue<Vector2>();
            IScrollable scrollable = FindScrollable(pos);
            if (scrollable != null){ scrollable.OnScroll(delta); return; }
            if (IsPointerOverUI(pos)) return;

            float dir = Mathf.Sign(delta);
            OnCameraZoomRequested?.Invoke(dir);
            CameraAnchor.Zoom(dir);
        }

        /// Finds any IScrollable component under the screen position in UI or 3D world.
        private IScrollable FindScrollable(Vector2 screenPos){
            if (EventSystem.current){
                _uiRaycastResults.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){ position = screenPos }, _uiRaycastResults);
                foreach (RaycastResult result in _uiRaycastResults){
                    IScrollable uiScrollable = result.gameObject.GetComponentInParent<IScrollable>();
                    if (uiScrollable != null) return uiScrollable;
                }
            }
            return Physics.Raycast(ActiveCam.ScreenPointToRay(screenPos), out RaycastHit hit, 500f) ? hit.collider.GetComponentInParent<IScrollable>() : null;
        }

        /// Enables or disables processing of gestures, hotkeys, and commands.
        public void SetControlsEnabled(bool isEnabled){
            _controlsEnabled = isEnabled;
            if (isEnabled) return;
            ResetState();
            InteractableHighlight.ClearHover();
        }

        /// Checks whether the screen position directly hits an active UI element.
        private bool IsPointerOverUI(Vector2 screenPos){
            if (!EventSystem.current) return false;
            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){ position = screenPos }, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        /// Resets active drag, hold, and command hold state.
        public void ResetState() => _isPressed = _isDragging = _holdFired = _isCommandHeld = false;

        /// Renders selection marquee box onto the screen GUI during dragging.
        private void OnGUI(){
            if (!_isDragging) return;
            Rect guiRect = SelectionScanner.GetScreenRect(new Vector2(_pressStartPos.x, Screen.height - _pressStartPos.y), new Vector2(_currentScreenPos.x, Screen.height - _currentScreenPos.y));
            SelectionScanner.DrawScreenRect(guiRect, boxFillColor);
            SelectionScanner.DrawScreenRectBorder(guiRect, 2f, boxBorderColor);
        }
    }
}
