using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Data;
using UnityEditor;
using UnityEngine;

namespace Editor.DataBakers{
    /// Unity Editor Tool to parse Archetypes CSV directly, populate ProblemArchetype objects,
    /// compile English localization values for names and descriptions, and supplant ProblemArchetypeDatabase.
    public static class ProblemArchetypeImporter{
        public const string
            DefaultCsvPath       = "Assets/Editor/Data/CSVtoJSON/Archetypes.csv",
            LegacyAssetFolder    = "Assets/Data/Archetypes",
            DBPath               = "Assets/Data/ArchetypeDB/ProblemArchetypeDatabase.asset",
            LocalizationFilePath = "Assets/StreamingAssets/Localization/archetypes_en.txt";

        [MenuItem("Tools/CRPG/4. Import Problem Archetypes")]
        public static void ImportArchetypesDatabase(){
            string csvPath = EnumGenerator.PromptOrResolveCsv(DefaultCsvPath, "Select Archetypes CSV");
            if (string.IsNullOrEmpty(csvPath))
                return;

            ImportArchetypesFromPath(csvPath);
        }

        /// Imports archetypes from the specified CSV file, completely supplanting previous archetypes and localization.
        public static void ImportArchetypesFromPath(string csvPath){
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Archetypes CSV file not found at: {csvPath}");

            EnsureDirectoryExists(Path.GetDirectoryName(DBPath));
            EnsureDirectoryExists(Path.GetDirectoryName(LocalizationFilePath));

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1){
                Debug.LogError("[ProblemArchetypeImporter] Archetypes CSV is empty or only contains a header.");
                return;
            }

            StringBuilder locBuilder = InitializeLocalizationHeader();
            List<ProblemArchetype> importedArchetypes = new();

            for (int i = 1; i < lines.Length; i++){
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> cols = EnumGenerator.ParseCsvRow(line);
                if (cols.Count == 0 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                string rawArchetypeId = cols[0].Trim();
                string archetypeId = EnumGenerator.SanitizeIdentifier(rawArchetypeId);
                string description = cols.Count > 1 ? cols[1].Trim() : string.Empty;

                string upperId = archetypeId.ToUpperInvariant();
                string nameKey = $"NAME_ARCHETYPE_{upperId}";
                string descKey = $"DESC_ARCHETYPE_{upperId}";

                List<ProblemActionEntry> actions = new();

                for (int c = 2; c < cols.Count; c += 2){
                    string actionRaw = cols[c].Trim();
                    if (string.IsNullOrEmpty(actionRaw))
                        continue;

                    string sanitizedAction = EnumGenerator.SanitizeIdentifier(actionRaw);
                    if (!Enum.TryParse(sanitizedAction, true, out ActionType actionType)){
                        Debug.LogWarning($"[ProblemArchetypeImporter] Unknown ActionType '{actionRaw}' in archetype '{archetypeId}' (Row {i + 1}).");
                        continue;
                    }

                    int levelOffset = 0;
                    if (c + 1 < cols.Count){
                        string offsetRaw = cols[c + 1].Trim().Replace("+", "");
                        int.TryParse(offsetRaw, out levelOffset);
                    }

                    actions.Add(new ProblemActionEntry{
                        actionType = actionType,
                        levelOffset = levelOffset
                    });
                }

                importedArchetypes.Add(new ProblemArchetype{
                    archetypeId = archetypeId,
                    nameKey = nameKey,
                    descKey = descKey,
                    actions = actions
                });

                locBuilder.AppendLine($"{nameKey} = \"{rawArchetypeId.Replace("\"", "\\\"")}\"");
                locBuilder.AppendLine($"{descKey} = \"{description.Replace("\"", "\\\"")}\"");
                locBuilder.AppendLine();
            }

            PurgeLegacyArchetypeFolder();
            File.WriteAllText(LocalizationFilePath, locBuilder.ToString(), Encoding.UTF8);

            ProblemArchetypeDatabase db = AssetDatabase.LoadAssetAtPath<ProblemArchetypeDatabase>(DBPath);
            if (db == null){
                db = ScriptableObject.CreateInstance<ProblemArchetypeDatabase>();
                AssetDatabase.CreateAsset(db, DBPath);
            }

            db.UpdateDatabase(importedArchetypes);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ProblemArchetypeImporter] Successfully supplanted and imported {importedArchetypes.Count} Problem Archetypes!\n" +
                      $"• Database: {DBPath}\n" +
                      $"• Localization: {LocalizationFilePath}\n" +
                      $"• Source CSV: {csvPath}", db);
        }

        /// Purges obsolete standalone asset folder if it exists.
        private static void PurgeLegacyArchetypeFolder(){
            if (AssetDatabase.IsValidFolder(LegacyAssetFolder))
                AssetDatabase.DeleteAsset(LegacyAssetFolder);
        }

        /// Prepares the localization file header.
        private static StringBuilder InitializeLocalizationHeader(){
            StringBuilder sb = new();
            sb.AppendLine("# --- Auto-Generated Problem Archetype Localization Keys ---");
            sb.AppendLine($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            return sb;
        }

        /// Ensures a directory exists, creating it if necessary.
        private static void EnsureDirectoryExists(string path){
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
