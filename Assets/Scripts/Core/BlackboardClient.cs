using Core.Managers;
using UnityEngine;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Core {
    /// <summary>
    /// Contract for game components requiring state persistence.
    /// Passes raw dictionary partitions that utilize Blackboard variables.
    /// </summary>
    public interface IBoundState {
        void OnLoadState(Partition state);
        void OnSaveState(Partition state);
    }

    /// <summary>
    /// A lifecycle mediator that automatically binds local game components to the unified Blackboard database.
    /// It grabs the entity's UniqueId, locates all local scripts implementing IBoundState,
    /// and coordinates their loading and saving hooks into the unified board.
    /// 
    /// It also acts as an on-demand proxy, allowing components and other systems to directly
    /// query active data partitions for this entity.
    /// </summary>
    [RequireComponent(typeof(UniqueId))]
    public class BlackboardClient : MonoBehaviour {
        private IBoundState[] _boundStates;

        public string EntityId { get; private set; }

        private void Awake() {
            EntityId     = GetComponent<UniqueId>().Id;
            _boundStates = GetComponents<IBoundState>();
        }

        private void Start() => LoadStateFromBlackboard();

        private void OnDestroy() => FlushStateToBlackboard();

        /// <summary>
        /// Flushes all local IBoundState components directly into this entity's unique blackboard partition.
        /// </summary>
        public void FlushStateToBlackboard() {
            Partition state = GetPartition();
            foreach (IBoundState boundState in _boundStates) {
                boundState?.OnSaveState(state);
            }
        }

        /// <summary>
        /// Restores all local IBoundState components directly from this entity's unique blackboard partition.
        /// </summary>
        public void LoadStateFromBlackboard() {
            Partition state = GetPartition();
            foreach (IBoundState bound in _boundStates) {
                bound?.OnLoadState(state);
            }
        }
        
        /// <summary>
        /// Shortcut accessor to get this entity's specific partition on the single, unified board.
        /// Automatically generates a clean, empty partition if it does not exist yet.
        /// </summary>
        public Partition GetPartition() {
            return SaveSystem.BlackBoard.GetOrCreatePartition(EntityId);
        }
    }
}
