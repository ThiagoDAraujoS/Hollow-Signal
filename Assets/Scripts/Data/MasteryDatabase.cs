using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data{
    /// A high-performance, global registry for all Mastery ScriptableObjects in the game.
    /// Utilizes Unity's modern "Preloaded Assets" feature to establish a scene-agnostic Singleton Instance
    /// at game boot without legacy Resources folders, complex file IO, or scene reference dragging.
    [CreateAssetMenu(fileName = "MasteryDatabase", menuName = "CRPG/Mastery Database")]
    public class MasteryDatabase : ScriptableObject{
        /// Global read-only static accessor. Assigned automatically at boot when the asset is preloaded.
        public static MasteryDatabase Instance{ get; private set; }

        [Header("Mastery Registry")] 
        [Tooltip("The backing list of all generated Mastery ScriptableObjects. Hidden in inspector, populated automatically by the Google Sheets importer.")] 
        [SerializeField]
        private List<Mastery> serializedMasteries = new();

        // High-performance pure C# dictionary lookup cache
        private readonly Dictionary<string, Mastery> _registry = new(StringComparer.OrdinalIgnoreCase);

        private void OnEnable(){
            Instance = this;
            BuildDictionaryCache();
        }
        
        /// Clears the runtime registry and builds a fast O(1) string-lookup dictionary from the serialized list.
        public void BuildDictionaryCache(){
            _registry.Clear();
            foreach (Mastery mastery in serializedMasteries){
                if (mastery == null){
                    Debug.LogError("[MasteryDatabase] Found a NULL Mastery reference in the database! Ensure all ScriptableObjects exist and are compiled.");
                    continue;
                }
                _registry[mastery.Id] = mastery;
            }
        }
        
        /// Instantly retrieves a Mastery asset by its unique string ID.
        /// O(1) lookup complexity. Returns null if not found.
        public static Mastery Get(string id) => Instance._registry.GetValueOrDefault(id);

#if UNITY_EDITOR
        /// Direct, type-safe hook for your Google Sheets Importer Editor Tool to inject generated Masteries.
        /// Saves to disk and marks the asset dirty automatically.
        public void UpdateDatabase(List<Mastery> newMasteries){
            if (newMasteries == null) return;
            serializedMasteries = new List<Mastery>(newMasteries);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[MasteryDatabase] Successfully synchronized {serializedMasteries.Count} Masteries from Importer Tool.");
        }
#endif
    }
}
