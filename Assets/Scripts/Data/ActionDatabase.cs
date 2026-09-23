using System;
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

        /// Retrieves an ActionDefinition by ActionType, falling back to default synthesized definition if missing.
        public ActionDefinition Get(ActionType type){
            if (_registry.TryGetValue(type, out ActionDefinition def))
                return def;

            return new ActionDefinition{
                actionType = type,
                displayName = type.ToString(),
                perkRequirement = PerkRequirementMode.ShownWhenLocked,
                applicableSkills = new(){ Skill.None }
            };
        }

#if UNITY_EDITOR
        /// Synchronizes action list from external spreadsheets or importers.
        public void UpdateDatabase(List<ActionDefinition> newActions){
            serializedActions = new List<ActionDefinition>(newActions);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
