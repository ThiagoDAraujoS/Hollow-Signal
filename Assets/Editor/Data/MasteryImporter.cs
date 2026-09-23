using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Data;
using UnityEditor;
using UnityEngine;

namespace Editor.DataBakers{
    /// Unity Editor Tool to parse Masteries CSV directly, create/update Mastery ScriptableObjects,
    /// compile English localization values, and supplant the database so the spreadsheet is the single source of truth.
    public static class MasteryImporter{
        public const string
            DefaultCsvPath       = "Assets/Editor/Data/CSVtoJSON/Masteries.csv",
            TargetAssetFolder    = "Assets/Data/Masteries",
            LocalizationFilePath = "Assets/StreamingAssets/Localization/masteries_en.txt",
            DBPath               = "Assets/Data/MasteryDB/MasteryDatabase.asset";

        [MenuItem("Tools/CRPG/2. Import Masteries")]
        public static void ImportMasteriesDatabase(){
            string csvPath = EnumGenerator.PromptOrResolveCsv(DefaultCsvPath, "Select Masteries CSV");
            if (string.IsNullOrEmpty(csvPath)) return;

            ImportMasteriesFromPath(csvPath);
        }

        /// Imports masteries from the specified CSV file, completely supplanting previous masteries.
        public static void ImportMasteriesFromPath(string csvPath){
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Masteries CSV file not found at: {csvPath}");

            EnsureDirectoryExists(TargetAssetFolder);
            EnsureDirectoryExists(Path.GetDirectoryName(LocalizationFilePath));
            EnsureDirectoryExists(Path.GetDirectoryName(DBPath));

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1){
                Debug.LogError("[MasteryImporter] Masteries CSV is empty or only contains a header.");
                return;
            }

            StringBuilder locBuilder = InitializeLocalizationHeader();
            HashSet<string> validAssetPaths = new(StringComparer.OrdinalIgnoreCase);
            List<Mastery> importedMasteries = new();

            for (int i = 1; i < lines.Length; i++){
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                List<string> cols = EnumGenerator.ParseCsvRow(line);
                if (cols.Count == 0 || string.IsNullOrWhiteSpace(cols[0])) continue;

                string rawName = cols[0].Trim();
                string description = cols.Count > 1 ? cols[1].Trim() : string.Empty;
                string prereqsRaw = cols.Count > 2 ? cols[2].Trim() : "None";

                MasteryImportData importData = new(){
                    id = "MASTERY_" + rawName.ToUpper().Replace(" ", "_").Replace("-", "_"),
                    nameKey = "NAME_MASTERY_" + rawName.ToUpper().Replace(" ", "_").Replace("-", "_"),
                    descKey = "DESC_MASTERY_" + rawName.ToUpper().Replace(" ", "_").Replace("-", "_"),
                    levelRequirement = 0,
                    prerequisites = ParsePrerequisites(prereqsRaw)
                };

                // Parse Skill 1 through Skill 4 (columns 3, 4, 5, 6)
                for (int c = 3; c <= 6 && c < cols.Count; c++){
                    string skillRaw = cols[c].Trim();
                    if (string.IsNullOrEmpty(skillRaw)) continue;

                    string sanitized = EnumGenerator.SanitizeIdentifier(skillRaw);
                    if (Enum.TryParse(sanitized, true, out Skill parsedSkill)){
                        importData.associatedSkills.Add(parsedSkill);
                    }
                    else{
                        Debug.LogWarning($"[MasteryImporter] Unknown skill '{skillRaw}' in mastery '{rawName}' (Row {i + 1}).");
                    }
                }

                string assetPath = $"{TargetAssetFolder}/{importData.id}.asset";
                validAssetPaths.Add(assetPath);

                SaveMasteryAsset(assetPath, importData);
                AppendLocalizationKeys(locBuilder, importData, rawName, description);

                Mastery savedAsset = AssetDatabase.LoadAssetAtPath<Mastery>(assetPath);
                if (savedAsset != null)
                    importedMasteries.Add(savedAsset);
            }

            // Supplant: Purge any existing mastery assets not found in the CSV
            PurgeObsoleteMasteryAssets(validAssetPaths);

            // Supplant: Update database asset singleton
            MasteryDatabase db = AssetDatabase.LoadAssetAtPath<MasteryDatabase>(DBPath);
            if (db == null){
                db = ScriptableObject.CreateInstance<MasteryDatabase>();
                AssetDatabase.CreateAsset(db, DBPath);
            }
            db.UpdateDatabase(importedMasteries);

            // Supplant: Overwrite localization file completely
            File.WriteAllText(LocalizationFilePath, locBuilder.ToString(), Encoding.UTF8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MasteryImporter] Successfully supplanted and imported {importedMasteries.Count} Masteries!\n" +
                      $"• Database: {DBPath}\n" +
                      $"• Assets Folder: {TargetAssetFolder}\n" +
                      $"• Localization: {LocalizationFilePath}\n" +
                      $"• Source CSV: {csvPath}", db);
        }

        /// Purges any .asset files in TargetAssetFolder that are not in the valid incoming paths.
        private static void PurgeObsoleteMasteryAssets(HashSet<string> validPaths){
            string[] existingGuids = AssetDatabase.FindAssets("t:Mastery", new[]{ TargetAssetFolder });
            foreach (string guid in existingGuids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!validPaths.Contains(path)){
                    Debug.Log($"[MasteryImporter] Supplanting: Removing obsolete mastery asset: {path}");
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        /// Parses prerequisite string into requirement rules.
        private static List<RequirementRule> ParsePrerequisites(string raw){
            List<RequirementRule> rules = new();
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("None", StringComparison.OrdinalIgnoreCase))
                return rules;

            string[] clauses = raw.Split(new[]{ ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string clause in clauses){
                string[] parts = clause.Split(new[]{ ':', '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    rules.Add(new RequirementRule(parts.Select(p => p.Trim()).ToList()));
            }
            return rules;
        }

        /// Appends Name and Description keys for a mastery to the localization StringBuilder.
        private static void AppendLocalizationKeys(StringBuilder locBuilder, MasteryImportData importData, string rawName, string description){
            locBuilder.AppendLine($"{importData.nameKey} = \"{rawName}\"");
            locBuilder.AppendLine($"{importData.descKey} = \"{description?.Trim().Replace("\"", "\\\"") ?? ""}\"");
            locBuilder.AppendLine();
        }

        /// Loads an existing Mastery asset or creates a new one, initializes it, and saves it.
        private static void SaveMasteryAsset(string assetPath, MasteryImportData importData){
            Mastery masteryAsset = AssetDatabase.LoadAssetAtPath<Mastery>(assetPath) ?? ScriptableObject.CreateInstance<Mastery>();
            masteryAsset.Initialize(importData);
            if (AssetDatabase.Contains(masteryAsset))
                EditorUtility.SetDirty(masteryAsset);
            else
                AssetDatabase.CreateAsset(masteryAsset, assetPath);
        }

        /// Prepares the localization file header.
        private static StringBuilder InitializeLocalizationHeader(){
            StringBuilder sb = new();
            sb.AppendLine("# --- Auto-Generated Mastery Localization Keys ---");
            sb.AppendLine($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            return sb;
        }

        /// Ensures a directory exists, creating it if necessary.
        private static void EnsureDirectoryExists(string path){
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
