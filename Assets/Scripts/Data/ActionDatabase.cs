using System.Collections.Generic;
using UnityEngine;

namespace Data{
    /// Global registry of all ActionDefinitions, mapping ActionType to its definition.
    [CreateAssetMenu(fileName = "ActionDatabase", menuName = "CRPG/Action Database")]
    public class ActionDatabase : ScriptableObject{
        public static ActionDatabase Instance{ get; private set; }

        [SerializeField] private List<ActionDefinition> serializedActions = new();

        private readonly Dictionary<ActionType, ActionDefinition> _registry = new();

        /// Caches singleton and builds fast lookup cache.
        private void OnEnable(){
            Instance = this;
            BuildDictionaryCache();
        }

        /// Populates dictionary cache for O(1) runtime queries.
        public void BuildDictionaryCache(){
            _registry.Clear();
            foreach (ActionDefinition action in serializedActions)
                _registry[action.actionType] = action;
        }

        /// Retrieves an ActionDefinition by ActionType.
        public ActionDefinition Get(ActionType type) => _registry[type];

#if UNITY_EDITOR
        /// Synchronizes action list from external spreadsheets or importers.
        public void UpdateDatabase(List<ActionDefinition> newActions){
            serializedActions = new List<ActionDefinition>(newActions);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
