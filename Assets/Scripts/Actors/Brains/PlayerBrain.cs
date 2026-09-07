using System;
using System.Collections.Generic;
using Actors.Player;
using Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Actors.Brains{
    /// <summary>
    /// Coordinates player input for direct commands / continuous hold-steering (Left Click) 
    /// and unit selection / context menus (Right Click) using Unity's New Input System. 
    /// Delegates input lifecycle to BoundActions, raycasting/geometry to SelectionScanner,
    /// and selection logic to PartySelection.
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
        [SerializeField] private InputActionReference selectAllActionRef;
        [SerializeField] private InputActionReference cycleLeaderActionRef;
        [SerializeField] private InputActionReference deselectActionRef;
        [SerializeField] private InputActionReference slot1ActionRef;
        [SerializeField] private InputActionReference slot2ActionRef;
        [SerializeField] private InputActionReference slot3ActionRef;
        [SerializeField] private InputActionReference slot4ActionRef;

        [Header("Modifier Key Actions")] [SerializeField]
        private InputActionReference modifierShiftActionRef;

        [SerializeField] private InputActionReference modifierAltActionRef;

        [Header("Raycast & Layers")] [SerializeField]
        private LayerMask characterLayer;

        [SerializeField] private Camera mainCamera;

        [Header("Timing & Thresholds")] [SerializeField]
        private float dragThreshold = 10f;

        [SerializeField] private float holdThreshold = 0.35f;

        [Tooltip("Interval in seconds between continuous path updates when holding Left Click.")] [SerializeField]
        private float continuousRepathInterval = 0.08f;

        [Header("Box Selection Visuals")] [SerializeField]
        private Color boxBorderColor = new(0.2f, 0.8f, 0.2f, 0.9f);

        [SerializeField] private Color boxFillColor = new(0.2f, 0.8f, 0.2f, 0.2f);

        private readonly PartySelection    _selection    = new();
        private readonly List<BoundAction> _boundActions = new();

        private Vector2 _currentScreenPos;

        private bool  _isLeftPressed;
        private float _lastContinuousCommandTime;

        private bool    _isRightPressed;
        private bool    _isRightDragging;
        private Vector2 _rightPressStartPos;
        private float   _rightPressStartTime;
        private bool    _contextMenuFired;

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

        public static bool IsShiftPressed => _instance != null && _instance.modifierShiftActionRef != null && _instance.modifierShiftActionRef.action.IsPressed();
        public static bool IsAltPressed   => _instance != null && _instance.modifierAltActionRef != null && _instance.modifierAltActionRef.action.IsPressed();

        private void Awake(){
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);

            if (mainCamera == null)
                mainCamera = Camera.main;

            InitializeBoundActions();
            _selection.OnSelectionChanged += UpdateSelectionCircles;
        }

        private void OnDestroy(){
            if (_instance != this) return;
            _selection.OnSelectionChanged -= UpdateSelectionCircles;
            _instance                     =  null;
        }

        public static void AddPartyMember(Character character){
            if (!_instance.activePartyMembers.Contains(character))
                _instance.activePartyMembers.Add(character);
        }

        public static void RemovePartyMember(Character character) =>
            _instance.activePartyMembers.Remove(character);

        public static void ClearPartyMembers() =>
            _instance.activePartyMembers.Clear();

        public static bool IsPartyMember(Character character) =>
            _instance.activePartyMembers.Contains(character);

        public static bool IsSelected(Character character) =>
            _instance._selection.Contains(character);

        public static void Deselect(Character character){
            if (_instance._selection.Contains(character))
                _instance._selection.ToggleAddSelection(character);
        }

        public static void Inspect(ISelectable selectable){
            if (selectable?.Sheet == null) return;
            CurrentInspectedSheet = selectable.Sheet;
            OnSheetInspected?.Invoke(CurrentInspectedSheet);
        }

        private void InitializeBoundActions(){
            _boundActions.Add(new BoundAction(commandActionRef,       OnCommandStarted,       OnCommandCanceled));
            _boundActions.Add(new BoundAction(primarySelectActionRef, OnPrimarySelectStarted, OnPrimarySelectCanceled));
            _boundActions.Add(new BoundAction(selectAllActionRef,     _ => _selection.SelectAll(activePartyMembers)));
            _boundActions.Add(new BoundAction(cycleLeaderActionRef,   _ => _selection.CycleLeader(activePartyMembers)));
            _boundActions.Add(new BoundAction(deselectActionRef,      _ => _selection.Clear()));
            _boundActions.Add(new BoundAction(slot1ActionRef,         _ => SelectSlot(0)));
            _boundActions.Add(new BoundAction(slot2ActionRef,         _ => SelectSlot(1)));
            _boundActions.Add(new BoundAction(slot3ActionRef,         _ => SelectSlot(2)));
            _boundActions.Add(new BoundAction(slot4ActionRef,         _ => SelectSlot(3)));

            if (modifierAltActionRef != null)
                _boundActions.Add(new BoundAction(modifierAltActionRef, _ => OnAltModifierChanged?.Invoke(true), _ => OnAltModifierChanged?.Invoke(false)));
        }

        private void OnEnable(){
            foreach (BoundAction t in _boundActions)
                t.Enable();

            pointActionRef?.action.Enable();
            modifierShiftActionRef?.action.Enable();
            UpdateSelectionCircles();
        }

        private void OnDisable(){
            foreach (BoundAction t in _boundActions)
                t.Disable();

            pointActionRef?.action.Disable();
            modifierShiftActionRef?.action.Disable();
            _isLeftPressed    = false;
            _isRightPressed   = false;
            _isRightDragging  = false;
            _contextMenuFired = false;
        }

        private void Update(){
            _currentScreenPos = pointActionRef.action.ReadValue<Vector2>();

            if (_isLeftPressed && Time.unscaledTime - _lastContinuousCommandTime >= continuousRepathInterval){
                _lastContinuousCommandTime = Time.unscaledTime;
                if (SelectionScanner.IsPointerInsideViewport(_currentScreenPos))
                    OnContinuousCommand?.Invoke(_currentScreenPos);
            }

            if (!_isRightPressed || _contextMenuFired) return;
            float dragDist = Vector2.Distance(_rightPressStartPos, _currentScreenPos);
            if (dragDist >= dragThreshold)
                _isRightDragging = true;
            else if (Time.unscaledTime - _rightPressStartTime >= holdThreshold){
                _contextMenuFired = true;
                OnContextMenuRequested?.Invoke(_currentScreenPos);
            }
        }

        private void OnCommandStarted(InputAction.CallbackContext context){
            Vector2 mousePos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(mousePos)) return;

            _isLeftPressed             = true;
            _lastContinuousCommandTime = Time.unscaledTime;
            OnDirectCommand?.Invoke(mousePos);
        }

        private void OnCommandCanceled(InputAction.CallbackContext context){
            if (!_isLeftPressed) return;
            _isLeftPressed = false;
        }

        private void OnPrimarySelectStarted(InputAction.CallbackContext context){
            Vector2 startPos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(startPos)) return;

            _isRightPressed      = true;
            _isRightDragging     = false;
            _rightPressStartPos  = startPos;
            _rightPressStartTime = Time.unscaledTime;
            _contextMenuFired    = false;
        }

        private void OnPrimarySelectCanceled(InputAction.CallbackContext context){
            if (!_isRightPressed) return;
            _isRightPressed = false;

            if (_isRightDragging){
                _isRightDragging = false;
                Vector2 releasePos = pointActionRef.action.ReadValue<Vector2>();
                List<Character> enclosed = SelectionScanner.GetCharactersInScreenRect(
                    mainCamera, _rightPressStartPos, releasePos, activePartyMembers);

                if (enclosed.Count <= 0) return;
                if (IsShiftPressed)
                    _selection.AdditiveBoxSelect(enclosed);
                else
                    _selection.DragboxSelect(enclosed);
            }
            else if (!_contextMenuFired){
                Vector2   releasePos   = pointActionRef.action.ReadValue<Vector2>();
                Character hitCharacter = SelectionScanner.RaycastCharacter(mainCamera, releasePos, characterLayer);
                if (hitCharacter == null) return;

                if (IsShiftPressed)
                    _selection.ToggleAddSelection(hitCharacter);
                else
                    _selection.SingleUnitSelect(hitCharacter);
            }
        }

        private void SelectSlot(int index){
            if (index >= 0 && index < activePartyMembers.Count)
                _selection.SingleUnitSelect(activePartyMembers[index]);
        }

        private void UpdateSelectionCircles(){
            foreach (Character member in activePartyMembers){
                if (member == null) continue;

                if (_selection.Contains(member))
                    member.TurnSelectionCircleOn();
                else
                    member.TurnSelectionCircleOff();
            }
        }

        private void OnGUI(){
            if (!_isRightDragging) return;

            float distance = Vector2.Distance(_rightPressStartPos, _currentScreenPos);
            if (distance < dragThreshold) return;

            Vector2 guiStart   = new(_rightPressStartPos.x, Screen.height - _rightPressStartPos.y);
            Vector2 guiCurrent = new(_currentScreenPos.x, Screen.height - _currentScreenPos.y);
            Rect    guiRect    = SelectionScanner.GetScreenRect(guiStart, guiCurrent);

            SelectionScanner.DrawScreenRect(guiRect, boxFillColor);
            SelectionScanner.DrawScreenRectBorder(guiRect, 2f, boxBorderColor);
        }
    }
}
