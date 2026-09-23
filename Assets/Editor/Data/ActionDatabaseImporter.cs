using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Data;
using UnityEditor;
using UnityEngine;

namespace Editor.DataBakers{
    /// Unity Editor Tool to parse Actions CSV directly, create/update ActionDefinition objects,
    /// compile English localization values for actions and quips, and supplant ActionDatabase.
    public static class ActionDatabaseImporter{
        public const string
            DefaultCsvPath       = "Assets/Editor/Data/CSVtoJSON/Actions.csv",
            DBPath               = "Assets/Data/ActionDB/ActionDatabase.asset",
            LocalizationFilePath = "Assets/StreamingAssets/Localization/actions_en.txt";

        [MenuItem("Tools/CRPG/3. Import Actions")]
        public static void ImportActionsDatabase(){
            string csvPath = EnumGenerator.PromptOrResolveCsv(DefaultCsvPath, "Select Actions CSV");
            if (string.IsNullOrEmpty(csvPath))
                return;

            ImportActionsFromPath(csvPath);
        }

        /// Imports actions from the specified CSV file, completely supplanting previous action definitions and localization.
        public static void ImportActionsFromPath(string csvPath){
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Actions CSV file not found at: {csvPath}");

            EnsureDirectoryExists(Path.GetDirectoryName(DBPath));
            EnsureDirectoryExists(Path.GetDirectoryName(LocalizationFilePath));

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1){
                Debug.LogError("[ActionDatabaseImporter] Actions CSV is empty or only contains a header.");
                return;
            }

            StringBuilder locBuilder = InitializeLocalizationHeader();
            List<ActionDefinition> importedActions = new();

            for (int i = 1; i < lines.Length; i++){
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> cols = EnumGenerator.ParseCsvRow(line);
                if (cols.Count == 0 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                string rawActionId = cols[0].Trim();
                string sanitizedActionId = EnumGenerator.SanitizeIdentifier(rawActionId);

                if (!Enum.TryParse(sanitizedActionId, true, out ActionType actionType)){
                    Debug.LogWarning($"[ActionDatabaseImporter] Unknown ActionType '{rawActionId}' (Row {i + 1}). Make sure to run '1. Generate Enums' first!");
                    continue;
                }

                string displayName = cols.Count > 1 ? cols[1].Trim() : actionType.ToString();
                string perkModeRaw = cols.Count > 7 ? cols[7].Trim() : "None";
                List<string> rawSuccessQuips = ParseQuips(cols.Count > 8 ? cols[8].Trim() : string.Empty);
                List<string> rawFailureQuips = ParseQuips(cols.Count > 9 ? cols[9].Trim() : string.Empty);

                string upperId = sanitizedActionId.ToUpperInvariant();
                string nameKey = $"NAME_ACTION_{upperId}";

                ActionDefinition def = new(){
                    actionType = actionType,
                    nameKey = nameKey,
                    perkRequirement = ParsePerkRequirementMode(perkModeRaw),
                    applicableSkills = new(),
                    successQuipKeys = new(),
                    failureQuipKeys = new()
                };

                for (int c = 2; c <= 6 && c < cols.Count; c++){
                    string skillRaw = cols[c].Trim();
                    if (string.IsNullOrEmpty(skillRaw))
                        continue;

                    string sanitizedSkill = EnumGenerator.SanitizeIdentifier(skillRaw);
                    if (Enum.TryParse(sanitizedSkill, true, out Skill parsedSkill)){
                        if (!def.applicableSkills.Contains(parsedSkill))
                            def.applicableSkills.Add(parsedSkill);
                    }
                    else
                        Debug.LogWarning($"[ActionDatabaseImporter] Unknown skill '{skillRaw}' in action '{rawActionId}' (Row {i + 1}).");
                }

                locBuilder.AppendLine($"{nameKey} = \"{displayName.Replace("\"", "\\\"")}\"");

                for (int s = 0; s < rawSuccessQuips.Count; s++){
                    string qKey = $"QUIP_SUCCESS_{upperId}_{s}";
                    def.successQuipKeys.Add(qKey);
                    locBuilder.AppendLine($"{qKey} = \"{rawSuccessQuips[s].Replace("\"", "\\\"")}\"");
                }

                for (int f = 0; f < rawFailureQuips.Count; f++){
                    string qKey = $"QUIP_FAILURE_{upperId}_{f}";
                    def.failureQuipKeys.Add(qKey);
                    locBuilder.AppendLine($"{qKey} = \"{rawFailureQuips[f].Replace("\"", "\\\"")}\"");
                }

                locBuilder.AppendLine();
                importedActions.Add(def);
            }

            File.WriteAllText(LocalizationFilePath, locBuilder.ToString(), Encoding.UTF8);

            ActionDatabase db = AssetDatabase.LoadAssetAtPath<ActionDatabase>(DBPath);
            if (db == null){
                db = ScriptableObject.CreateInstance<ActionDatabase>();
                AssetDatabase.CreateAsset(db, DBPath);
            }

            db.UpdateDatabase(importedActions);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ActionDatabaseImporter] Successfully supplanted and imported {importedActions.Count} Actions!\n" +
                      $"• Database: {DBPath}\n" +
                      $"• Localization: {LocalizationFilePath}\n" +
                      $"• Source CSV: {csvPath}", db);
        }

        /// Parses the PerkMode string into PerkRequirementMode enum.
        private static PerkRequirementMode ParsePerkRequirementMode(string raw){
            if (string.IsNullOrWhiteSpace(raw))
                return PerkRequirementMode.None;

            string cleaned = raw.Trim().ToLower();
            if (cleaned == "locked" || cleaned == "shownwhenlocked")
                return PerkRequirementMode.ShownWhenLocked;
            if (cleaned == "hidden" || cleaned == "hiddenwhenlocked")
                return PerkRequirementMode.HiddenWhenLocked;

            return PerkRequirementMode.None;
        }

        /// Splits semicolon-delimited quips into trimmed strings.
        private static List<string> ParseQuips(string raw){
            List<string> quips = new();
            if (string.IsNullOrWhiteSpace(raw))
                return quips;

            string[] split = raw.Split(new[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string q in split){
                string trimmed = q.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    quips.Add(trimmed);
            }
            return quips;
        }

        /// Prepares the localization file header.
        private static StringBuilder InitializeLocalizationHeader(){
            StringBuilder sb = new();
            sb.AppendLine("# --- Auto-Generated Action Localization Keys ---");
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
