using System;
using UI.Dialog;
using UnityEngine;
using World.Actors.Brains;
using World.Actors.Player;

namespace Narrative.Dialog{
    /// Manages active dialogue graph progression, knot state, and crisis turn suspension for a character.
    [DisallowMultipleComponent]
    public class CharacterDialogueSession : MonoBehaviour{
        private Character _character;

        public Character          Character          => _character ??= GetComponent<Character>();
        public DialogueBehaviour  CurrentDialogue    { get; private set; }
        public string             CurrentKnotId      { get; private set; }
        public bool               IsSuspendedInCrisis{ get; private set; }
        public int                ScreenIndex        { get; private set; } = -1;
        public DialogueController Controller         { get; private set; }

        public bool HasActiveDialogue => CurrentDialogue != null && !string.IsNullOrEmpty(CurrentKnotId);

        public event Action<DialogueBehaviour, string> OnKnotChanged;
        public event Action                            OnDialogueEnded;

        /// Binds roster screen index and corresponding dialogue controller reference.
        public void BindScreen(int index, DialogueController controller){
            ScreenIndex = index;
            Controller  = controller;
        }

        /// Starts dialogue session using assigned screen controller.
        public void StartDialogue(DialogueBehaviour dialogue, string startingKnot = "Main"){
            Character character = Character;
            if (dialogue.IsInUse && dialogue.CurrentUser != character)
                return;

            CurrentDialogue = dialogue;
            CurrentKnotId   = startingKnot;

            if (Controller == null)
                PlayerBrain.Instance.SyncDialogueScreens();

            Controller.BeginDialogue(dialogue, startingKnot, this);
        }

        /// Updates active dialogue and knot tracking.
        public void SetKnot(DialogueBehaviour dialogue, string knotId){
            CurrentDialogue = dialogue;
            CurrentKnotId   = knotId;
            OnKnotChanged?.Invoke(dialogue, knotId);
        }

        /// Clears active dialogue and knot tracking upon dialogue completion.
        public void Clear(){
            if (CurrentDialogue != null)
                CurrentDialogue.ReleaseUser(Character);
            CurrentDialogue     = null;
            CurrentKnotId       = null;
            IsSuspendedInCrisis = false;
            OnDialogueEnded?.Invoke();
        }

        /// Suspends conversation progress until the next crisis round.
        public void SuspendCrisis() => IsSuspendedInCrisis = true;

        /// Resumes conversation progress for the current crisis round.
        public void ResumeCrisis() => IsSuspendedInCrisis = false;
    }
}
