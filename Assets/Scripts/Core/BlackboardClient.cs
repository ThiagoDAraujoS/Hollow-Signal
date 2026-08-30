using System.Collections.Generic;
using UnityEngine;

namespace Core{
    /// <summary>
    /// Contract for game components requiring state persistence.
    /// Passes raw dictionary partitions that utilize <see cref="BlackboardExtensions"/>.
    /// </summary>
    public interface IBoundState{
        void OnLoadState(Dictionary<string, float> state);
        void OnSaveState(Dictionary<string, float> state);
    }

    /// <summary>
    /// A lifecycle mediator that automatically binds local game components to the global database.
    /// It grabs the entity's <see cref="UniqueId"/>, locates all local scripts implementing 
    /// <see cref="IBoundState"/>, and coordinates their loading and saving hooks.
    /// </summary>
    [RequireComponent(typeof(UniqueId))]
    public class BlackboardClient : MonoBehaviour{
        private string        _entityId;
        private IBoundState[] _boundStates;

        private void Awake(){
            _entityId    = GetComponent<UniqueId>().Id;
            _boundStates = GetComponents<IBoundState>();
        }

        private void Start(){
            Dictionary<string, float> state = Blackboard.Entity(_entityId);
            foreach (IBoundState bound in _boundStates)
                bound.OnLoadState(state);
        }

        private void OnDestroy(){
            Dictionary<string, float> state = Blackboard.Entity(_entityId);
            foreach (IBoundState bound in _boundStates)
                bound.OnSaveState(state);
        }
    }
}