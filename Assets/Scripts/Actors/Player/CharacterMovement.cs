using Core;
using UnityEngine;
using UnityEngine.AI;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Actors.Player {
    /// Manages entity movement using Unity NavMeshAgent and automatically persists
    /// the character's physical world position into the Blackboard, while updating Animator parameters.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class CharacterMovement : TrackedBehaviour {
        /// Tracked coordinate values for seamless Blackboard persistence
        public Tracked<float> posX = new("pos_x", 0f);
        public Tracked<float> posY = new("pos_y", 0f);
        public Tracked<float> posZ = new("pos_z", 0f);

        private NavMeshAgent _agent;
        private Animator _animator;
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        /// Cached component initialization
        protected override void OnAwake() {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }

        private void Update() {
            // Update the Animator's speed parameter based on the agent's velocity.
            _animator.SetFloat(SpeedParam, _agent.velocity.magnitude);
        }

        /// Direct navigation command to move towards a specific coordinate
        public void MoveTo(Vector3 destination) {
            _agent.destination = destination;
        }

        /// Instantly warps the character and NavMeshAgent to a specific coordinate
        public void WarpTo(Vector3 position) {
            _agent.Warp(position);
        }
    }
}
