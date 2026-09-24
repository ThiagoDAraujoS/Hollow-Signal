using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data{
    /// Global registry of all ProblemArchetypes, mapping archetype ID to its definition.
    [CreateAssetMenu(fileName = "ProblemArchetypeDatabase", menuName = "CRPG/Problem Archetype Database")]
    public class ProblemArchetypeDatabase : ScriptableObject{
        private static ProblemArchetypeDatabase _instance;

        public static ProblemArchetypeDatabase Instance{
            get{
                if (_instance == null){
                    _instance = Resources.Load<ProblemArchetypeDatabase>("ProblemArchetypeDatabase");

#if UNITY_EDITOR
                    if (_instance == null){
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ProblemArchetypeDatabase");
                        if (guids.Length > 0){
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ProblemArchetypeDatabase>(path);
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

        [SerializeField] private List<ProblemArchetype> serializedArchetypes = new();

        private readonly Dictionary<string, ProblemArchetype> _registry = new(StringComparer.OrdinalIgnoreCase);

        /// Caches singleton and builds fast lookup cache.
        private void OnEnable(){
            Instance = this;
            BuildDictionaryCache();
        }

        /// Populates dictionary cache for O(1) runtime queries.
        public void BuildDictionaryCache(){
            _registry.Clear();
            foreach (ProblemArchetype archetype in serializedArchetypes)
                _registry[archetype.archetypeId] = archetype;
        }

        /// Retrieves a ProblemArchetype definition by unique string ID.
        public ProblemArchetype Get(string id){
            if (_registry.Count == 0 && serializedArchetypes.Count > 0)
                BuildDictionaryCache();

            return _registry.TryGetValue(id, out ProblemArchetype archetype) ? archetype : null;
        }

#if UNITY_EDITOR
        /// Synchronizes archetype list from external spreadsheets or importers.
        public void UpdateDatabase(List<ProblemArchetype> newArchetypes){
            serializedArchetypes = new List<ProblemArchetype>(newArchetypes);
            BuildDictionaryCache();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
