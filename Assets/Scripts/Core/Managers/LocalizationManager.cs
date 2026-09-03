using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.Managers{
    /// MonoBehaviour-backed localization manager supporting plaintext file parsing and a clean static facade.
    public class LocalizationManager : MonoBehaviour{
        public static event Action OnLanguageChanged;

        private static LocalizationManager _instance;

        public static string CurrentLanguage => _instance?._currentLanguage ?? "en";

        private const string DefaultLanguage = "en";

        private readonly Dictionary<string, string> _baseDatabase = new();
        
        private readonly Dictionary<string, string> _sceneDatabase = new();
        
        private string _currentLanguage = DefaultLanguage;
        
        private string _currentSceneName = string.Empty;

        private void Awake(){
            if (_instance == null){
                _instance = this;
                LoadBaseStrings(_currentLanguage);
            }
            else if (_instance != this)
                Destroy(gameObject);
        }

        /// Loads the persistent base system and items localization files for a given language.
        public static void LoadBaseStrings(string language){
            if (_instance == null) return;
                
            _instance._currentLanguage = language;
            _instance._baseDatabase.Clear();

            string folder = Path.Combine(Application.streamingAssetsPath, "Localization");
            
            string systemPath = Path.Combine(folder, $"system_{language}.txt");
            ParseFileToDictionary(systemPath, _instance._baseDatabase);
            
            string masteriesPath = Path.Combine(folder, $"masteries_{language}.txt");
            ParseFileToDictionary(masteriesPath, _instance._baseDatabase);

            string itemsPath = Path.Combine(folder, $"items_{language}.txt");
            ParseFileToDictionary(itemsPath, _instance._baseDatabase);

            OnLanguageChanged?.Invoke();
        }
        
        /// Loads local dialogue and quest strings for a specific scene.
        public static void LoadSceneStrings(string sceneName){
            _instance._currentSceneName = sceneName;
            _instance._sceneDatabase.Clear();

            string path = Path.Combine(Application.streamingAssetsPath, "Localization", "Scenes", $"{sceneName}_{_instance._currentLanguage}.txt");
            ParseFileToDictionary(path, _instance._sceneDatabase);
        }

        public static void UnloadSceneStrings(){
            _instance._currentSceneName = string.Empty;
            _instance. _sceneDatabase.Clear();
        }
        
        /// Sets a new active language, re-loads all current files, and triggers UI updates.
        public static void SetLanguage(string language){
            if (_instance._currentLanguage == language) return;

            _instance._currentLanguage = language;
            LoadBaseStrings(language);

            if (!string.IsNullOrEmpty(_instance._currentSceneName))
                LoadSceneStrings(_instance._currentSceneName);
        }
        
        /// Retrieves a localized string by key and formats it if arguments are provided.
        public static string Get(string key, params object[] args){
            if (_instance._sceneDatabase.TryGetValue(key, out string result))
                return FormatString(result, args);
            
            if (_instance._baseDatabase.TryGetValue(key, out result))
                return FormatString(result, args);
            
            return key;
        }

        private static string FormatString(string localizedText, params object[] args){
            if (args == null || args.Length == 0) return localizedText;

            try{
                return string.Format(localizedText, args);
            }
            catch (FormatException){
                Debug.LogWarning($"[Localization] Format exception on key check. Text: '{localizedText}'");
                return localizedText;
            }
        }

        private static void ParseFileToDictionary(string path, Dictionary<string, string> targetDict){
            if (!File.Exists(path)){
                Debug.LogWarning($"[Localization] File not found: {path}");
                return;
            }

            try{
                using StreamReader reader = new StreamReader(path);
                while (reader.ReadLine() is{ } line){
                    // Trim leading/trailing whitespaces
                    line = line.Trim();

                    // Skip empty lines or comments
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;
                    
                    // Find the first occurrence of '=' to split key and value
                    int splitIdx = line.IndexOf('=');
                    if (splitIdx == -1) continue; // Invalid format line

                    string key = line[..splitIdx].Trim();
                    string val = line[(splitIdx + 1)..].Trim();

                    // Handle C# style escape sequence for newline
                    val = val.Replace("\\n", "\n");

                    targetDict[key] = val;
                }
            }
            catch (Exception e){
                Debug.LogError($"[Localization] Error parsing localization file {path}: {e.Message}");
            }
        }
    }
}
