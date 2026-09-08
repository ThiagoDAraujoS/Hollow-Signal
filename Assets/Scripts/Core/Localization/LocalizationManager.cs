using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.Localization{
    /// Central manager organizing localized strings into isolated, named file tables (file -> key -> text).
    public class LocalizationManager : MonoBehaviour{
        public static event Action OnLanguageChanged;

        private static LocalizationManager _instance;

        public static string CurrentLanguage => _instance._currentLanguage;

        private const string DefaultLanguage = "en";

        private readonly Dictionary<string, Dictionary<string, string>> _tables = new(StringComparer.OrdinalIgnoreCase);

        private string _currentLanguage = DefaultLanguage;

        private void Awake(){
            if (_instance == null){
                _instance = this;
                LoadBaseStrings(_currentLanguage);
            }
            else if (_instance != this){
                Destroy(gameObject);
            }
        }

        /// Loads persistent core localization files (system, masteries, items) into memory.
        public static void LoadBaseStrings(string language){
            _instance._currentLanguage = language;
            LoadTable("system");
            OnLanguageChanged?.Invoke();
        }

        /// Loads a named localization table from StreamingAssets/Localization into memory.
        public static void LoadTable(string tableName){
            string folder = Path.Combine(Application.streamingAssetsPath, "Localization");
            string fileName = $"{tableName.ToLowerInvariant()}_{_instance._currentLanguage}.txt";

            string filePath = Path.Combine(folder, fileName);
            if (!File.Exists(filePath))
                filePath = Path.Combine(folder, "Scenes", fileName);

            if (!_instance._tables.TryGetValue(tableName, out Dictionary<string, string> tableDict)){
                tableDict = new Dictionary<string, string>(StringComparer.Ordinal);
                _instance._tables[tableName] = tableDict;
            }
            else
                tableDict.Clear();

            ParseFileToDictionary(filePath, tableDict);
        }

        /// Unloads a named localization table, releasing its strings from memory.
        public static void UnloadTable(string tableName) => _instance._tables.Remove(tableName);

        /// Checks if a specific table is currently resident in memory.
        public static bool IsTableLoaded(string tableName) => _instance._tables.ContainsKey(tableName);

        /// Sets a new active language and reloads all currently resident tables.
        public static void SetLanguage(string language){
            if (_instance._currentLanguage == language) return;

            _instance._currentLanguage = language;

            List<string> activeTableNames = new(_instance._tables.Keys);
            _instance._tables.Clear();

            foreach (string tableName in activeTableNames)
                LoadTable(tableName);

            OnLanguageChanged?.Invoke();
        }

        /// Retrieves a localized string scoped directly to a specific file table.
        public static string Get(string tableName, string key, params object[] args){
            if (_instance._tables.TryGetValue(tableName, out Dictionary<string, string> tableDict) && tableDict.TryGetValue(key, out string val))
                return args is { Length: > 0 } ? string.Format(val, args) : val;

            return key;
        }

        private static void ParseFileToDictionary(string path, Dictionary<string, string> targetDict){
            using StreamReader reader = new(path);
            while (reader.ReadLine() is { } line){
                line = line.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                int splitIdx = line.IndexOf('=');
                if (splitIdx == -1) continue;

                string key = line[..splitIdx].Trim();
                string val = line[(splitIdx + 1)..].Trim();

                if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
                    val = val[1..^1];

                val = val.Replace("\\n", "\n").Replace("\\\"", "\"");
                targetDict[key] = val;
            }
        }
    }
}
