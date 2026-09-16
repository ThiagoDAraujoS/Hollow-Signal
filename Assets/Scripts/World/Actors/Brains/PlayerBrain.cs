using System;
using System.Collections.Generic;
using Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using World.Actors.Player;
using World.Anchors;

namespace World.Actors.Brains{
    /// <summary>
    /// Central input coordinator and party facade.
    /// Delegates selection gestures to SelectionGestureHandler, movement and usable commands to PlayerCommandDispatcher,
    /// and selection state to PartySelection.
    /// </summary>
    public class PlayerBrain : MonoBehaviour{
        private static PlayerBrain _instance;

        [Header("Party Roster")] [Tooltip("Canonical list of active party members in roster order.")] [SerializeField]
        private List<Character> activePartyMembers = new();

        [Header("Mouse Action References")] [Tooltip("Command / Action: Left Click by default.")] [FormerlySerializedAs("inspectActionRef")] [SerializeField]
        private InputActionReference commandActionRef;

        [Tooltip("Select / Box Select / Context: Right Click by default.")] [SerializeField]
        private InputActionReference primarySelectActionRef;

        [SerializeField] private InputActionReference pointActionRef;
        [SerializeField] private InputActionReference scrollActionRef;
        [SerializeField] private InputActionReference selectAllActionRef;
        [SerializeField] private InputActionReference cycleLeaderActionRef;
        [SerializeField] private InputActionReference deselectActionRef;
        [SerializeField] private InputActionReference stopActionRef;
        [SerializeField] private InputActionReference slot1ActionRef;
        [SerializeField] private InputActionReference slot2ActionRef;
        [SerializeField] private InputActionReference slot3ActionRef;
        [SerializeField] private InputActionReference slot4ActionRef;

        [Header("Modifier Key Actions")]
        [FormerlySerializedAs("modifierShiftActionRef")]
        [SerializeField] private InputActionReference modifierAppendActionRef;

        [SerializeField] private InputActionReference modifierAltActionRef;

        [Header("Raycast & Layers")] [SerializeField]
        private LayerMask characterLayer;

        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private Camera    mainCamera;

        [Header("Timing & Thresholds")] [SerializeField]
        private float dragThreshold = 10f;

        [SerializeField] private float holdThreshold = 0.35f;

        [Tooltip("Interval in seconds between continuous path updates when holding Left Click.")] [SerializeField]
        private float continuousRepathInterval = 0.08f;

        [Header("Box Selection Visuals")] [SerializeField]
        private Color boxBorderColor = new(0.2f, 0.8f, 0.2f, 0.9f);

        [SerializeField] private Color boxFillColor = new(0.2f, 0.8f, 0.2f, 0.2f);

        private readonly PartySelection          _selection        = new();
        private readonly List<BoundAction>       _boundActions     = new();
        private readonly List<RaycastResult>     _uiRaycastResults = new();
        private          SelectionGestureHandler _gestureHandler;
        private          PlayerCommandDispatcher _commandDispatcher;

        private Vector2 _currentScreenPos;

        public static PartySelection     Selection          => _instance._selection;
        public static Character          Lead               => _instance._selection.Lead;
        public static HashSet<Character> SelectedCharacters => _instance._selection.Selected;
        public static List<Character>    ActivePartyMembers => _instance.activePartyMembers;

        public static Sheet               CurrentInspectedSheet{ get; private set; }
        public static event Action<Sheet> OnSheetInspected;

        public static event Action<Vector2> OnDirectCommand;
        public static event Action<Vector2> OnContinuousCommand;
        public static event Action<Vector2> OnContextMenuRequested;
        public static event Action<bool>    OnAltModifierChanged;

        public static bool IsAppendPressed => _instance.modifierAppendActionRef.action.IsPressed();
        public static bool IsShiftPressed  => IsAppendPressed;

        public static bool IsAltPressed => _instance.modifierAltActionRef != null && _instance.modifierAltActionRef.action.IsPressed();

        private void Awake(){
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);

