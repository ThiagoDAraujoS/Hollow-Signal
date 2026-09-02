using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core{
    /// Pure data transfer object representing save file details for UI binding.
    public class SaveFileMetadata{
        public string   displayName;
        public string   saveName;
        public DateTime lastWriteTime;
        public bool     isAutosave;
    }

    /// Handles physical disk operations using slot or string-based JSON file serialization.
    public class SaveSystem : MonoBehaviour{
        private BlackBoard _blackBoard;

        public string saveFileName = "savegame_01";
        public string loadFileName = "savegame_01";

        public static BlackBoard BlackBoard  => Instance._blackBoard;
        public static SaveSystem Instance     { get; private set; }

        private static string _saveDirectory;

        public static string GetBoardPath(string baseName) => Path.Combine(_saveDirectory, $"{baseName}.json");

        public void Awake(){
            if (Instance != null && Instance != this){
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _saveDirectory = Application.persistentDataPath;
            DontDestroyOnLoad(gameObject);
            _blackBoard  = gameObject.GetComponent<BlackBoard>();
        }

        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }

        /// Saves the game under a specified file name.
        /// Flushes all active client states before writing.
        public static void SaveGame(string fileName, Action<string> onFailure = null){
            BlackboardClient[] activeClients = FindObjectsByType<BlackboardClient>();
            foreach (BlackboardClient client in activeClients){
                if (client != null)
                    client.FlushStateToBlackboard();
            }
            BlackBoard.SerializeBoard(GetBoardPath(fileName), onFailure);
        }

        /// Loads a game from a specified file name.
        /// Clears dynamic active boards and fully hydrates the passive board.
        public static bool LoadGame(string fileName, Action<string> onFailure = null){
            BlackBoard.Clear();
            return BlackBoard.DeserializeAllBoards(GetBoardPath(fileName), onFailure);
        }

        /// Saves a game using the current "saveFileName" configured by the UI/MVC.
        public void SaveCurrentConfiguredFile(Action<string> onFailure = null){
            if (string.IsNullOrEmpty(saveFileName)){
                onFailure?.Invoke("Save aborted: No save filename has been set.");
                return;
            }
            SaveGame(saveFileName, onFailure);
        }

        /// Loads a game using the current "loadFileName" configured by the UI/MVC.
        public bool LoadCurrentConfiguredFile(Action<string> onFailure = null){
            if (!string.IsNullOrEmpty(loadFileName)) return LoadGame(loadFileName, onFailure);
            onFailure?.Invoke("Load aborted: No load filename has been set.");
            return false;
        }

        /// Gathers all save files from disk (including auto saves) and returns them sorted by newest first.
        public List<SaveFileMetadata> GetSaveFileList(){
            List<SaveFileMetadata> list = new();
            if (!Directory.Exists(_saveDirectory)) return list;
            
            string[] files = Directory.GetFiles(_saveDirectory, "*.json");
            foreach (string file in files){
                string filename = Path.GetFileNameWithoutExtension(file);
                
                bool isSave = filename.StartsWith("savegame_", StringComparison.OrdinalIgnoreCase);
                bool isAuto = filename.StartsWith("autosave_", StringComparison.OrdinalIgnoreCase);
                if (!isSave && !isAuto) continue;

                SaveFileMetadata meta = new(){
                    saveName      = filename,
                    lastWriteTime = File.GetLastWriteTime(file),
                    isAutosave    = isAuto
                };

                if (meta.isAutosave){
                    string numPart = filename.Replace("autosave_", "");
                    meta.displayName = $"Autosave {numPart}";
                }
                else
                    meta.displayName = filename.Replace("savegame_", "Save Slot ");

                list.Add(meta);
            }

            list.Sort((a, b) => b.lastWriteTime.CompareTo(a.lastWriteTime));
            return list;
        }

        public static void Autosave(Action<string> onFailure = null){
            DateTime oldestTime = DateTime.MaxValue;
            string   targetName = "autosave_00";

            for (int i = 0; i < 3; i++){
                string baseName = $"autosave_{i:D2}";
                string path     = GetBoardPath(baseName);

                if (!File.Exists(path)){
                    targetName = baseName;
                    break;
                }

                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime >= oldestTime) continue;
                oldestTime = writeTime;
                targetName = baseName;
            }
            SaveGame(targetName, onFailure);
        }
    }
}
