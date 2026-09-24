using Core.Managers;
using Core.State;
using Narrative.Dialog;
using UnityEngine;
using UnityEngine.AI;
using World.Actors.Brains;
using World.Tactical;

namespace World.Actors.Player{
    [RequireComponent(typeof(UniqueId))]
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(CharacterDialogueSession))]
    public class Character : MonoBehaviour, ISelectable{
        [HideInInspector] public CharacterMovement       movement;
        [HideInInspector] public CharacterSheet          sheet;
        [HideInInspector] public CharacterDialogueSession dialogueSession;
        [HideInInspector] public NavMeshAgent            nmAgent;
        [HideInInspector] public Animator                animator;

        [Header("Hero Identity")]
        [SerializeField] public HeroEnum heroType;

        [Header("Selection & Visuals")] public Transform selectionCircle;

        [Tooltip("The parent object holding body, items, colliders, and visuals.")] [SerializeField]
        public GameObject body;

        private UniqueId _uniqueId;

        public AreaSlot CurrentSlot{ get; set; }

        /// Caches required component references across awake and edit-time calls.
        public void EnsureInitialized(){
            _uniqueId       ??= GetComponent<UniqueId>();
            movement        ??= GetComponent<CharacterMovement>();
            sheet           ??= GetComponent<CharacterSheet>();
            dialogueSession ??= GetComponent<CharacterDialogueSession>();
            nmAgent         ??= GetComponentInChildren<NavMeshAgent>(true);
            animator        ??= GetComponentInChildren<Animator>(true);
        }

        /// Caches references and disables default standalone movement.
        private void Awake(){
            EnsureInitialized();
            movement.enabled = false;
        }

        /// Re-links component references upon reset.
        private void Reset() => EnsureInitialized();

        public Sheet      Sheet           => sheet;
        public Transform  SelectionCircle => selectionCircle;
        public Transform  BodyTransform   => body.transform;
        public Vector3    WorldPosition   => BodyTransform.position;
        public Quaternion WorldRotation   => BodyTransform.rotation;

        /// Activates the ground selection circle visual indicator.
        public void TurnSelectionCircleOn()  => selectionCircle.gameObject.SetActive(true);

        /// Deactivates the ground selection circle visual indicator.
        public void TurnSelectionCircleOff() => selectionCircle.gameObject.SetActive(false);

        /// Vacates and releases any currently occupied area slot.
        public void LeaveSlot(){
            if (CurrentSlot == null) return;
            if (CurrentSlot is TacticalSlot tactical)
                tactical.Vacate(this);
            else
                CurrentSlot.Release();
            CurrentSlot = null;
        }

        /// Toggles visual body active state and disables movement components if inactive.
        public void SetVisualsActive(bool active){
            body.SetActive(active);

            if (active) return;
            movement.enabled = false;
            nmAgent.enabled  = false;
        }

        /// Registers character with the session party, activates visuals, and warps to spawn.
        public void JoinParty(){
            EnsureInitialized();
            GameSessionManager.Instance.SetCharacterActive(_uniqueId.Id, true);
            SetVisualsActive(true);
            PlayerBrain.AddPartyMember(this);

            nmAgent.enabled = true;
            nmAgent.Warp(GameSessionManager.CurrentMapManager.DefaultSpawnPoint.position);
            movement.enabled = true;
        }

        /// Deregisters character from session party, deselects, and deactivates visuals.
        public void LeaveParty(){
            LeaveSlot();
            EnsureInitialized();
            GameSessionManager.Instance.SetCharacterActive(_uniqueId.Id, false);
            PlayerBrain.RemovePartyMember(this);
            PlayerBrain.Deselect(this);
            TurnSelectionCircleOff();
            SetVisualsActive(false);
        }
    }
}
