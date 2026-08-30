using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Core{
    /// Pure data transfer object representing a single save file snapshot.
    public class SaveData{
        /// Serialized global blackboard variables.
        public Dictionary<string, float> globalData = new();

        /// Serialized scene-specific blackboard boards mapped by scene name.
        public Dictionary<string, Dictionary<string, float>> sceneData = new();

        /// Serialized entity-specific blackboard boards mapped by UUID.
        public Dictionary<string, Dictionary<string, float>> entityData = new();
    }

    /// Handles physical disk operations using slot-based JSON file serialization.
    public static class SaveLoadManager{
        /// The physical directory on the local system where save files are written.
        private static readonly string SAVE_DIRECTORY = Application.persistentDataPath;

        /// Generates the absolute physical system path for a specific save slot.
        public static string GetSaveFilePath(int slot) => Path.Combine(SAVE_DIRECTORY, $"savegame_{slot:D2}.json");

        /// Captures the current Blackboard state and writes it as an indented JSON file to disk.
        public static void SaveGame(int slot){
            SaveData savePackage = Blackboard.ExportSavePackage();
            string   json        = JsonConvert.SerializeObject(savePackage, Formatting.Indented);
            string   path        = GetSaveFilePath(slot);

            try{
                File.WriteAllText(path, json);
                Debug.Log($"[SaveSystem] Successfully saved slot {slot} to: {path}");
            }
            catch (System.Exception e){
                Debug.LogError($"[SaveSystem] Failed to write save file for slot {slot}: {e.Message}");
            }
        }

        /// Reads a save file from disk and completely overwrites active Blackboard memory with its contents.
        public static bool LoadGame(int slot){
            string path = GetSaveFilePath(slot);
            if (!File.Exists(path)){
                Debug.LogWarning($"[SaveSystem] Load aborted: No save game file exists at: {path}");
                return false;
            }

            try{
                string   json        = File.ReadAllText(path);
                SaveData savePackage = JsonConvert.DeserializeObject<SaveData>(json);

                if (savePackage == null)
                    return false;

                Blackboard.ImportSavePackage(savePackage);
                Debug.Log($"[SaveSystem] Successfully loaded and restored save state from slot {slot}.");
                return true;
            }
            catch (System.Exception e){
                Debug.LogError($"[SaveSystem] Save file for slot {slot} is corrupted: {e.Message}");
                return false;
            }
        }
    }
}