using Core.Managers;
using System.Collections.Generic;
using UnityEngine;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Core {
    /// Contract for game components requiring state persistence.
    /// Passes raw dictionary partitions that utilize Blackboard variables.
    public interface IBoundState {
        void OnLoadState(Partition state);
        void OnSaveState(Partition state);
    }
    
    /// A lifecycle mediator that automatically binds local game components to the unified Blackboard database.
    /// It grabs the entity's UniqueId, locates all local scripts implementing IBoundState,
    /// and coordinates their loading and saving hooks into the unified board.
    /// It also acts as an on-demand proxy, allowing components and other systems to directly
    /// query active data partitions for this entity.
    [RequireComponent(typeof(UniqueId))]
    public class BlackboardClient : MonoBehaviour {
        public static readonly HashSet<BlackboardClient> ActiveClients = new();

        private IBoundState[] _boundStates;
        
        public string fileName = string.Empty;  

        public string EntityId { get; private set; }

        private void Awake() {
            EntityId     = GetComponent<UniqueId>().Id;
            _boundStates = GetComponents<IBoundState>();
        }

        private void OnEnable() {
            ActiveClients.Add(this);
            LoadStateFromBlackboard();
        }

        private void OnDisable() {
            ActiveClients.Remove(this);
            FlushStateToBlackboard();
        }
        
        /// Flushes all local IBoundState components directly into this entity's unique blackboard partition.
        public void FlushStateToBlackboard() {
            Partition state = GetPartition();
            foreach (IBoundState boundState in _boundStates)
                boundState?.OnSaveState(state);
        }
        
        /// Restores all local IBoundState components directly from this entity's unique blackboard partition.
        public void LoadStateFromBlackboard() {
            Partition state = GetPartition();
            foreach (IBoundState bound in _boundStates) 
                bound?.OnLoadState(state);
        }
        
        /// Shortcut accessor to get this entity's specific partition on the single, unified board.
        /// Automatically generates a clean, empty partition if it does not exist yet.
        public Partition GetPartition() => SaveSystem.Blackboard.GetPartition(fileName, EntityId);
    }
}