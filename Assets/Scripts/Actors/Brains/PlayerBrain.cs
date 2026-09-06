using System;
using System.Collections.Generic;
using Actors.Player;
using Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Actors.Brains{
    /// <summary>
    /// Coordinates player input for unit selection and target inspection using Unity's New Input System.
    /// Delegates input lifecycle to BoundActions, raycasting/geometry to SelectionScanner,
    /// and selection logic to PartySelection.
    /// </summary>
    public class PlayerBrain : MonoBehaviour{
        public static PlayerBrain Instance{ get; private set; }

        [Header("Party Roster")] [Tooltip("Canonical list of active party members in roster order.")] [SerializeField]
        private List<Character> activePartyMembers = new();

        [Header("Selection Input Actions")] [SerializeField]
        private InputActionReference primarySelectActionRef;

        [SerializeField] private InputActionReference pointActionRef;
        [SerializeField] private InputActionReference selectAllActionRef;
        [SerializeField] private InputActionReference cycleLeaderActionRef;
        [SerializeField] private InputActionReference deselectActionRef;
        [SerializeField] private InputActionReference slot1ActionRef;
        [SerializeField] private InputActionReference slot2ActionRef;
        [SerializeField] private InputActionReference slot3ActionRef;
        [SerializeField] private InputActionReference slot4ActionRef;

        [Header("Inspection Input Action")] [SerializeField]
        private InputActionReference inspectActionRef;

        [Header("Raycast & Layers")] [SerializeField]
        private LayerMask characterLayer;

        [SerializeField] private Camera mainCamera;

        [Header("Box Selection Visuals")] [SerializeField]
        private Color boxBorderColor = new(0.2f, 0.8f, 0.2f, 0.9f);

        [SerializeField] private Color boxFillColor  = new(0.2f, 0.8f, 0.2f, 0.2f);
        [SerializeField] private float dragThreshold = 10f;

        private readonly PartySelection    _selection    = new();
        private readonly List<BoundAction> _boundActions = new();

        private bool    _isDragging;
        private Vector2 _dragStartScreenPos;
        private Vector2 _currentScreenPos;

        public PartySelection     Selection          => _selection;
        public Character          Lead               => _selection.Lead;
        public HashSet<Character> SelectedCharacters => _selection.Selected;
        public List<Character>    ActivePartyMembers => activePartyMembers;

        public Sheet                      CurrentInspectedSheet{ get; private set; }
        public static event Action<Sheet> OnSheetInspected;

        private void Awake(){
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            if (mainCamera == null)
                mainCamera = Camera.main;

            InitializeBoundActions();
        }

        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }

        private void InitializeBoundActions(){
            _boundActions.Add(new BoundAction(primarySelectActionRef, OnPrimarySelectStarted, OnPrimarySelectCanceled));
            _boundActions.Add(new BoundAction(inspectActionRef,       OnInspectPerformed));
            _boundActions.Add(new BoundAction(selectAllActionRef,     _ => _selection.SelectAll(activePartyMembers)));
            _boundActions.Add(new BoundAction(cycleLeaderActionRef,   _ => _selection.CycleLeader(activePartyMembers)));
            _boundActions.Add(new BoundAction(deselectActionRef,      _ => _selection.Clear()));
            _boundActions.Add(new BoundAction(slot1ActionRef,         _ => SelectSlot(0)));
            _boundActions.Add(new BoundAction(slot2ActionRef,         _ => SelectSlot(1)));
            _boundActions.Add(new BoundAction(slot3ActionRef,         _ => SelectSlot(2)));
            _boundActions.Add(new BoundAction(slot4ActionRef,         _ => SelectSlot(3)));
        }

        private void OnEnable(){
            foreach (BoundAction t in _boundActions)
                t.Enable();

            pointActionRef.action.Enable();
        }

        private void OnDisable(){
            foreach (BoundAction t in _boundActions)
                t.Disable();

            pointActionRef.action.Disable();
            _isDragging = false;
        }

        private void Update() => _currentScreenPos = pointActionRef.action.ReadValue<Vector2>();

        private void OnPrimarySelectStarted(InputAction.CallbackContext context){
            Vector2 startPos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(startPos)) return;

            _isDragging         = true;
            _dragStartScreenPos = startPos;
        }

        private void OnPrimarySelectCanceled(InputAction.CallbackContext context){
            if (!_isDragging) return;
            _isDragging = false;

            Vector2 releasePos   = pointActionRef.action.ReadValue<Vector2>();
            float   dragDistance = Vector2.Distance(_dragStartScreenPos, releasePos);
            bool    isShiftHeld  = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

            if (dragDistance < dragThreshold){
                Character hitCharacter = SelectionScanner.RaycastCharacter(mainCamera, releasePos, characterLayer);
                if (hitCharacter == null) return;
                if (isShiftHeld)
                    _selection.ToggleAddSelection(hitCharacter);
                else
                    _selection.SingleUnitSelect(hitCharacter);
            }
            else{
                List<Character> enclosed = SelectionScanner.GetCharactersInScreenRect(
                    mainCamera, _dragStartScreenPos, releasePos, activePartyMembers);

                if (enclosed.Count <= 0) return;
                if (isShiftHeld)
                    _selection.AdditiveBoxSelect(enclosed);
                else
                    _selection.DragboxSelect(enclosed);
            }
        }

        private void OnInspectPerformed(InputAction.CallbackContext context){
            Vector2 mousePos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(mousePos)) return;

            ISelectable selectable = SelectionScanner.RaycastSelectable(mainCamera, mousePos, characterLayer);
            if (selectable == null || selectable.Sheet == null) return;
            CurrentInspectedSheet = selectable.Sheet;
            OnSheetInspected?.Invoke(CurrentInspectedSheet);
        }

        private void SelectSlot(int index){
            if (index >= 0 && index < activePartyMembers.Count)
                _selection.SingleUnitSelect(activePartyMembers[index]);
        }

        private void OnGUI(){
            if (!_isDragging) return;

            float distance = Vector2.Distance(_dragStartScreenPos, _currentScreenPos);
            if (distance < dragThreshold) return;

            Vector2 guiStart   = new(_dragStartScreenPos.x, Screen.height - _dragStartScreenPos.y);
            Vector2 guiCurrent = new(_currentScreenPos.x, Screen.height - _currentScreenPos.y);
            Rect    guiRect    = SelectionScanner.GetScreenRect(guiStart, guiCurrent);

            SelectionScanner.DrawScreenRect(guiRect, boxFillColor);
            SelectionScanner.DrawScreenRectBorder(guiRect, 2f, boxBorderColor);
        }
    }
}
