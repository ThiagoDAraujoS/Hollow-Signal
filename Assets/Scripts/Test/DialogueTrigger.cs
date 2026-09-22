using Narrative.Dialog;
using UI.Dialog;
using UnityEngine;
using World;
using World.Actors.Player;
using World.Tactical;

namespace Test{
    /// Triggers dialogue interaction when clicked via PlayerBrain raycast or character interaction.
    [DisallowMultipleComponent]
    public class DialogueTrigger : MonoBehaviour, IUsable{
        [Header("Dialogue Setup")]
        [SerializeField] private DialogueBehaviour dialogueBehaviour;
        [SerializeField] private string startingKnot = "Main";

        [Header("Interaction Pose (Widget)")]
        [SerializeField] private SpatialPose anchorPose = SpatialPose.Default;

        public Vector3    UsePosition => anchorPose.GetWorldPosition(transform);
        public Quaternion UseRotation => anchorPose.GetWorldRotation(transform);

        /// Resolves DialogueBehaviour reference on reset.
        private void Reset() => dialogueBehaviour = GetComponent<DialogueBehaviour>();

        /// Executes dialogue interaction on this object.
        public void Use(CharacterSheet whosUsing) => StartDialogue();

        /// Starts dialogue session on the dialogue controller.
        public void StartDialogue() => DialogueController.StartDialogue(dialogueBehaviour, startingKnot);

        /// Triggers dialogue test directly when clicking this object in the scene.
        private void OnMouseDown() => StartDialogue();
    }
}
