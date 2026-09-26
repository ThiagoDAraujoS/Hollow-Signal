using System;
using System.Collections.Generic;
using Cameras;
using Narrative.Dialog;
using UI.Dialog;
using UI.Shared.Transitions;
using UnityEngine;
using World;
using World.Actors.Player;
using World.Interactables;
using World.Tactical;

namespace World.Actors.Brains{
    /// Central coordinator for party roster management, unit selection, and movement order routing.
    public class PlayerBrain : MonoBehaviour{
        private static PlayerBrain _instance;

        [SerializeField] private List<Character> activePartyMembers = new();
        [SerializeField] private DialogueController[] dialogueScreens = new DialogueController[4];
        [SerializeField] private GameObject sharedDialogueBackground;
        [SerializeField] private CanvasTransitionController dialogueTransition;

        private readonly PartySelection _selection = new();

        public static PlayerBrain Instance => _instance;
        public static PartySelection Selection => _instance._selection;
        public static Character Lead => _instance._selection.Lead;
        public static HashSet<Character> SelectedCharacters => _instance._selection.Selected;
        public static List<Character> ActivePartyMembers => _instance.activePartyMembers;

        public static Sheet CurrentInspectedSheet{ get; private set; }
        public static event Action<Sheet> OnSheetInspected;

        public static bool IsShiftPressed => PlayerGestureController.IsAppendPressed;
        public static bool IsAltPressed   => PlayerGestureController.IsAltPressed;

        /// Initializes singleton instance and registers selection listeners.
        private void Awake(){
            if (!_instance) _instance = this;
            else if (_instance != this){ Destroy(gameObject); return; }

            _selection.OnSelectionChanged += UpdateSelectionCircles;
            _selection.OnSelectionChanged += UpdateDialogueScreens;
            SyncDialogueScreens();
        }

        /// Cleans up selection event listeners and singleton instance.
        private void OnDestroy(){
            if (_instance != this) return;
            _selection.OnSelectionChanged -= UpdateSelectionCircles;
            _selection.OnSelectionChanged -= UpdateDialogueScreens;
            _instance = null;
        }

        /// Synchronizes dialogue screens on start.
        private void Start(){
            SyncDialogueScreens();
            UpdateDialogueScreens();
        }

        /// Subscribes to gesture controller events and dialogue notifications.
        private void OnEnable(){
            if (_instance != this) return;
            PlayerGestureController.OnSelectCharacter       += HandleSelectCharacter;
            PlayerGestureController.OnMarqueeSelect         += HandleMarqueeSelect;
            PlayerGestureController.OnDeselect              += HandleDeselect;
            PlayerGestureController.OnSelectAll             += HandleSelectAll;
            PlayerGestureController.OnCycleLeader           += HandleCycleLeader;
            PlayerGestureController.OnSelectSlot            += SelectSlot;
            PlayerGestureController.OnStop                  += StopSelectedUnits;
            PlayerGestureController.OnCommandInteractable   += HandleClickInteractable;
            PlayerGestureController.OnCommandCharacter      += HandleCommandCharacter;
            PlayerGestureController.OnCommandMove           += HandleMoveCommand;
            PlayerGestureController.OnContinuousCommandMove += HandleMoveCommand;
            DialogueController.OnDialogueActiveChanged      += HandleDialogueActiveChanged;

            UpdateSelectionCircles();
            UpdateDialogueScreens();
        }

        /// Unsubscribes from gesture controller events and dialogue notifications.
        private void OnDisable(){
            if (_instance != this) return;
            PlayerGestureController.OnSelectCharacter       -= HandleSelectCharacter;
            PlayerGestureController.OnMarqueeSelect         -= HandleMarqueeSelect;
            PlayerGestureController.OnDeselect              -= HandleDeselect;
            PlayerGestureController.OnSelectAll             -= HandleSelectAll;
            PlayerGestureController.OnCycleLeader           -= HandleCycleLeader;
            PlayerGestureController.OnSelectSlot            -= SelectSlot;
            PlayerGestureController.OnStop                  -= StopSelectedUnits;
            PlayerGestureController.OnCommandInteractable   -= HandleClickInteractable;
            PlayerGestureController.OnCommandCharacter      -= HandleCommandCharacter;
            PlayerGestureController.OnCommandMove           -= HandleMoveCommand;
            PlayerGestureController.OnContinuousCommandMove -= HandleMoveCommand;
            DialogueController.OnDialogueActiveChanged      -= HandleDialogueActiveChanged;
        }

        /// Handles single unit selection or lead promotion.
        private void HandleSelectCharacter(Character character, bool isAdditive){
            if (!activePartyMembers.Contains(character)) return;
            if (isAdditive) _selection.AddUnitSelect(character);
            else if (_selection.Contains(character) && _selection.Count > 1) _selection.SetLead(character);
            else _selection.SingleUnitSelect(character);
        }

        /// Handles marquee box selection of candidate party members.
        private void HandleMarqueeSelect(Vector2 startPos, Vector2 endPos, bool isAdditive){
            Camera cam = CameraStackCoordinator.ActiveBaseCamera ? CameraStackCoordinator.ActiveBaseCamera : Camera.main;
            List<Character> enclosed = SelectionScanner.GetCharactersInScreenRect(cam, startPos, endPos, activePartyMembers);
            if (enclosed.Count == 0) return;
            if (isAdditive) _selection.AdditiveBoxSelect(enclosed);
            else _selection.DragboxSelect(enclosed);
        }

        /// Handles right-click command on a character (selecting party member or interaction).
        private void HandleCommandCharacter(Character character, bool isAdditive){
            if (IsPartyMember(character)) HandleSelectCharacter(character, isAdditive);
        }

