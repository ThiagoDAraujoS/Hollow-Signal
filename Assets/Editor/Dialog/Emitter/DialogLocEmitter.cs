using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Editor.Dialog.Parser;

namespace Editor.Dialog.Emitter{
    /// Generates standard key-value localization text files matching the project's LocalizationManager format.
    /// Supports routing via LOC_FILE: <FileName> to batch multiple dialogues into the same file,
    /// with fallback to MAP: <MapName> (in Scenes/ folder) or script name (in root).
    public static class DialogLocEmitter{
        private const string BaseLocalizationFolder = "Assets/StreamingAssets/Localization";

        /// Generates the localization file for a single AST.
        /// If the file already exists, cleanly replaces only this script's section to prevent duplicate keys.
        public static string Emit(DialogScriptAst ast, string language = "en", string targetDirectory = null){
            ResolveOutputLocation(ast, language, targetDirectory, out string folder, out string fileName);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fullPath = Path.Combine(folder, fileName);
            StringBuilder scriptContentSb = new();
            AppendAstStrings(scriptContentSb, ast);
            string scriptSection = scriptContentSb.ToString().TrimEnd();

            string headerTag = $"# >>> [Script: {ast.scriptName}.dialog] <<<";

            if (File.Exists(fullPath)){
                string existingContent = File.ReadAllText(fullPath);
                
                // If section already exists in file, replace it cleanly
                string pattern = $@"{Regex.Escape(headerTag)}[\s\S]*?(?=(# >>> \[Script:|$))";
                if (Regex.IsMatch(existingContent, pattern)){
                    string updated = Regex.Replace(existingContent, pattern, scriptSection + "\n\n");
                    File.WriteAllText(fullPath, updated.TrimEnd() + "\n", Encoding.UTF8);
                    return fullPath;
                }

                // Otherwise append to existing file
                StringBuilder sb = new(existingContent);
                if (!existingContent.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine(scriptSection);
                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            }
            else{
                StringBuilder sb = new();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                sb.AppendLine($"# --- Auto-Generated Dialogue Localization Keys ---");
                sb.AppendLine($"# Target: {Path.GetFileNameWithoutExtension(fileName)}");
                sb.AppendLine($"# Generated on: {timestamp}");
                sb.AppendLine();
                sb.AppendLine(scriptSection);
                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            }

            return fullPath;
        }

        /// Batches a collection of parsed ASTs grouped by their resolved target localization file.
        /// Rewrites each target file cleanly in a single pass with no duplicate headers.
        public static Dictionary<string, int> EmitBatch(IEnumerable<DialogScriptAst> astList, string language = "en"){
            Dictionary<string, List<DialogScriptAst>> grouped = new(StringComparer.OrdinalIgnoreCase);

            foreach (DialogScriptAst ast in astList){
                ResolveOutputLocation(ast, language, null, out string folder, out string fileName);
                string fullPath = Path.Combine(folder, fileName);

                if (!grouped.TryGetValue(fullPath, out var list)){
                    list = new List<DialogScriptAst>();
                    grouped[fullPath] = list;
                }
                list.Add(ast);
            }

            Dictionary<string, int> results = new();

            foreach (var kvp in grouped){
                string fullPath = kvp.Key;
                List<DialogScriptAst> scripts = kvp.Value;

                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                StringBuilder sb = new();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                sb.AppendLine($"# --- Auto-Generated Dialogue Localization Keys ---");
                sb.AppendLine($"# Target: {Path.GetFileNameWithoutExtension(fullPath)}");
                sb.AppendLine($"# Batched scripts: {scripts.Count}");
                sb.AppendLine($"# Generated on: {timestamp}");
                sb.AppendLine();

                foreach (DialogScriptAst ast in scripts){
                    AppendAstStrings(sb, ast);
                    sb.AppendLine();
                }

                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
                results[fullPath] = scripts.Count;
            }

            return results;
        }

        private static void ResolveOutputLocation(DialogScriptAst ast, string language, string targetDirectory, out string folder, out string fileName){
            if (!string.IsNullOrEmpty(targetDirectory)){
                folder = targetDirectory;
                string baseName = !string.IsNullOrEmpty(ast.locFileName) ? ast.locFileName : ast.scriptName;
                fileName = $"{baseName.ToLowerInvariant()}_{language}.txt";
            }
            else if (!string.IsNullOrEmpty(ast.locFileName)){
                folder = BaseLocalizationFolder;
                fileName = $"{ast.locFileName.ToLowerInvariant()}_{language}.txt";
            }
            else if (!string.IsNullOrEmpty(ast.mapName)){
                folder = Path.Combine(BaseLocalizationFolder, "Scenes");
                fileName = $"{ast.mapName.ToLowerInvariant()}_{language}.txt";
            }
            else{
                folder = BaseLocalizationFolder;
                fileName = $"{ast.scriptName.ToLowerInvariant()}_{language}.txt";
            }
        }

        private static void AppendAstStrings(StringBuilder sb, DialogScriptAst ast){
            sb.AppendLine($"# >>> [Script: {ast.scriptName}.dialog] <<<");
            string fileSlug = SanitizeKeySlug(ast.scriptName);

            foreach (DialogKnotDef knot in ast.knots){
                string knotSlug = SanitizeKeySlug(knot.knotId);
                bool hasHeader = false;

                // 1. Spoken Prompt Line
                if (!string.IsNullOrEmpty(knot.promptText)){
                    EnsureKnotHeader(sb, ref hasHeader, knot.knotId);
                    string promptKey = $"DLG_{fileSlug}_{knotSlug}_PROMPT_00";
                    sb.AppendLine($"{promptKey} = \"{EscapeString(knot.promptText)}\"");
                }

                // 2. Choices
                for (int i = 0; i < knot.choices.Count; i++){
                    DialogChoiceDef choice = knot.choices[i];
                    if (string.IsNullOrEmpty(choice.text)) continue;

                    EnsureKnotHeader(sb, ref hasHeader, knot.knotId);
                    string choiceKey = $"DLG_{fileSlug}_{knotSlug}_CHOICE_{i:D2}";
                    sb.AppendLine($"{choiceKey} = \"{EscapeString(choice.text)}\"");
                }

                // 3. Skill Check Outcomes
                if (knot.skillCheck != null){
                    AppendOutcomeLine(sb, ref hasHeader, knot.knotId, fileSlug, knotSlug, "SUCCESS", knot.skillCheck.onSuccess);
                    AppendOutcomeLine(sb, ref hasHeader, knot.knotId, fileSlug, knotSlug, "FAILURE", knot.skillCheck.onFailure);
                    AppendOutcomeLine(sb, ref hasHeader, knot.knotId, fileSlug, knotSlug, "CRITICAL_SUCCESS", knot.skillCheck.onCriticalSuccess);
                    AppendOutcomeLine(sb, ref hasHeader, knot.knotId, fileSlug, knotSlug, "CRITICAL_FAILURE", knot.skillCheck.onCriticalFailure);
                }

                if (hasHeader)
                    sb.AppendLine();
            }
        }

        private static void AppendOutcomeLine(StringBuilder sb, ref bool hasHeader, string knotId, string fileSlug, string knotSlug, string outcomeName, DialogOutcomeDef outcome){
            if (outcome == null || string.IsNullOrEmpty(outcome.text)) return;
            EnsureKnotHeader(sb, ref hasHeader, knotId);
            string key = $"DLG_{fileSlug}_{knotSlug}_{outcomeName}_00";
            sb.AppendLine($"{key} = \"{EscapeString(outcome.text)}\"");
        }

        private static void EnsureKnotHeader(StringBuilder sb, ref bool hasHeader, string knotId){
            if (hasHeader) return;
            sb.AppendLine($"# [Knot: {knotId}]");
            hasHeader = true;
        }

        private static string EscapeString(string raw){
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return raw.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        }

        private static string SanitizeKeySlug(string raw){
            if (string.IsNullOrEmpty(raw)) return "KEY";
            return raw.ToUpperInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }
}
