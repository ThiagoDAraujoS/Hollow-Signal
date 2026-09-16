using Narrative.Dialog;
using UI.Dialog;
using UnityEngine;
using World;
using World.Actors.Player;

namespace Test{
    /// Triggers dialogue interaction when clicked via PlayerBrain raycast or character interaction.
    [DisallowMultipleComponent]
    public class DialogueTrigger : MonoBehaviour, IUsable{
        [Header("Dialogue Setup")]
        [SerializeField] private DialogueBehaviour dialogueBehaviour;
        [SerializeField] private string startingKnot = "Main";

        [Header("Interaction Spot (Optional)")]
        [SerializeField] private Transform useSpot;

        public Transform UseSpot => useSpot != null ? useSpot : transform;
        public Quaternion UseRotation => useSpot != null ? useSpot.rotation : transform.rotation;

        private void Reset() => dialogueBehaviour = GetComponent<DialogueBehaviour>();

        /// Executes dialogue interaction on this object.
        public void Use(CharacterSheet whosUsing) => StartDialogue();

        /// Starts dialogue session on the dialogue controller.
        public void StartDialogue(){
            DialogueBehaviour dialogue = dialogueBehaviour != null ? dialogueBehaviour : GetComponent<DialogueBehaviour>();
            DialogueController.StartDialogue(dialogue, startingKnot);
        }

        /// Triggers dialogue test directly when clicking this object in the scene.
        private void OnMouseDown() => StartDialogue();
    }
}
