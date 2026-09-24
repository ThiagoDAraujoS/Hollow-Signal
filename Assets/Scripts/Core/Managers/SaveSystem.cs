using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.State;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Managers{
    [RequireComponent(typeof(Blackboard))]
    public class SaveSystem : MonoBehaviour{
        private Blackboard _blackboard;

        public static SaveSystem Instance{ get; private set; }

        public static Blackboard Blackboard => Instance._blackboard;
        public        string[]   coreFileNames = { "core" };

        [SerializeField] private string currentSaveSlot;
        public static            string CurrentSaveSlot          => Instance.currentSaveSlot;
        public static            string CurrentSaveSlotDirectory => Path.Combine(_baseSavePath, CurrentSaveSlot);

        [SerializeField] private string defaultSaveTemplate = "template";
        public static            string DefaultSaveTemplate => Instance.defaultSaveTemplate;

        private const string TempDirectoryName          = "temp";
        private const string ActiveSessionDirectoryName = "_active_session";

        public static string TempDirectory          => Path.Combine(_baseSavePath, TempDirectoryName);
        public static string ActiveSessionDirectory => _activeSessionDirectory;

        private static string _baseSavePath;
        private static string _activeSessionDirectory;

        public void Awake(){
            if (Instance != null && Instance != this){
                Destroy(gameObject);
                return;
            }

            Instance                = this;
            _baseSavePath           = Path.Combine(Application.persistentDataPath, "Saves");
            _activeSessionDirectory = Path.Combine(Application.persistentDataPath, ActiveSessionDirectoryName);

            if (!Directory.Exists(_baseSavePath))
                Directory.CreateDirectory(_baseSavePath);

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

        /// Copies files from the selected source save slot directly into the active session working folder.
        public static void InitializeSession(string sourceSlotName){
            Instance.currentSaveSlot = sourceSlotName;

            if (Directory.Exists(ActiveSessionDirectory))
                Directory.Delete(ActiveSessionDirectory, true);

            Directory.CreateDirectory(ActiveSessionDirectory);

            string sourceDir = Path.Combine(_baseSavePath, sourceSlotName);
            if (Directory.Exists(sourceDir))
                CopyDirectory(sourceDir, ActiveSessionDirectory);
        }

        /// Flushes active entities and commits all currently loaded Blackboard partitions to the active session folder on disk.
        public static async Task CommitToActiveSessionAsync(Action<string> onFailure = null){
            foreach (BlackboardClient client in BlackboardClient.ActiveClients)
                client.FlushStateToBlackboard();

            await Blackboard.SerializeBoard(onFailure);
        }

        /// Serializes a specific partition to the active session folder on disk and purges it from RAM.
        public static async Task CommitAndReleaseFileAsync(string fileName, Action<string> onFailure = null){
            if (!Blackboard.Contains(fileName)) return;

            foreach (BlackboardClient client in BlackboardClient.ActiveClients)
                if (string.Equals(client.fileName, fileName, StringComparison.OrdinalIgnoreCase))
                    client.FlushStateToBlackboard();

            await Blackboard.SerializeFile(fileName, onFailure);
            Blackboard.ReleaseFile(fileName);
        }

        /// Cold Stop: Completely purges ALL loaded partitions from active memory.
        public static void ClearActiveMemory() => Blackboard.Clear();

        /// Scene Transition: Unloads specific, non-persistent file partitions from active memory.
        public static void ReleaseFile(string fileName){
            if (Blackboard.Contains(fileName))
                Blackboard.ReleaseFile(fileName);
        }

        /// Commits active RAM state to the working session and copies the session folder to the target save slot.
        public static async Task SaveGame(string targetSlotName = null, Action<string> onFailure = null){
            try{
                if (!string.IsNullOrEmpty(targetSlotName))
                    Instance.currentSaveSlot = targetSlotName;

                await CommitToActiveSessionAsync(onFailure);

                string charName = GameSessionManager.Instance != null
                    ? GameSessionManager.Instance.mainCharacterName.Value
                    : null;

                string mapLocation = GameSessionManager.Instance != null && !string.IsNullOrEmpty(GameSessionManager.Instance.currentMapName.Value)
                    ? GameSessionManager.Instance.currentMapName.Value
                    : (!string.IsNullOrEmpty(SceneCoordinator.Instance?.ActiveMapScene)
                        ? SceneCoordinator.Instance.ActiveMapScene
                        : "Unknown Location");

                SaveFileMetadata meta = new(
                    CurrentSaveSlot,
                    DateTime.Now,
                    mapLocation,
                    charName,
                    "Station Outpost"
                );

                string metaJson     = JsonConvert.SerializeObject(meta, Formatting.Indented);
                string metaFilePath = Path.Combine(ActiveSessionDirectory, "meta.json");
                await File.WriteAllTextAsync(metaFilePath, metaJson);

                if (Directory.Exists(TempDirectory))
                    Directory.Delete(TempDirectory, true);

                CopyDirectory(ActiveSessionDirectory, TempDirectory);

                if (Directory.Exists(CurrentSaveSlotDirectory))
                    Directory.Delete(CurrentSaveSlotDirectory, true);

                Directory.Move(TempDirectory, CurrentSaveSlotDirectory);
            }
            catch (Exception){
                if (Directory.Exists(TempDirectory))
                    Directory.Delete(TempDirectory, true);
            }
        }

        /// Helper method to recursively copy all files from source directory to destination directory.
        private static void CopyDirectory(string sourceDir, string destinationDir){
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);

            foreach (string subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destinationDir, Path.GetFileName(subDir)));
        }

        /// Loads a specific Blackboard partition on demand from the active session directory.
        public static async Task LoadFile(string fileName, Action<string> onFailure = null) =>
            await Instance._blackboard.DeserializeFiles(new[]{ fileName }, onFailure);

        /// Loads multiple Blackboard partitions in parallel on demand from the active session directory.
        public static async Task LoadFiles(IEnumerable<string> fileNames, Action<string> onFailure = null){
            if (fileNames == null) return;
            await Instance._blackboard.DeserializeFiles(fileNames, onFailure);
        }

        /// Starts a new game session using the default template.
        public static async Task StartNewGameAsync() => await SceneCoordinator.StartGameSessionAsync(DefaultSaveTemplate);

        /// Loads the most recent save file and launches the game session.
        public static async Task ContinueGameAsync(){
            List<SaveFileMetadata> saves = GetSaveFileList();
            if (saves.Count == 0) return;
            await SceneCoordinator.StartGameSessionAsync(saves[0].slotName);
        }

        /// Deletes a save slot directory and its contents from disk.
        public static bool DeleteSave(string slotName){
            if (string.IsNullOrEmpty(slotName)) return false;
            if (string.Equals(slotName, TempDirectoryName, StringComparison.OrdinalIgnoreCase)) return false;
            if (Instance != null && string.Equals(slotName, Instance.defaultSaveTemplate, StringComparison.OrdinalIgnoreCase)) return false;

            _baseSavePath ??= Path.Combine(Application.persistentDataPath, "Saves");
            string targetDir = Path.Combine(_baseSavePath, slotName);
            if (!Directory.Exists(targetDir)) return false;
            Directory.Delete(targetDir, true);
            return true;
        }

        /// Gathers all save folders from disk and reconstructs their metadata by reading
        /// their coined meta.json files, sorting them the newest first.
        public static List<SaveFileMetadata> GetSaveFileList(){
            List<SaveFileMetadata> saveList = new();

            string[] directories = Directory.GetDirectories(_baseSavePath);
            foreach (string dirPath in directories){
                string dirName = Path.GetFileName(dirPath);

                if (string.Equals(dirName, TempDirectoryName,            StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(dirName, Instance.defaultSaveTemplate, StringComparison.OrdinalIgnoreCase)) continue;

                string metaFilePath = Path.Combine(dirPath, "meta.json");
                try{
                    string           json = File.ReadAllText(metaFilePath);
                    SaveFileMetadata meta = JsonConvert.DeserializeObject<SaveFileMetadata>(json);
                    saveList.Add(meta);
                }
                catch (Exception e){
                    Debug.LogWarning($"[SaveSystem] Failed to parse metadata for {dirName}: {e.Message}");
                    DateTime lastWriteTime = Directory.GetLastWriteTimeUtc(dirPath);
                    saveList.Add(new SaveFileMetadata(dirName, lastWriteTime.ToLocalTime(), "Unknown Location"));
                }
            }

            saveList.Sort((a, b) => b.lastSaveTime.CompareTo(a.lastSaveTime));
            return saveList;
        }

        /// Automatically performs an atomic save to one of three rolling autosave directories,
        /// overwriting the oldest existing autosave slot.
        public static async Task AutosaveAsync(Action<string> onFailure = null){
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

            await SaveGame(targetName, onFailure);
        }

#if UNITY_EDITOR
        [ContextMenu("New Game")]
        public void StartNewGame() => _ = SceneCoordinator.StartGameSessionAsync(DefaultSaveTemplate);

        [ContextMenu("Load")]
        public void LoadNewGame() => _ = SceneCoordinator.StartGameSessionAsync("TestSave");

        [ContextMenu("Close")]
        public void CloseSession() => _ = SceneCoordinator.Instance.ReturnToTitleMenuAsync();

        [ContextMenu("Save")]
        public void Save() => _ = SaveGame("TestSave");

        [ContextMenu("AutoSave")]
        public void AutoSave() => _ = AutosaveAsync();
#endif
    }

    [Serializable]
    public struct SaveFileMetadata{
        public string   slotName;
        public DateTime lastSaveTime;
        public string   location;
        public string   characterName;
        public string   region;

        public SaveFileMetadata(string slotName, DateTime lastSaveTime, string location, string characterName = null, string region = null){
            this.slotName      = slotName;
            this.lastSaveTime  = lastSaveTime;
            this.location      = location;
            this.characterName = characterName;
            this.region        = region;
        }
    }
}
