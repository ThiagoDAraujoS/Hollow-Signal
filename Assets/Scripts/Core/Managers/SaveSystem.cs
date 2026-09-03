using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Managers {
    [RequireComponent(typeof(Blackboard))]
    public class SaveSystem : MonoBehaviour{
        private        Blackboard _blackboard;

        public static SaveSystem Instance{ get; private set; }

        public static Blackboard Blackboard => Instance._blackboard;
        public        string[]   coreFileNames = { "core" };

        [SerializeField] private string currentSaveSlot;
        public static            string CurrentSaveSlot          => Instance.currentSaveSlot;
        public static            string CurrentSaveSlotDirectory => Path.Combine(_baseSavePath, CurrentSaveSlot);

        [SerializeField] private string defaultSaveTemplate = "template";

        private const string TempDirectoryName = "temp";
        public static string TempDirectory => Path.Combine(_baseSavePath, TempDirectoryName);

        private static string _baseSavePath;

        public void Awake(){
            if (Instance != null && Instance != this){
                Destroy(gameObject);
                return;
            }

            Instance     = this;
            _baseSavePath = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_baseSavePath)){
                Directory.CreateDirectory(_baseSavePath);
            }

            _blackboard = gameObject.GetComponent<Blackboard>();
        }

        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }

        /// Configures the active save slot folder. Creates the directory if it does not exist.
        public static void SetSaveSlot(string slotName){
            Instance.currentSaveSlot = slotName;
            string directory = CurrentSaveSlotDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// Cold Stop: Completely purges ALL loaded partitions from active memory.
        public static void ClearActiveMemory() => Blackboard.Clear();

        /// Scene Transition: Unloads specific, non-persistent file partitions from active memory 
        public static void ReleaseFile(string fileName){
            if (Blackboard.Contains(fileName))
                Blackboard.ReleaseFile(fileName);
        }

        [ContextMenu("Save")]
        public void Save() => _ = SaveGame();

        /// Saves the Blackboard into the active save slot directory atomically.
        /// Serializes all partitions and a metadata file to the Temp directory first, 
        /// then performs a rapid directory swap.
        public static async Task SaveGame(Action<string> onFailure = null){
            try{
                if (Directory.Exists(TempDirectory))
                    Directory.Delete(TempDirectory, true);
                Directory.CreateDirectory(TempDirectory);

                // IMPORTANT: Flush all active game objects into the Blackboard dict before saving
                foreach (BlackboardClient client in BlackboardClient.ActiveClients){
                    client.FlushStateToBlackboard();
                }

                await Blackboard.SerializeBoard(onFailure);
                SaveFileMetadata meta = new(
                    CurrentSaveSlot,
                    DateTime.Now,
                    CurrentSaveSlotDirectory
                );
                string metaJson     = JsonConvert.SerializeObject(meta, Formatting.Indented);
                string metaFilePath = Path.Combine(TempDirectory, "meta.json");
                await File.WriteAllTextAsync(metaFilePath, metaJson);

                if (Directory.Exists(CurrentSaveSlotDirectory))
                    Directory.Delete(CurrentSaveSlotDirectory, true);

                Directory.Move(TempDirectory, CurrentSaveSlotDirectory);
            }
            catch (Exception){
                if (Directory.Exists(TempDirectory))
                    Directory.Delete(TempDirectory, true);
            }
        }

        /// Loads a specific Blackboard partition on demand from the active save slot directory.
        public static async Task LoadFile(string fileName, Action<string> onFailure = null){
            await Instance._blackboard.DeserializeFiles(new[]{ fileName }, onFailure);
        }

        /// Loads multiple Blackboard partitions in parallel on demand from the active save slot directory.
        public static async Task LoadFiles(IEnumerable<string> fileNames, Action<string> onFailure = null){
            if (fileNames == null) return;
            await Instance._blackboard.DeserializeFiles(fileNames, onFailure);
        }

        /// Gathers all save folders from disk and reconstructs their metadata by reading
        /// their coined meta.json files, sorting them the newest first.
        public static List<SaveFileMetadata> GetSaveFileList(){
            List<SaveFileMetadata> saveList = new();

            string[] directories = Directory.GetDirectories(_baseSavePath);
            foreach (string dirPath in directories){
                string dirName = Path.GetFileName(dirPath);
                
                if (string.Equals(dirName, TempDirectoryName, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(dirName, Instance.defaultSaveTemplate, StringComparison.OrdinalIgnoreCase)) continue;
                
                string metaFilePath = Path.Combine(dirPath, "meta.json");
                try{
                    string json = File.ReadAllText(metaFilePath);
                    
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

            SetSaveSlot(targetName);
            await SaveGame(onFailure);
        }

        [ContextMenu("Close Session")]
        public void CloseSession()=> _ = CloseSessionAsync();

        
        /// Dynamically discovers and cleanly unloads all loaded additive scenes 
        /// (GameSession, active levels) and clears active Blackboard memory.
        public static async Task CloseSessionAsync() {
            try {
                Scene bootScene  = SceneManager.GetActiveScene();
                int   sceneCount = SceneManager.sceneCount;

                if (Blackboard != null) 
                    Blackboard.Clear();
                
                // Unload all non-boot scenes
                List<AsyncOperation> unloadOperations = new();
                for (int i = sceneCount - 1; i >= 0; i--) {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded || scene == bootScene) continue;
                    unloadOperations.Add(SceneManager.UnloadSceneAsync(scene));
                }

                foreach (AsyncOperation op in unloadOperations.Where(op => op != null))
                    while (!op.isDone) 
                        await Task.Yield();
            }
            catch (Exception e) {
                Debug.LogError($"[Boot] Failed to close session: {e.Message}");
            }
        }

        [ContextMenu("Load New Game")]
        public void LoadNewGame() => _ = LoadNewGameAsync();

        public static async Task LoadNewGameAsync() {
            Debug.Log($"{CurrentSaveSlotDirectory}");
            try {
                await CloseSessionAsync();
                IEnumerable<string> files = Instance.coreFileNames ?? new[] { "core" };
                await LoadFiles(files, _ => { /* TODO: Abort, close game scenes return to main menu */ });

                AsyncOperation op = SceneManager.LoadSceneAsync("GameSession", LoadSceneMode.Additive);
                while (op is { isDone: false })
                    await Task.Yield();
            }
            catch (Exception e) {
                Debug.LogError($"[Boot] Failed to load new game: {e.Message}");
            }
        }

        [ContextMenu("Start New Game")]
        public void StartNewGame() {
            SetSaveSlot(defaultSaveTemplate);
            LoadNewGame();
            SetSaveSlot("autosave_00");
        }

        public static async Task StartNewGameAsync() {
            SetSaveSlot(Instance.defaultSaveTemplate);
            await LoadNewGameAsync();
            SetSaveSlot("autosave_00");
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