using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data{
    /// Global registry of all ProblemArchetypes, mapping archetype ID to its definition.
    [CreateAssetMenu(fileName = "ProblemArchetypeDatabase", menuName = "CRPG/Problem Archetype Database")]
    public class ProblemArchetypeDatabase : ScriptableObject{
        public static ProblemArchetypeDatabase Instance{ get; private set; }

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
        public ProblemArchetype Get(string id) => _registry[id];

#if UNITY_EDITOR
        /// Synchronizes archetype list from external spreadsheets or importers.
        public void UpdateDatabase(List<ProblemArchetype> newArchetypes){
            serializedArchetypes = new List<ProblemArchetype>(newArchetypes);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
