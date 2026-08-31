using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Editor.DataBakers{
    /// Unity Editor Tool to parse raw JSON databases, generate or update Mastery ScriptableObjects,
    /// and compile English localization values without breaking asset links.
    public static class MasteryImporter{
        private const string
            DefaultJsonPath      = "Assets/Editor/DataBakers/masteries.json",
            TargetAssetFolder    = "Assets/Data/Masteries",
            LocalizationFilePath = "Assets/StreamingAssets/Localization/masteries_en.txt";

        // ReSharper disable once ClassNeverInstantiated.Local
        private class MasteryJsonData{
            public string
                name,
                description;

            public List<List<string>> requirements;
            public List<string>       bonuses;
        }

        [MenuItem("Tools/CRPG/Import Masteries Database")]
        public static void ImportMasteriesDatabase(){
            string jsonPath = ResolveJsonPath();
            if (string.IsNullOrEmpty(jsonPath)) return;
            string jsonContent = File.ReadAllText(jsonPath);
            
            Dictionary<string, MasteryJsonData> masteriesMap = DeserializeMasteries(jsonContent);
            if (masteriesMap == null || masteriesMap.Count == 0){
                Debug.LogError("[MasteryImporter] Masteries database is empty or could not be parsed.");
                return;
            }

            EnsureDirectoryExists(TargetAssetFolder);
            StringBuilder locBuilder = InitializeLocalizationHeader();

            int importedCount = ImportAllMasteries(masteriesMap, locBuilder);
            SaveLocalizationFile(locBuilder.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MasteryImporter] Successfully imported {importedCount} Masteries and compiled Localization.");
        }

        /// Resolves the masteries.json path. Falls back to opening a file dialog if the default path is missing.
        private static string ResolveJsonPath(){
            if (File.Exists(DefaultJsonPath))
                return DefaultJsonPath;

            Debug.LogWarning($"[MasteryImporter] Default masteries JSON not found at: {DefaultJsonPath}. Opening file panel...");

            string selectedPath = EditorUtility.OpenFilePanel("Select masteries.json", "Assets", "json");
            if (string.IsNullOrEmpty(selectedPath)){
                Debug.LogError("[MasteryImporter] Import cancelled: No masteries.json file was selected.");
                return null;
            }

            if (selectedPath.Contains("Assets/"))
                selectedPath = "Assets" + selectedPath.Split(new[]{ "Assets" }, StringSplitOptions.None)[1];

            return selectedPath;
        }

        /// Deserializes the JSON content into a Dictionary. Falls back to a List format if needed.
        private static Dictionary<string, MasteryJsonData> DeserializeMasteries(string jsonContent){
            try{
                return JsonConvert.DeserializeObject<Dictionary<string, MasteryJsonData>>(jsonContent);
            }
            catch (Exception ex){
                Debug.LogWarning($"[MasteryImporter] Map deserialization failed ({ex.Message}). Trying list format fallback...");
                try{
                    List<MasteryJsonData> flatList = JsonConvert.DeserializeObject<List<MasteryJsonData>>(jsonContent);

                    Dictionary<string, MasteryJsonData> map = new();
                    
                    if (flatList == null) return map;
                    foreach (MasteryJsonData item in flatList)
                        if (!string.IsNullOrWhiteSpace(item.name))
                            map[item.name] = item;
                    return map;
                }
                catch (Exception fallbackEx){
                    Debug.LogError($"[MasteryImporter] Both map and list deserialization failed. Error: {fallbackEx.Message}");
                    return null;
                }
            }
        }

        /// Prepares the localization file header.
        private static StringBuilder InitializeLocalizationHeader(){
            StringBuilder sb = new();
            sb.AppendLine("# --- Auto-Generated Mastery Localization Keys ---");
            sb.AppendLine($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            return sb;
        }

        /// Iterates over all masteries, processes localization, parses skills, and creates/updates assets.
        private static int ImportAllMasteries(Dictionary<string, MasteryJsonData> masteriesMap, StringBuilder locBuilder){
            int count = 0;

            foreach (KeyValuePair<string, MasteryJsonData> kvp in masteriesMap){
                MasteryJsonData data = kvp.Value;
                if (data == null || string.IsNullOrWhiteSpace(data.name)) continue;

                string rawName   = data.name.Trim();
                string cleanId   = "MASTERY_" + rawName.ToUpper().Replace(" ", "_");
                string assetName = cleanId + ".asset";
                string assetPath = Path.Combine(TargetAssetFolder, assetName);

                AppendLocalizationKeys(locBuilder, cleanId, rawName, data.description);
                ParseMasterySkills(rawName, data.bonuses, out List<Skill> positiveSkills, out List<Skill> penalizedSkills);
                SaveMasteryAsset(assetPath, cleanId, positiveSkills, penalizedSkills, data.requirements ?? new List<List<string>>());

                count++;
            }
            return count;
        }

        /// Appends Name and Description keys for a mastery to the localization StringBuilder.
        private static void AppendLocalizationKeys(StringBuilder locBuilder, string cleanId, string rawName, string description){
            string nameKey = $"NAME_{cleanId}";
            string descKey = $"DESC_{cleanId}";

            locBuilder.AppendLine($"{nameKey} = \"{rawName}\"");
            locBuilder.AppendLine($"{descKey} = \"{description?.Trim().Replace("\"", "\\\"") ?? ""}\"");
            locBuilder.AppendLine();
        }

        /// Loads an existing Mastery asset or creates a new one, initializes it, and saves it.
        private static void SaveMasteryAsset(string assetPath, string cleanId, List<Skill> positiveSkills, List<Skill> penalizedSkills, List<List<string>> requirements){
            string nameKey = $"NAME_{cleanId}";
            string descKey = $"DESC_{cleanId}";

            Mastery masteryAsset = AssetDatabase.LoadAssetAtPath<Mastery>(assetPath);
            bool    isNew        = false;

            if (masteryAsset == null){
                masteryAsset = ScriptableObject.CreateInstance<Mastery>();
                isNew        = true;
            }
            
            List<RequirementRule> requirementsRules = requirements.Select(args => new RequirementRule(args)).ToList();
            masteryAsset.Initialize(cleanId, nameKey, descKey, positiveSkills, penalizedSkills, requirementsRules);

            if (isNew)
                AssetDatabase.CreateAsset(masteryAsset, assetPath);
            else
                EditorUtility.SetDirty(masteryAsset);
        }

        /// Writes the final localization output to disk.
        private static void SaveLocalizationFile(string localizationContent){
            try{
                EnsureDirectoryExists(Path.GetDirectoryName(LocalizationFilePath));
                File.WriteAllText(LocalizationFilePath, localizationContent, Encoding.UTF8);
            }
            catch (Exception ex){
                Debug.LogError($"[MasteryImporter] Error writing localization values to disk: {ex.Message}");
            }
        }

        /// Ensures a directory exists, creating it if necessary.
        private static void EnsureDirectoryExists(string path){
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        /// Splits bonuses list into positive skills and penalized skills.
        private static void ParseMasterySkills(string masteryName, List<string> rawSkills, out List<Skill> positive, out List<Skill> penalized){
            positive  = new List<Skill>();
            penalized = new List<Skill>();

            if (rawSkills == null) return;

            foreach (string rawSkill in rawSkills){
                if (string.IsNullOrWhiteSpace(rawSkill)) continue;

                string trimmedSkill = rawSkill.Trim();
                bool   isNegative   = false;

                if (trimmedSkill.EndsWith("-")){
                    isNegative   = true;
                    trimmedSkill = trimmedSkill[..^1].Trim();
                }

                string sanitizedSkill = trimmedSkill.Replace(" ", "");

                if (!Enum.TryParse(sanitizedSkill, true, out Skill parsedSkill))
                    Debug.LogWarning($"[MasteryImporter] Mastery '{masteryName}' contains unknown Skill identifier '{rawSkill}'.");
                else
                    if (isNegative)
                        penalized.Add(parsedSkill);
                    else
                        positive.Add(parsedSkill);
            }
        }
    }
}