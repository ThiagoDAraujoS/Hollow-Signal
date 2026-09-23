using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor.DataBakers{
    /// Editor utility to procedurally generate type-safe C# enums (ActionType and Skill) from CSV databases.
    /// Completely supplants the target enum script with the CSV contents as the single source of truth.
    public static class EnumGenerator{
        public const string DefaultActionsCsvPath = "Assets/Editor/Data/CSVtoJSON/Actions.csv";
        public const string DefaultSkillsCsvPath = "Assets/Editor/Data/CSVtoJSON/Skills.csv";
        public const string ActionTypeScriptPath = "Assets/Scripts/Data/ActionType.cs";
        public const string SkillScriptPath = "Assets/Scripts/Data/Skill.cs";

        [MenuItem("Tools/CRPG/1. Generate Enums (Actions & Skills)")]
        public static void GenerateAllEnums(){
            string actionsPath = PromptOrResolveCsv(DefaultActionsCsvPath, "Select Actions CSV");
            if (string.IsNullOrEmpty(actionsPath)) return;

            string skillsPath = PromptOrResolveCsv(DefaultSkillsCsvPath, "Select Skills CSV");
            if (string.IsNullOrEmpty(skillsPath)) return;

            int actionCount = GenerateActionTypeEnumFromPath(actionsPath);
            int skillCount = GenerateSkillEnumFromPath(skillsPath);
            AssetDatabase.Refresh();

            UnityEngine.Object actionScript = AssetDatabase.LoadAssetAtPath<MonoScript>(ActionTypeScriptPath);
            UnityEngine.Object skillScript = AssetDatabase.LoadAssetAtPath<MonoScript>(SkillScriptPath);

            Debug.Log($"[EnumGenerator] Successfully supplanted and generated enums!\n" +
                      $"• ActionType ({actionCount} values) -> {ActionTypeScriptPath}\n" +
                      $"• Skill ({skillCount} values) -> {SkillScriptPath}", actionScript != null ? actionScript : skillScript);
        }

        /// Checks for default CSV. If present, asks user if they want to use default or pick another file.
        public static string PromptOrResolveCsv(string defaultPath, string title){
            if (File.Exists(defaultPath)){
                int choice = EditorUtility.DisplayDialogComplex(
                    title,
                    $"Found default file at:\n{defaultPath}\n\nDo you want to use this default file or select a custom file?",
                    "Use Default",
                    "Cancel",
                    "Select Custom File..."
                );

                if (choice == 0) return defaultPath;
                if (choice == 1) return null; // Cancelled
            }

            string initialDir = File.Exists(defaultPath)
                ? Path.GetDirectoryName(Path.GetFullPath(defaultPath))
                : Application.dataPath;

            string selected = EditorUtility.OpenFilePanel(title, initialDir, "csv");
            return string.IsNullOrEmpty(selected) ? null : selected;
        }

        /// Generates ActionType.cs from the specified CSV file path, completely supplanting previous values.
        public static int GenerateActionTypeEnumFromPath(string csvPath){
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Actions CSV file not found at: {csvPath}");

            List<string> csvActions = ParseActionIdsFromCsv(csvPath);
            Dictionary<string, int> finalMap = BuildFreshEnumMap(csvActions);
            WriteEnumScript(ActionTypeScriptPath, "ActionType", "Auto-generated ActionType enum mapping to the Actions spreadsheet database.", finalMap);
            return finalMap.Count;
        }

        /// Generates Skill.cs from the specified CSV file path, completely supplanting previous values.
        public static int GenerateSkillEnumFromPath(string csvPath){
            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"Skills CSV file not found at: {csvPath}");

            List<string> csvSkills = ParseSkillsFromCsv(csvPath);
            Dictionary<string, int> finalMap = BuildFreshEnumMap(csvSkills);
            WriteEnumScript(SkillScriptPath, "Skill", "Auto-generated Skill enum mapping to the spreadsheet skills list database.", finalMap);
            return finalMap.Count;
        }

        /// Extracts ActionId from first column of Actions.csv.
        private static List<string> ParseActionIdsFromCsv(string path){
            List<string> actions = new();
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++){
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                List<string> cols = ParseCsvRow(line);
                if (cols.Count == 0) continue;
                string actionId = SanitizeIdentifier(cols[0]);
                if (!string.IsNullOrEmpty(actionId) && !actions.Contains(actionId))
                    actions.Add(actionId);
            }
            return actions;
        }

        /// Extracts Skill Name from first column of Skills.csv.
        private static List<string> ParseSkillsFromCsv(string path){
            List<string> skills = new();
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++){
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                List<string> cols = ParseCsvRow(line);
                if (cols.Count == 0) continue;
                string skillName = SanitizeIdentifier(cols[0]);
                if (!string.IsNullOrEmpty(skillName) && !skills.Contains(skillName))
                    skills.Add(skillName);
            }
            return skills;
        }

        /// Builds a brand new enum dictionary starting with None = 0, supplanting any previous definitions.
        private static Dictionary<string, int> BuildFreshEnumMap(List<string> entries){
            Dictionary<string, int> map = new(StringComparer.OrdinalIgnoreCase){
                { "None", 0 }
            };

            int nextId = 1;
            foreach (string entry in entries){
                if (!map.ContainsKey(entry)){
                    map[entry] = nextId++;
                }
            }

            return map;
        }

        /// Formats and writes C# enum code to file.
        private static void WriteEnumScript(string scriptPath, string enumName, string docComment, Dictionary<string, int> entries){
            string dir = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            List<KeyValuePair<string, int>> sorted = new(entries);
            sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

            StringBuilder sb = new();
            sb.AppendLine("namespace Data");
            sb.AppendLine("{");
            sb.AppendLine($"    /// {docComment}");
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");

            foreach (KeyValuePair<string, int> kvp in sorted)
                sb.AppendLine($"        {kvp.Key} = {kvp.Value},");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(scriptPath, sb.ToString(), Encoding.UTF8);
        }

        /// Strips spaces, punctuation, and invalid characters for C# identifiers.
        public static string SanitizeIdentifier(string raw){
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return raw.Replace(" ", "").Replace("\"", "").Replace("'", "").Replace("-", "").Trim();
        }

        /// Parses a CSV row taking quotes and commas into account.
        public static List<string> ParseCsvRow(string line){
            List<string> result = new();
            bool inQuotes = false;
            StringBuilder current = new();

            for (int i = 0; i < line.Length; i++){
                char c = line[i];
                if (c == '"'){
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"'){
                        current.Append('"');
                        i++;
                    }
                    else{
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes){
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else{
                    current.Append(c);
                }
            }
            result.Add(current.ToString().Trim());
            return result;
        }
    }
}
