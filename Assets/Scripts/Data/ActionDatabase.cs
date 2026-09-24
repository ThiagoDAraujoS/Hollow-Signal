using System.Collections.Generic;
using UnityEngine;

namespace Data{
    /// Global registry of all ActionDefinitions, mapping ActionType to its definition.
    [CreateAssetMenu(fileName = "ActionDatabase", menuName = "CRPG/Action Database")]
    public class ActionDatabase : ScriptableObject{
        private static ActionDatabase _instance;

        public static ActionDatabase Instance{
            get{
                if (_instance == null){
                    _instance = Resources.Load<ActionDatabase>("ActionDatabase");

#if UNITY_EDITOR
                    if (_instance == null){
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ActionDatabase");
                        if (guids.Length > 0){
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ActionDatabase>(path);
                        }
                    }
#endif
                    if (_instance != null)
                        _instance.BuildDictionaryCache();
                }
                return _instance;
            }
            private set => _instance = value;
        }

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
        public ActionDefinition Get(ActionType type){
            if (_registry.Count == 0 && serializedActions.Count > 0)
                BuildDictionaryCache();

            return _registry.TryGetValue(type, out ActionDefinition def) ? def : null;
        }

#if UNITY_EDITOR
        /// Synchronizes action list from external spreadsheets or importers.
        public void UpdateDatabase(List<ActionDefinition> newActions){
            serializedActions = new List<ActionDefinition>(newActions);
            BuildDictionaryCache();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