        /// Clears active selection if lead is not in active dialogue.
        private void HandleDeselect(){
            if (!(Lead && Lead.dialogueSession && Lead.dialogueSession.HasActiveDialogue)) _selection.Clear();
        }

        /// Selects all active party members.
        private void HandleSelectAll() => _selection.SelectAll(activePartyMembers);

        /// Cycles lead unit among active party members.
        private void HandleCycleLeader() => _selection.CycleLeader(activePartyMembers);

        /// Directs lead character to move to and use the interactable object.
        private void HandleClickInteractable(IUsable usable){
            Character lead = Lead;
            if (!lead) return;
            if (usable is DialogueBehaviour db && db.IsInUse && db.CurrentUser != lead) return;
            if (usable is AreaSlot slot){
                if (!slot.IsAvailable && slot.Occupant != lead && slot.ReservedBy != lead) return;
                if (slot.LinkedUsable is DialogueBehaviour slotDb && slotDb.IsInUse && slotDb.CurrentUser != lead) return;
            }
            lead.movement.MoveToAndUse(usable, lead.sheet);
        }

        /// Dispatches move order to selected units in formation.
        private void HandleMoveCommand(Vector3 destinationPoint){
            if (Lead && Lead.dialogueSession && Lead.dialogueSession.HasActiveDialogue) return;
            PlayerCommandDispatcher.MoveSelectedTo(destinationPoint, Lead, _selection.Selected);
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
            if (_instance._selection.Contains(character)) _instance._selection.ToggleAddSelection(character);
        }

        /// Disables player input and movement commands.
        public static void TurnControlsOff(){
            PlayerGestureController.Instance.SetControlsEnabled(false);
            InteractableHighlight.ClearHover();
        }

        /// Enables player input and movement commands.
        public static void TurnControlsOn() => PlayerGestureController.Instance.SetControlsEnabled(true);

        /// Inspects character or interactable sheet and fires inspection event.
        public static void Inspect(ISelectable selectable){
            CurrentInspectedSheet = selectable.Sheet;
            OnSheetInspected?.Invoke(CurrentInspectedSheet);
        }

        /// Commands all currently selected units to halt movement.
        public static void StopSelectedUnits() => PlayerCommandDispatcher.StopUnits(_instance._selection.Selected);

        /// Synchronizes screen index and controller references across active party members.
        public void SyncDialogueScreens(){
            if (dialogueScreens == null || dialogueScreens.Length == 0) dialogueScreens = new DialogueController[4];
            if (!dialogueScreens[0]){
                DialogueController[] controllers = FindObjectsByType<DialogueController>(FindObjectsInactive.Include);
                for (int i = 0; i < controllers.Length && i < dialogueScreens.Length; i++) dialogueScreens[i] = controllers[i];
            }
            if (!dialogueTransition) dialogueTransition = FindFirstObjectByType<CanvasTransitionController>(FindObjectsInactive.Include);
            if (!sharedDialogueBackground && dialogueScreens.Length > 0 && dialogueScreens[0] && dialogueScreens[0].DialogRoot)
                sharedDialogueBackground = dialogueScreens[0].DialogRoot;

            for (int i = 0; i < activePartyMembers.Count; i++){
                DialogueController controller = i < dialogueScreens.Length ? dialogueScreens[i] : null;
                if (activePartyMembers[i] && activePartyMembers[i].dialogueSession)
                    activePartyMembers[i].dialogueSession.BindScreen(i, controller);
            }
        }

        /// Synchronizes visibility of dialogue screens with the selected lead hero.
        public void UpdateDialogueScreens(){
            Character lead = Lead;
            bool hasActiveDialogue = lead && lead.dialogueSession && lead.dialogueSession.HasActiveDialogue;

            if (dialogueTransition){
                if (hasActiveDialogue) dialogueTransition.ShowHero(lead.dialogueSession.ScreenIndex);
                else dialogueTransition.Hide();
                return;
            }

            for (int i = 0; i < dialogueScreens.Length; i++){
                if (!dialogueScreens[i]) continue;
                bool shouldShow = hasActiveDialogue && (lead.dialogueSession.ScreenIndex == i || lead.dialogueSession.Controller == dialogueScreens[i]);
                dialogueScreens[i].SetVisible(shouldShow);
            }

            foreach (Character member in activePartyMembers){
                if (!member || !member.dialogueSession || !member.dialogueSession.Controller) continue;
                if (member != lead) member.dialogueSession.Controller.SetVisible(false);
            }

            if (sharedDialogueBackground && !hasActiveDialogue) sharedDialogueBackground.SetActive(false);
        }

        /// Resets gestures and halts units when dialogue state changes.
        private void HandleDialogueActiveChanged(bool isActive){
            UpdateDialogueScreens();
            if (!isActive) return;
            PlayerGestureController.Instance.ResetState();
            StopSelectedUnits();
            InteractableHighlight.ClearHover();
        }

        /// Selects the party member corresponding to the given zero-based roster slot index.
        private void SelectSlot(int slotIndex){
            if (slotIndex < 0 || slotIndex >= activePartyMembers.Count) return;
            Character target = activePartyMembers[slotIndex];
            if (PlayerGestureController.IsAppendPressed) _selection.AddUnitSelect(target);
            else _selection.SingleUnitSelect(target);
        }

        /// Refreshes selection indicator rings on all active party members.
        private void UpdateSelectionCircles(){
            foreach (Character c in activePartyMembers){
                if (!c) continue;
                if (_selection.Contains(c)) c.TurnSelectionCircleOn();
                else c.TurnSelectionCircleOff();
            }
        }
    }
}
