using Actors.Brains;
using Core;
using Core.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace Actors.Player {
    [RequireComponent(typeof(UniqueId))]
    public class Character : MonoBehaviour, ISelectable {
        public CharacterMovement movement;
        public CharacterSheet sheet;
        public NavMeshAgent nmAgent;
        public Animator animator;
        public Transform selectionCircle;

        [Header("Physical Representation")]
        [Tooltip("The parent object holding body, items, colliders, and visuals. If null, all direct children are toggled.")]
        [SerializeField] private GameObject visualBody;

        private UniqueId _uniqueId;

        private void Awake() => _uniqueId = GetComponent<UniqueId>();

        public Sheet Sheet => sheet;
        public Transform SelectionCircle => selectionCircle;

        public void TurnSelectionCircleOn() {
            if (selectionCircle != null)
                selectionCircle.gameObject.SetActive(true);
        }

        public void TurnSelectionCircleOff() {
            if (selectionCircle != null)
                selectionCircle.gameObject.SetActive(false);
        }

        public void SetVisualsActive(bool active) {
            if (visualBody != null)
                visualBody.SetActive(active);
            else {
                for (int i = 0; i < transform.childCount; i++)
                    transform.GetChild(i).gameObject.SetActive(active);
            }

            if (nmAgent != null)
                nmAgent.enabled = active;

            if (movement != null)
                movement.enabled = active;
        }

        public void JoinParty() {
            GameSessionManager.Instance.SetCharacterActive(_uniqueId.Id, true);
            SetVisualsActive(true);
            PlayerBrain.AddPartyMember(this);
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