            if (modifierAppendActionRef == null) {
                PlayerInput playerInput = GetComponent<PlayerInput>();
                modifierAppendActionRef = InputActionReference.Create(playerInput.actions.FindAction("ModifierAppend"));
            }

            if (scrollActionRef == null && TryGetComponent<PlayerInput>(out var input))
                scrollActionRef = InputActionReference.Create(input.actions.FindAction("Zoom"));

            if (mainCamera == null)
                mainCamera = Camera.main;

            _gestureHandler = new SelectionGestureHandler(
                mainCamera,
                characterLayer,
                _selection,
                activePartyMembers,
                dragThreshold,
                holdThreshold,
                boxBorderColor,
                boxFillColor);

            _gestureHandler.OnContextMenuRequested += pos => OnContextMenuRequested?.Invoke(pos);

            _commandDispatcher = new PlayerCommandDispatcher(
                mainCamera,
                groundLayer,
                continuousRepathInterval);

            InitializeBoundActions();
            _selection.OnSelectionChanged += UpdateSelectionCircles;
        }

        private void OnDestroy(){
            if (_instance != this) return;
            _selection.OnSelectionChanged -= UpdateSelectionCircles;
            _instance                     =  null;
        }

        /// Sets the active world camera for command dispatching and unit selection.
        public static void SetCamera(Camera camera){
            if (_instance == null) return;
            _instance.mainCamera = camera;
            _instance._commandDispatcher.SetCamera(camera);
            _instance._gestureHandler.SetCamera(camera);
        }

        public static void AddPartyMember(Character character){
            if (!_instance.activePartyMembers.Contains(character))
                _instance.activePartyMembers.Add(character);
        }

        public static void RemovePartyMember(Character character) => _instance.activePartyMembers.Remove(character);

        public static void ClearPartyMembers() => _instance.activePartyMembers.Clear();

        public static bool IsPartyMember(Character character) => _instance.activePartyMembers.Contains(character);

        public static bool IsSelected(Character character) => _instance._selection.Contains(character);

        public static void Deselect(Character character){
            if (_instance._selection.Contains(character))
                _instance._selection.ToggleAddSelection(character);
        }

        public static void Inspect(ISelectable selectable){
            if (selectable?.Sheet == null) return;
            CurrentInspectedSheet = selectable.Sheet;
            OnSheetInspected?.Invoke(CurrentInspectedSheet);
        }

        public static void StopSelectedUnits() =>
            PlayerCommandDispatcher.StopUnits(_instance._selection.Selected);

        private void InitializeBoundActions(){
            _boundActions.Add(new BoundAction(commandActionRef,       OnCommandStarted,       OnCommandCanceled));
            _boundActions.Add(new BoundAction(primarySelectActionRef, OnPrimarySelectStarted, OnPrimarySelectCanceled));
            _boundActions.Add(new BoundAction(selectAllActionRef,     _ => _selection.SelectAll(activePartyMembers)));
            _boundActions.Add(new BoundAction(cycleLeaderActionRef,   _ => _selection.CycleLeader(activePartyMembers)));
            _boundActions.Add(new BoundAction(deselectActionRef,      _ => _selection.Clear()));
            _boundActions.Add(new BoundAction(stopActionRef,          _ => StopSelectedUnits()));
            _boundActions.Add(new BoundAction(slot1ActionRef,         _ => SelectSlot(0)));
            _boundActions.Add(new BoundAction(slot2ActionRef,         _ => SelectSlot(1)));
            _boundActions.Add(new BoundAction(slot3ActionRef,         _ => SelectSlot(2)));
            _boundActions.Add(new BoundAction(slot4ActionRef,         _ => SelectSlot(3)));

            if (scrollActionRef != null)
                _boundActions.Add(new BoundAction(scrollActionRef, OnScrollPerformed));

            if (modifierAltActionRef != null)
                _boundActions.Add(new BoundAction(modifierAltActionRef, _ => OnAltModifierChanged?.Invoke(true), _ => OnAltModifierChanged?.Invoke(false)));
        }

