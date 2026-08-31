using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor.DataBakers{
    /// Unity Editor Tool to parse raw CSV databases and generate or update the compile-safe Skill Enum file.
    public static class SkillsImporter{
        private const string
            DefaultCsvPath      = "Assets/StreamingAssets/skills.csv",
            SkillEnumScriptPath = "Assets/Scripts/Core/Data/Skill.cs";

        [MenuItem("Tools/CRPG/Import Skills Enum")]
        public static void ImportSkillsCsv(){
            string csvPath = DefaultCsvPath;
            if (!File.Exists(csvPath)){
                // Fallback: Show file panel if not found at default location
                csvPath = EditorUtility.OpenFilePanel("Select Skills CSV File", "Assets", "csv");
                if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath)){
                    Debug.LogError($"[SkillsImporter] Skills CSV file not found at: {DefaultCsvPath}");
                    return;
                }
            }

            List<string> skills = ParseSkillsFromCsv(csvPath);
            if (skills == null || skills.Count == 0){
                Debug.LogError("[SkillsImporter] No skills found or parsed from CSV.");
                return;
            }

            GenerateSkillEnumFile(skills, SkillEnumScriptPath);
            AssetDatabase.Refresh();
            Debug.Log($"[SkillsImporter] Successfully generated Skill Enum script with {skills.Count} values.");
        }

        private static List<string> ParseSkillsFromCsv(string csvPath){
            List<string>    skillNames   = new List<string>();
            HashSet<string> uniqueSkills = new HashSet<string>();

            try{
                string[] lines = File.ReadAllLines(csvPath);
                foreach (string line in lines){
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    string rawSkillName = parts[1].Trim();
                    rawSkillName = rawSkillName.Replace("\"", "").Replace("'", "").Replace(" ", "").Trim();
                    if (string.IsNullOrEmpty(rawSkillName)) continue;
                    if (uniqueSkills.Add(rawSkillName))
                        skillNames.Add(rawSkillName);
                }
            }
            catch (Exception ex){
                Debug.LogError($"[SkillsImporter] Error reading skills CSV file: {ex.Message}");
                return null;
            }
            return skillNames;
        }

        private static void GenerateSkillEnumFile(List<string> skills, string targetPath){
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            StringBuilder code = new();
            code.AppendLine("namespace Core.Data");
            code.AppendLine("{");
            code.AppendLine("    /// Auto-generated Skill enum mapping to the spreadsheet skills list database.");
            code.AppendLine("    public enum Skill");
            code.AppendLine("    {");
            code.AppendLine("        None = 0,");

            for (int i = 0; i < skills.Count; i++)
                code.AppendLine($"        {skills[i]} = {i + 1},");
            code.AppendLine("    }");
            code.AppendLine("}");

            File.WriteAllText(targetPath, code.ToString(), Encoding.UTF8);
        }
    }
}