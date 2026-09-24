using System;
using System.Collections.Generic;
using Core.Input;
using UI.Dialog;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using World.Actors.Player;
using World.Anchors;

namespace World.Actors.Brains{
    /// Central input coordinator and party facade.
    public class PlayerBrain : MonoBehaviour{
        private static PlayerBrain _instance;

        [Header("Party Roster")] [Tooltip("Canonical list of active party members in roster order.")] [SerializeField]
        private List<Character> activePartyMembers = new();

        [Header("Dialogue Screens (Roster Slots 1-4)")]
        [SerializeField] private DialogueController[] dialogueScreens = new DialogueController[4];
        [SerializeField] private GameObject           sharedDialogueBackground;

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

        [Header("Modifier Key Actions")] [FormerlySerializedAs("modifierShiftActionRef")] [SerializeField]
        private InputActionReference modifierAppendActionRef;

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
        private bool    _isDialogueActive;

        public static PlayerBrain        Instance           => _instance;
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

        /// Initializes singleton instance, binds input action references, and sets up selection handlers.
        private void Awake(){
            if (_instance == null)
                _instance = this;
            else if (_instance != this){
                Destroy(gameObject);
                return;
            }

            if (modifierAppendActionRef == null){
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
            _selection.OnSelectionChanged += UpdateDialogueScreens;
            SyncDialogueScreens();
        }

        /// Cleans up selection event listeners and singleton instance reference.
        private void OnDestroy(){
            if (_instance != this) return;
            _selection.OnSelectionChanged -= UpdateSelectionCircles;
            _selection.OnSelectionChanged -= UpdateDialogueScreens;
            _instance                     =  null;
        }

        /// Synchronizes screens and hides dialogue background on session start.
        private void Start(){
            SyncDialogueScreens();
            UpdateDialogueScreens();
        }

        /// Sets the active world camera for command dispatching and unit selection.
        public static void SetCamera(Camera camera){
            if (_instance == null) return;
            _instance.mainCamera = camera;
            _instance._commandDispatcher.SetCamera(camera);
            _instance._gestureHandler.SetCamera(camera);
        }

        /// Appends character to active party roster if not already present.
        public static void AddPartyMember(Character character){
            if (_instance.activePartyMembers.Contains(character)) return;
            _instance.activePartyMembers.Add(character);
            _instance.SyncDialogueScreens();
        }

        /// Removes character from active party roster.
        public static void RemovePartyMember(Character character){
            _instance.activePartyMembers.Remove(character);
            _instance.SyncDialogueScreens();
        }

        /// Clears all characters from active party roster.
        public static void ClearPartyMembers(){
            _instance.activePartyMembers.Clear();
            _instance.SyncDialogueScreens();
        }

        /// Checks if a character is currently in the active party roster.
        public static bool IsPartyMember(Character character) => _instance.activePartyMembers.Contains(character);

        /// Checks if a character is currently part of the active selection.
        public static bool IsSelected(Character character) => _instance._selection.Contains(character);

        /// Removes a character from the active selection.
        public static void Deselect(Character character){
            if (_instance._selection.Contains(character))
                _instance._selection.ToggleAddSelection(character);
        }

        /// Inspects character or interactable sheet and fires inspection event.
        public static void Inspect(ISelectable selectable){
            if (selectable?.Sheet == null) return;
            CurrentInspectedSheet = selectable.Sheet;
            OnSheetInspected?.Invoke(CurrentInspectedSheet);
        }

        /// Commands all currently selected units to halt movement.
        public static void StopSelectedUnits() =>
            PlayerCommandDispatcher.StopUnits(_instance._selection.Selected);

        /// Binds input system action callbacks to party commands and selection behaviors.
        private void InitializeBoundActions(){
            _boundActions.Add(new BoundAction(commandActionRef,       OnCommandStarted,       OnCommandCanceled));
            _boundActions.Add(new BoundAction(primarySelectActionRef, OnPrimarySelectStarted, OnPrimarySelectCanceled));
            _boundActions.Add(new BoundAction(selectAllActionRef,     _ => _selection.SelectAll(activePartyMembers)));
            _boundActions.Add(new BoundAction(cycleLeaderActionRef,   _ => {
                _selection.CycleLeader(activePartyMembers);
            }));
            _boundActions.Add(new BoundAction(deselectActionRef,      _ => {
                bool leadInDialogue = Lead != null && Lead.dialogueSession != null && Lead.dialogueSession.HasActiveDialogue;
                if (!leadInDialogue) _selection.Clear();
            }));
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

        /// Enables bound input actions and registers dialogue listeners.
        private void OnEnable(){
            if (_instance != this) return;

            foreach (BoundAction t in _boundActions)
                t.Enable();

            pointActionRef.action.Enable();
            modifierAppendActionRef.action.Enable();
            DialogueController.OnDialogueActiveChanged += HandleDialogueActiveChanged;
            UpdateSelectionCircles();
            UpdateDialogueScreens();
        }

        /// Disables bound input actions and unregisters dialogue listeners.
        private void OnDisable(){
            if (_instance != this) return;

            foreach (BoundAction t in _boundActions)
                t.Disable();

            pointActionRef.action.Disable();
            modifierAppendActionRef.action.Disable();
            DialogueController.OnDialogueActiveChanged -= HandleDialogueActiveChanged;
            _commandDispatcher.Reset();
            _gestureHandler.Reset();
        }

        /// Synchronizes screen index and controller references across active party members.
        public void SyncDialogueScreens(){
            if (dialogueScreens == null || dialogueScreens.Length == 0)
                dialogueScreens = new DialogueController[4];

            if (dialogueScreens[0] == null){
                DialogueController[] controllers = FindObjectsByType<DialogueController>(FindObjectsInactive.Include);
                for (int i = 0; i < controllers.Length && i < dialogueScreens.Length; i++)
                    dialogueScreens[i] = controllers[i];
            }

            if (sharedDialogueBackground == null && dialogueScreens.Length > 0 && dialogueScreens[0] != null && dialogueScreens[0].DialogRoot != null)
                sharedDialogueBackground = dialogueScreens[0].DialogRoot;

            for (int i = 0; i < activePartyMembers.Count; i++){
                DialogueController controller = i < dialogueScreens.Length ? dialogueScreens[i] : null;
                if (activePartyMembers[i] != null && activePartyMembers[i].dialogueSession != null)
                    activePartyMembers[i].dialogueSession.BindScreen(i, controller);
            }
        }

        /// Synchronizes visibility of dialogue screens and shared background with the selected lead hero.
        public void UpdateDialogueScreens(){
            Character lead = Lead;
            bool hasActiveDialogue = lead != null && lead.dialogueSession != null && lead.dialogueSession.HasActiveDialogue;

            if (sharedDialogueBackground != null)
                sharedDialogueBackground.SetActive(hasActiveDialogue);

            for (int i = 0; i < dialogueScreens.Length; i++){
                if (dialogueScreens[i] == null) continue;
                bool shouldShow = hasActiveDialogue && (lead.dialogueSession.ScreenIndex == i || lead.dialogueSession.Controller == dialogueScreens[i]);
                dialogueScreens[i].SetVisible(shouldShow);
            }

            foreach (Character member in activePartyMembers){
                if (member == null || member.dialogueSession == null || member.dialogueSession.Controller == null) continue;
                if (member != lead)
                    member.dialogueSession.Controller.SetVisible(false);
            }

            if (!hasActiveDialogue){
                if (sharedDialogueBackground != null)
                    sharedDialogueBackground.SetActive(false);
                else if (dialogueScreens.Length > 0 && dialogueScreens[0] != null && dialogueScreens[0].DialogRoot != null)
                    dialogueScreens[0].DialogRoot.SetActive(false);
            }
        }

        /// Resets gesture and command dispatchers and halts units when dialogue enters or exits.
        private void HandleDialogueActiveChanged(bool isActive){
            _isDialogueActive = isActive;
            UpdateDialogueScreens();
            if (!isActive) return;
            _commandDispatcher.Reset();
            _gestureHandler.Reset();
            StopSelectedUnits();
        }

        /// Updates pointer tracking, gesture recognition, and continuous movement dispatching.
        private void Update(){
            _currentScreenPos = pointActionRef.action.ReadValue<Vector2>();
            _gestureHandler.Update(_currentScreenPos);

            bool leadInDialogue = Lead != null && Lead.dialogueSession != null && Lead.dialogueSession.HasActiveDialogue;
            if (leadInDialogue) return;

            if (!_commandDispatcher.IsCommandHeld || !SelectionScanner.IsPointerInsideViewport(_currentScreenPos)) return;
            _commandDispatcher.UpdateContinuous(_currentScreenPos, Lead, _selection.Selected);
            OnContinuousCommand?.Invoke(_currentScreenPos);
        }

        /// Checks whether the screen position directly hits a UI element using EventSystem raycast.
        private bool IsPointerOverUI(Vector2 screenPos){
            if (EventSystem.current == null) return false;
            PointerEventData pointerData = new(EventSystem.current){ position = screenPos };
            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        /// Selects a hero or dispatches direct command at pointer position.
        private void OnCommandStarted(InputAction.CallbackContext context){
            Vector2 mousePos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(mousePos) || IsPointerOverUI(mousePos)) return;

            Character hitCharacter = SelectionScanner.RaycastCharacter(mainCamera, mousePos, characterLayer);
            if (hitCharacter != null && activePartyMembers.Contains(hitCharacter)){
                if (IsAppendPressed)
                    _selection.AddUnitSelect(hitCharacter);
                else{
                    if (_selection.Contains(hitCharacter) && _selection.Count > 1)
                        _selection.SetLead(hitCharacter);
                    else
                        _selection.SingleUnitSelect(hitCharacter);
                }
                return;
            }

            bool leadInDialogue = Lead != null && Lead.dialogueSession != null && Lead.dialogueSession.HasActiveDialogue;
            if (leadInDialogue) return;

            _commandDispatcher.OnCommandStarted();
            _commandDispatcher.ExecuteDirectCommand(mousePos, Lead, _selection.Selected);
            OnDirectCommand?.Invoke(mousePos);
        }

        /// Resets command dispatcher state on command button release.
        private void OnCommandCanceled(InputAction.CallbackContext context) => _commandDispatcher.OnCommandCanceled();

        /// Starts selection gesture handling on primary select press.
        private void OnPrimarySelectStarted(InputAction.CallbackContext context){
            Vector2 startPos = pointActionRef.action.ReadValue<Vector2>();
            if (!SelectionScanner.IsPointerInsideViewport(startPos) || IsPointerOverUI(startPos)) return;

            _gestureHandler.OnPressStarted(startPos);
        }

        /// Concludes selection gesture handling on primary select release.
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

            Vector2     mousePos   = pointActionRef.action.ReadValue<Vector2>();
            IScrollable scrollable = FindScrollable(mousePos);
            if (scrollable != null){
                scrollable.OnScroll(scrollDelta);
                return;
            }

            if (IsPointerOverUI(mousePos)) return;

            CameraAnchor.Zoom(Mathf.Sign(scrollDelta));
        }

        /// Selects active party member corresponding to slot index.
        public void SelectSlot(int index){
            if (index < 0 || index >= activePartyMembers.Count) return;
            Character hero = activePartyMembers[index];

            if (IsAppendPressed)
                _selection.AddUnitSelect(hero);
            else{
                if (_selection.Contains(hero) && _selection.Count > 1)
                    _selection.SetLead(hero);
                else
                    _selection.SingleUnitSelect(hero);
            }
        }

        /// Synchronizes ground selection ring indicators with current selection state.
        private void UpdateSelectionCircles() =>
            activePartyMembers.ForEach(member => {
                if (member == null) return;
                if (_selection.Contains(member))
                    member.TurnSelectionCircleOn();
                else
                    member.TurnSelectionCircleOff();
            });

        /// Draws selection box marquee graphics.
        private void OnGUI() => _gestureHandler.DrawGUI(_currentScreenPos);
    }
}
