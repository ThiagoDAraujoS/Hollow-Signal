using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Managers {
    public class SaveSystem : MonoBehaviour{
        private static SaveSystem _instance;
        private        Blackboard _blackboard;

        public static Blackboard Blackboard => _instance._blackboard;

        private       string _currentSaveSlot;
        public static string CurrentSaveSlot          => _instance._currentSaveSlot;
        public static string CurrentSaveSlotDirectory => Path.Combine(_baseSavePath, CurrentSaveSlot);

        private const string TempDirectoryName = "temp";
        public static string TempDirectory => Path.Combine(_baseSavePath, TempDirectoryName);

        private static string _baseSavePath;

        public void Awake(){
            if (_instance != null && _instance != this){
                Destroy(gameObject);
                return;
            }

            _instance     = this;
            _baseSavePath = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_baseSavePath)){
                Directory.CreateDirectory(_baseSavePath);
            }

            DontDestroyOnLoad(gameObject);
            _blackboard = gameObject.GetComponent<Blackboard>();
        }

        private void OnDestroy(){
            if (_instance == this)
                _instance = null;
        }

        /// Configures the active save slot folder. Creates the directory if it does not exist.
        public void SetSaveSlot(string slotName){
            _currentSaveSlot = slotName;
            string directory = CurrentSaveSlotDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        
        /// Cold Stop: Completely purges ALL loaded partitions from active memory.
        public static void ClearActiveMemory() => Blackboard.Clear();
        
        /// Scene Transition: Unloads specific, non-persistent file partitions from active memory 
        public static void ReleaseFile(string fileName){
            if (!Blackboard.Contains(fileName))
                Blackboard.ReleaseFile(fileName);
        }
        
        /// Saves the Blackboard into the active save slot directory atomically.
        /// Serializes all partitions and a metadata file to the Temp directory first, 
        /// then performs a rapid directory swap.
        public static async Task SaveGame(Action<string> onFailure = null) {
            try {
                if (Directory.Exists(TempDirectory)) 
                    Directory.Delete(TempDirectory, true);
                Directory.CreateDirectory(TempDirectory);

                await Blackboard.SerializeBoard(onFailure);
                SaveFileMetadata meta = new(
                    CurrentSaveSlot,
                    DateTime.Now,
                    CurrentSaveSlotDirectory
                );
                string metaJson = JsonConvert.SerializeObject(meta, Formatting.Indented);
                string metaFilePath = Path.Combine(TempDirectory, "meta.json");
                await File.WriteAllTextAsync(metaFilePath, metaJson);

                if (Directory.Exists(CurrentSaveSlotDirectory))
                    Directory.Delete(CurrentSaveSlotDirectory, true);

                Directory.Move(TempDirectory, CurrentSaveSlotDirectory);
            }
            catch (Exception) {
                if (Directory.Exists(TempDirectory))
                    Directory.Delete(TempDirectory, true);
            }
        }

        /// Loads a specific Blackboard partition on demand from the active save slot directory.
        public async Task LoadFile(string fileName, Action<string> onFailure = null){
            await _blackboard.DeserializeFiles(new[]{ fileName }, onFailure);
        }

        /// Loads multiple Blackboard partitions in parallel on demand from the active save slot directory.
        public async Task LoadFiles(IEnumerable<string> fileNames, Action<string> onFailure = null){
            await _blackboard.DeserializeFiles(fileNames, onFailure);
        }
        
        /// Gathers all save folders from disk and reconstructs their metadata by reading
        /// their coined meta.json files, sorting them the newest first.
        public List<SaveFileMetadata> GetSaveFileList() {
            List<SaveFileMetadata> saveList = new();
            string[] directories = Directory.GetDirectories(_baseSavePath);
            foreach (string dirPath in directories){
                string dirName = Path.GetFileName(dirPath);
                if (string.Equals(dirName, TempDirectoryName, StringComparison.OrdinalIgnoreCase)) continue;
                string metaFilePath = Path.Combine(dirPath, "meta.json");
                try{
                    string           json = File.ReadAllText(metaFilePath);
                    SaveFileMetadata meta = JsonConvert.DeserializeObject<SaveFileMetadata>(json);
                    saveList.Add(meta);
                }
                catch (Exception e){
                    Debug.LogWarning($"[SaveSystem] Failed to parse metadata for {dirName}: {e.Message}");
                    DateTime lastWriteTime = Directory.GetLastWriteTimeUtc(dirPath);
                    saveList.Add(new SaveFileMetadata(dirName, lastWriteTime.ToLocalTime(), dirPath));
                }
            }
            saveList.Sort((a, b) => b.lastSaveTime.CompareTo(a.lastSaveTime));
            return saveList;
        }

        /// Automatically performs an atomic save to one of three rolling autosave directories,
        /// overwriting the oldest existing autosave slot.
        public static async Task Autosave(Action<string> onFailure = null){
            DateTime oldestTime = DateTime.MaxValue;
            string   targetName = "autosave_00";

            for (int i = 0; i < 3; i++){
                string baseName = $"autosave_{i:D2}";
                string path     = Path.Combine(_baseSavePath, baseName);
                
                if (!Directory.Exists(path)){
                    targetName = baseName;
                    break;
                }
                
                DateTime writeTime = Directory.GetLastWriteTimeUtc(path);
                if (writeTime >= oldestTime)
                    continue;

                oldestTime = writeTime;
                targetName = baseName;
            }
            
            _instance.SetSaveSlot(targetName);
            await SaveGame(onFailure);
        }
    }

    [Serializable]
    public struct SaveFileMetadata{
        public string   slotName;
        public DateTime lastSaveTime;
        public string   directoryPath;

        public SaveFileMetadata(string slotName, DateTime lastSaveTime, string directoryPath){
            this.slotName      = slotName;
            this.lastSaveTime  = lastSaveTime;
            this.directoryPath = directoryPath;
        }
    }
}
