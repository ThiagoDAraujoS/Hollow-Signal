using System;
using System.IO;
using System.Text;
using Editor.Dialog.Parser;

namespace Editor.Dialog.Emitter{
    /// Generates standard key-value localization text files matching the project's LocalizationManager format.
    /// If ast.mapName is defined, directs output into Assets/StreamingAssets/Localization/Scenes/<mapName>_<lang>.txt.
    /// Otherwise directs output into Assets/StreamingAssets/Localization/<scriptName>_<lang>.txt.
    public static class DialogLocEmitter{
        private const string BaseLocalizationFolder = "Assets/StreamingAssets/Localization";

        /// Generates the localization file for the given AST.
        /// Returns the path of the written file.
        public static string Emit(DialogScriptAst ast, string language = "en", string targetDirectory = null){
            string folder;
            string fileName;

            if (!string.IsNullOrEmpty(targetDirectory)){
                folder = targetDirectory;
                fileName = $"{ast.scriptName.ToLowerInvariant()}_{language}.txt";
            }
            else if (!string.IsNullOrEmpty(ast.mapName)){
                folder = Path.Combine(BaseLocalizationFolder, "Scenes");
                fileName = $"{ast.mapName}_{language}.txt";
            }
            else{
                folder = BaseLocalizationFolder;
                fileName = $"{ast.scriptName.ToLowerInvariant()}_{language}.txt";
            }

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fullPath = Path.Combine(folder, fileName);

            StringBuilder sb = new();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // If file exists (e.g. appending to a map's localization file), preserve or append
            if (File.Exists(fullPath)){
                string existingContent = File.ReadAllText(fullPath);
                sb.Append(existingContent);
                if (!existingContent.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }
            else{
                sb.AppendLine($"# --- Auto-Generated Dialogue Localization Keys ---");
                sb.AppendLine($"# Generated on: {timestamp}");
                sb.AppendLine();
            }

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

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
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