        private void OnEnable(){
            foreach (BoundAction t in _boundActions)
                t.Enable();

            pointActionRef.action.Enable();
            modifierAppendActionRef.action.Enable();
            UpdateSelectionCircles();
        }

        private void OnDisable(){
            foreach (BoundAction t in _boundActions)
                t.Disable();

            pointActionRef.action.Disable();
            modifierAppendActionRef.action.Disable();
            _commandDispatcher.Reset();
            _gestureHandler.Reset();
        }

        private void Update(){
            _currentScreenPos = pointActionRef.action.ReadValue<Vector2>();
            _gestureHandler.Update(_currentScreenPos);

            if (!_commandDispatcher.IsCommandHeld || !SelectionScanner.IsPointerInsideViewport(_currentScreenPos)) return;
            _commandDispatcher.UpdateContinuous(_currentScreenPos, Lead, _selection.Selected);
            OnContinuousCommand?.Invoke(_currentScreenPos);
        }

        /// Selects a hero or dispatches direct command at pointer position.
        private void OnCommandStarted(InputAction.CallbackContext context){
            Vector2 mousePos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(mousePos)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (_selection.Count == 0){
                Character hitCharacter = SelectionScanner.RaycastCharacter(mainCamera, mousePos, characterLayer);
                if (hitCharacter != null){
                    _selection.SingleUnitSelect(hitCharacter);
                    return;
                }
            }

            _commandDispatcher.OnCommandStarted();
            _commandDispatcher.ExecuteDirectCommand(mousePos, Lead, _selection.Selected);
            OnDirectCommand?.Invoke(mousePos);
        }

        private void OnCommandCanceled(InputAction.CallbackContext context) => _commandDispatcher.OnCommandCanceled();

        private void OnPrimarySelectStarted(InputAction.CallbackContext context){
            Vector2 startPos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(startPos)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            _gestureHandler.OnPressStarted(startPos);
        }

        private void OnPrimarySelectCanceled(InputAction.CallbackContext context){
            Vector2 releasePos = pointActionRef.action.ReadValue<Vector2>();
            _gestureHandler.OnPressCanceled(releasePos, IsAppendPressed);
        }

        /// Searches for an IScrollable target under the pointer in UI or the 3D world.
        private IScrollable FindScrollable(Vector2 screenPos){
            if (EventSystem.current != null){
                PointerEventData pointerData = new(EventSystem.current){ position = screenPos };
                _uiRaycastResults.Clear();
                EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
                foreach (RaycastResult result in _uiRaycastResults){
                    IScrollable uiScrollable = result.gameObject.GetComponentInParent<IScrollable>();
                    if (uiScrollable != null)
                        return uiScrollable;
                }
            }

            if (mainCamera != null && Physics.Raycast(mainCamera.ScreenPointToRay(screenPos), out RaycastHit hit, 500f))
                return hit.collider.GetComponentInParent<IScrollable>();

            return null;
        }

        /// Routes scroll wheel input to an IScrollable target or defaults to camera map zoom.
        private void OnScrollPerformed(InputAction.CallbackContext context){
            float scrollDelta = context.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollDelta) < 0.01f) return;

            Vector2 mousePos = pointActionRef.action.ReadValue<Vector2>();
            IScrollable scrollable = FindScrollable(mousePos);
            if (scrollable != null){
                scrollable.OnScroll(scrollDelta);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            CameraAnchor.Zoom(Mathf.Sign(scrollDelta));
        }

        private void SelectSlot(int index){
            if (index < 0 || index >= activePartyMembers.Count) return;
            Character hero = activePartyMembers[index];

            if (IsAppendPressed) {
                _selection.AddUnitSelect(hero);
            }
            else {
                _selection.SingleUnitSelect(hero);
                CameraAnchor.Track(hero.BodyTransform);
            }
        }

        private void UpdateSelectionCircles() =>
            activePartyMembers.ForEach(member => {
                if (member == null) return;
                if (_selection.Contains(member))
                    member.TurnSelectionCircleOn();
                else
                    member.TurnSelectionCircleOff();
            });

        private void OnGUI() => _gestureHandler.DrawGUI(_currentScreenPos);
    }
}
