using Core;
using UnityEngine;
using UnityEngine.AI;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Actors.Player {
    /// Manages entity movement using Unity NavMeshAgent and automatically persists
    /// the character's physical world position into the Blackboard.
    [RequireComponent(typeof(NavMeshAgent))]
    public class CharacterMovement : TrackedBehaviour {
        /// Tracked coordinate values for seamless Blackboard persistence
        public Tracked<float> posX = new("pos_x", 0f);
        public Tracked<float> posY = new("pos_y", 0f);
        public Tracked<float> posZ = new("pos_z", 0f);

        private NavMeshAgent _agent;

        /// Cached NavMeshAgent initialization
        protected override void OnAwake() {
            _agent = GetComponent<NavMeshAgent>();
        }

        /// Direct navigation command to move towards a specific coordinate
        public void MoveTo(Vector3 destination) {
            _agent.destination = destination;
        }

        /// Instantly warps the character and NavMeshAgent to a specific coordinate
        public void WarpTo(Vector3 position) {
            _agent.Warp(position);
        }

        /// Captures the current transform coordinates before serializing state
        public override void OnSaveState(Partition state) {
            posX.Value = transform.position.x;
            posY.Value = transform.position.y;
            posZ.Value = transform.position.z;
            base.OnSaveState(state);
        }

        /// Restores coordinate values from the Blackboard and instantly warps the agent
        public override void OnLoadState(Partition state) {
            base.OnLoadState(state);
            Vector3 loadedPosition = new Vector3(posX, posY, posZ);
            _agent.Warp(loadedPosition);
        }
    }
}
