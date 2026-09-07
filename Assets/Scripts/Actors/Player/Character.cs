using Actors.Brains;
using Core;
using Core.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace Actors.Player {
    [RequireComponent(typeof(UniqueId))]
    [RequireComponent(typeof(CharacterMovement))]
    public class Character : MonoBehaviour, ISelectable {
        [HideInInspector] public CharacterMovement movement;
        [HideInInspector] public CharacterSheet sheet;
        [HideInInspector] public NavMeshAgent nmAgent;
        [HideInInspector] public Animator animator;

        [Header("Selection & Visuals")]
        public Transform selectionCircle;
        
        [Tooltip("The parent object holding body, items, colliders, and visuals.")]
        [SerializeField] public GameObject body;

        private UniqueId _uniqueId;

        private void Awake() {
            _uniqueId = GetComponent<UniqueId>();
            if (movement == null) movement = GetComponent<CharacterMovement>();
            if (sheet == null) sheet = GetComponent<CharacterSheet>();
            if (nmAgent == null) nmAgent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Reset() {
            movement = GetComponent<CharacterMovement>();
            sheet = GetComponent<CharacterSheet>();
            nmAgent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
        }

        public Sheet Sheet => sheet;
        public Transform SelectionCircle => selectionCircle;
        public Transform BodyTransform => body.transform;
        public Vector3 WorldPosition => BodyTransform.position;
        public Quaternion WorldRotation => BodyTransform.rotation;

        public void TurnSelectionCircleOn() => selectionCircle.gameObject.SetActive(true);
        public void TurnSelectionCircleOff() => selectionCircle.gameObject.SetActive(false);

        public void SetVisualsActive(bool active) {
            body.SetActive(active);

            if (!active) {
                movement.enabled = false;
                nmAgent.enabled = false;
            }
        }

        public void JoinParty() {
            GameSessionManager.Instance.SetCharacterActive(_uniqueId.Id, true);
            SetVisualsActive(true);
            PlayerBrain.AddPartyMember(this);

            nmAgent.enabled = true;
            nmAgent.Warp(GameSessionManager.CurrentMapManager.DefaultSpawnPoint.position);
            movement.enabled = true;
        }

        public void LeaveParty() {
            GameSessionManager.Instance.SetCharacterActive(_uniqueId.Id, false);
            PlayerBrain.RemovePartyMember(this);
            PlayerBrain.Deselect(this);
            TurnSelectionCircleOff();
            SetVisualsActive(false);
        }
    }
}
