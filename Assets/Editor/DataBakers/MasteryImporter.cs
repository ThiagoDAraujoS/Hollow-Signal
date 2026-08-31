using System;
using System.Collections.Generic;
using System.IO;
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
            DefaultJsonPath      = "Assets/StreamingAssets/masteries.json",
            TargetAssetFolder    = "Assets/Data/Masteries",
            LocalizationFilePath = "Assets/StreamingAssets/Localization/masteries_en.txt";

        [Serializable]
        private class MasteryJsonData{
            public string       name;
            public string       description;
            public List<string> bonuses;
        }

        [MenuItem("Tools/CRPG/Import Masteries Database")]
        public static void ImportMasteriesDatabase(){
            string jsonPath = DefaultJsonPath;
            if (!File.Exists(jsonPath)){
                // Fallback: Show file panel if not found at default location
                jsonPath = EditorUtility.OpenFilePanel("Select Masteries JSON Database", "Assets", "json");
                if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)){
                    Debug.LogError($"[MasteryImporter] Masteries JSON database not found at: {DefaultJsonPath}");
                    return;
                }
            }

            string jsonContent = File.ReadAllText(jsonPath);

            Dictionary<string, MasteryJsonData> masteriesDict = null;

            try{
                masteriesDict = JsonConvert.DeserializeObject<Dictionary<string, MasteryJsonData>>(jsonContent);
            }
            catch (Exception){
                try{
                    List<MasteryJsonData> masteriesList = JsonConvert.DeserializeObject<List<MasteryJsonData>>(jsonContent);
                    if (masteriesList != null){
                        masteriesDict = new Dictionary<string, MasteryJsonData>();
                        foreach (MasteryJsonData item in masteriesList)
                            if (!string.IsNullOrWhiteSpace(item.name))
                                masteriesDict[item.name] = item;
                    }
                }
                catch (Exception ex){
                    Debug.LogError($"[MasteryImporter] Failed to parse JSON database: {ex.Message}");
                    return;
                }
            }

            if (masteriesDict == null || masteriesDict.Count == 0){
                Debug.LogError("[MasteryImporter] Masteries database is empty or could not be parsed.");
                return;
            }
            if (!Directory.Exists(TargetAssetFolder))
                Directory.CreateDirectory(TargetAssetFolder);

            StringBuilder locBuilder = new ();
            locBuilder.AppendLine("# --- Auto-Generated Mastery Localization Keys ---");
            locBuilder.AppendLine($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            locBuilder.AppendLine();

            int importedCount = 0;

            foreach ((string key, MasteryJsonData data) in masteriesDict){
                string displayName = string.IsNullOrWhiteSpace(data.name) ? key : data.name;
                if (string.IsNullOrWhiteSpace(displayName)) continue;

                string rawName   = displayName.Trim();
                string cleanId   = "MASTERY_" + rawName.ToUpper().Replace(" ", "_");
                string assetName = cleanId + ".asset";
                string assetPath = Path.Combine(TargetAssetFolder, assetName);
                
                string nameKey = $"NAME_{cleanId}";
                string descKey = $"DESC_{cleanId}";

                locBuilder.AppendLine($"{nameKey} = \"{rawName}\"");
                locBuilder.AppendLine($"{descKey} = \"{data.description?.Trim().Replace("\"", "\\\"") ?? ""}\"");
                locBuilder.AppendLine();
                
                ParseMasterySkills(rawName, data.bonuses, out List<Skill> positiveSkills, out List<Skill> penalizedSkills);
                
                Mastery masteryAsset = AssetDatabase.LoadAssetAtPath<Mastery>(assetPath);
                bool    isNew        = false;

                if (masteryAsset == null){
                    masteryAsset = ScriptableObject.CreateInstance<Mastery>();
                    isNew        = true;
                }

                masteryAsset.Initialize(cleanId, nameKey, descKey, positiveSkills, penalizedSkills);

                if (isNew)
                    AssetDatabase.CreateAsset(masteryAsset, assetPath);
                else
                    EditorUtility.SetDirty(masteryAsset);

                importedCount++;
            }

            try{
                string locDirectory = Path.GetDirectoryName(LocalizationFilePath);
                if (!string.IsNullOrEmpty(locDirectory) && !Directory.Exists(locDirectory))
                    Directory.CreateDirectory(locDirectory);
                File.WriteAllText(LocalizationFilePath, locBuilder.ToString(), Encoding.UTF8);
            }
            catch (Exception ex){
                Debug.LogError($"[MasteryImporter] Error writing localization values to disk: {ex.Message}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MasteryImporter] Successfully imported {importedCount} Masteries and compiled Localization.");
        }

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
                    trimmedSkill = trimmedSkill.Substring(0, trimmedSkill.Length - 1).Trim();
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