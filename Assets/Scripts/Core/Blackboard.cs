using System.Collections.Generic;
using UnityEngine;

namespace Core{
    /// Central database managing global, scene, and entity variables in memory.
    public class Blackboard : MonoBehaviour{
        /// Internal singleton instance routing static calls to active memory.
        private static Blackboard Instance{ get; set; }

        /// Active global variables that persist across scene loads.
        private Dictionary<string, float> _globalBoard = new();

        /// Active scene-specific variables mapped by scene name.
        private Dictionary<string, Dictionary<string, float>> _sceneBoards = new();

        /// Active entity-specific variables mapped by unique UUID.
        private Dictionary<string, Dictionary<string, float>> _entityBoards = new();

        private void Awake(){
            if (Instance == null){
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else{
                Destroy(gameObject);
            }
        }

        /// Gets the global variable board.
        public static Dictionary<string, float> Globals => Instance._globalBoard;

        /// Gets or creates the variable board for a specific scene.
        public static Dictionary<string, float> Scene(string name) => Instance.GetSceneBoardInternal(name);

        /// Gets or creates the variable board for a specific entity UUID.
        public static Dictionary<string, float> Entity(string id) => Instance.GetEntityBoardInternal(id);

        /// Safely retrieves or instantiates a scene-specific board partition.
        private Dictionary<string, float> GetSceneBoardInternal(string sceneName){
            if (!_sceneBoards.ContainsKey(sceneName))
                _sceneBoards[sceneName] = new Dictionary<string, float>();
            return _sceneBoards[sceneName];
        }

        /// Safely retrieves or instantiates an entity-specific board partition.
        private Dictionary<string, float> GetEntityBoardInternal(string entityId){
            if (!_entityBoards.ContainsKey(entityId))
                _entityBoards[entityId] = new Dictionary<string, float>();
            return _entityBoards[entityId];
        }

        /// Exports a deep-copy snapshot of all active boards for save serialization.
        public static SaveData ExportSavePackage(){
            return new SaveData{
                globalData = new Dictionary<string, float>(Instance._globalBoard),
                sceneData  = DeepCopyNested(Instance._sceneBoards),
                entityData = DeepCopyNested(Instance._entityBoards)
            };
        }

        /// Wipes active memory boards and populates them from a loaded save snapshot.
        public static void ImportSavePackage(SaveData savePackage){
            Instance._globalBoard  = new Dictionary<string, float>(savePackage.globalData);
            Instance._sceneBoards  = DeepCopyNested(savePackage.sceneData);
            Instance._entityBoards = DeepCopyNested(savePackage.entityData);
        }

        /// reates a full deep copy of a nested float dictionary.
        private static Dictionary<string, Dictionary<string, float>> DeepCopyNested(Dictionary<string, Dictionary<string, float>> source){
            Dictionary<string, Dictionary<string, float>> destination = new();
            foreach (KeyValuePair<string, Dictionary<string, float>> kvp in source)
                destination[kvp.Key] = new Dictionary<string, float>(kvp.Value);
            return destination;
        }
    }

    /// Extension methods providing typed accessors for raw string-to-float dictionaries.
    public static class BlackboardExtensions{
        /// Assigns or updates a boolean flag as a serialized float (true = 1.0f, false = 0.0f).
        public static void SetBool(this Dictionary<string, float> dict, string key, bool value) => dict[key] = value ? 1.0f : 0.0f;

        /// Assigns or updates an integer value in the dictionary.
        public static void SetInt(this Dictionary<string, float> dict, string key, int value) => dict[key] = value;

        /// Safely gets a float value, returning a default value if the key is missing.
        public static float Get(this Dictionary<string, float> dict, string key, float defaultValue = 0.0f)
            => dict.GetValueOrDefault(key, defaultValue);

        /// Safely gets a boolean flag, translating non-zero values as true.
        public static bool GetBool(this Dictionary<string, float> dict, string key, bool defaultValue = false)
            => dict.Get(key, defaultValue ? 1.0f : 0.0f) != 0.0f;

        /// Safely gets an integer value from the dictionary.
        public static int GetInt(this Dictionary<string, float> dict, string key, int defaultValue = 0)
            => (int)dict.Get(key, defaultValue);
    }
}
