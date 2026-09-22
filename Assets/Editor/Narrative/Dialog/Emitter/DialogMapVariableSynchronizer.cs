using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Editor.Dialog.Parser;
using UnityEngine;

namespace Editor.Dialog.Emitter{
    /// Synchronizes map-wide variables declared in .dialog files with the concrete MapDialogVariables C# script.
    public static class DialogMapVariableSynchronizer{
        private const string DefaultMapScriptsFolder = "Assets/Scripts/Generated/Maps";

        /// Inspects the AST for 'VAR map' declarations and synchronizes them into <MapName>Variables.cs.
        public static string Synchronize(DialogScriptAst ast, string targetFolder = null){
            if (string.IsNullOrEmpty(ast.mapName))
                return null;

            var mapVars = ast.variables.FindAll(v => v.scope == "map");
            if (mapVars.Count == 0)
                return null;

            string folder = string.IsNullOrEmpty(targetFolder) ? DefaultMapScriptsFolder : targetFolder;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string className = $"{SanitizeIdentifier(ast.mapName)}Variables";
            string filePath = Path.Combine(folder, $"{className}.cs");

            if (!File.Exists(filePath)){
                CreateNewMapClass(filePath, className, ast.mapName, mapVars);
                Debug.Log($"<color=#5ce1e6><b>[DialogMapSync]</b></color> Created new map variables file: '<b>{className}.cs</b>'");
            }
            else
                AppendMissingVariables(filePath, className, mapVars);

            return filePath;
        }

        /// Generates a new MapDialogVariables partial class file for a map scene.
        private static void CreateNewMapClass(string filePath, string className, string mapName, System.Collections.Generic.List<DialogVarDef> mapVars){
            StringBuilder sb = new();
            sb.AppendLine("using Core;");
            sb.AppendLine("using Core.State;");
            sb.AppendLine("using Narrative.Dialog;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace Generated.Maps{");
            sb.AppendLine($"    /// Concrete map-wide dialogue variables for the '{mapName}' level scene.");
            sb.AppendLine($"    public class {className} : MapDialogVariables{{");

            foreach (DialogVarDef varDef in mapVars){
                string csType = NormalizeType(varDef.typeName);
                string defaultVal = FormatDefaultValue(varDef.typeName, varDef.defaultValue);
                sb.AppendLine($"        public Tracked<{csType}> {varDef.name} = new(\"{varDef.name}\", {defaultVal});");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// Appends undeclared Tracked fields into an existing MapDialogVariables class file.
        private static void AppendMissingVariables(string filePath, string className, System.Collections.Generic.List<DialogVarDef> mapVars){
            string fileContent = File.ReadAllText(filePath);
            StringBuilder newVarsBuilder = new();
            int appendedCount = 0;

            foreach (DialogVarDef varDef in mapVars){
                Regex fieldRegex = new($@"\b{varDef.name}\b");
                if (!fieldRegex.IsMatch(fileContent)){
                    string csType = NormalizeType(varDef.typeName);
                    string defaultVal = FormatDefaultValue(varDef.typeName, varDef.defaultValue);
                    newVarsBuilder.AppendLine($"        public Tracked<{csType}> {varDef.name} = new(\"{varDef.name}\", {defaultVal});");
                    appendedCount++;
                }
            }

            if (appendedCount == 0)
                return;

            int lastBraceIdx = fileContent.LastIndexOf('}');
            if (lastBraceIdx > 0){
                int secondToLastBrace = fileContent.LastIndexOf('}', lastBraceIdx - 1);
                int insertionPoint = secondToLastBrace > 0 ? secondToLastBrace : lastBraceIdx;

                string before = fileContent.Substring(0, insertionPoint);
                string after = fileContent.Substring(insertionPoint);

                string updatedContent = $"{before}\n{newVarsBuilder}{after}";
                File.WriteAllText(filePath, updatedContent, Encoding.UTF8);
                Debug.Log($"<color=#5ce1e6><b>[DialogMapSync]</b></color> Appended <b>{appendedCount}</b> new variable(s) to '<b>{className}.cs</b>'");
            }
        }

        /// Maps DSL primitive type strings to C# types.
        private static string NormalizeType(string typeName) => typeName switch{
            "bool" => "bool",
            "int" => "int",
            "float" => "float",
            "string" => "string",
            _ => typeName
        };

        /// Formats initial variable values into C# literal syntax.
        private static string FormatDefaultValue(string typeName, string rawDefault){
            if (string.IsNullOrEmpty(rawDefault))
                return typeName == "string" ? "\"\"" : "default";
            if (typeName == "string" && !rawDefault.StartsWith("\""))
                return $"\"{rawDefault}\"";
            if (typeName == "float" && !rawDefault.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                return $"{rawDefault}f";
            return rawDefault;
        }

        /// Sanitizes raw map name string into a valid C# identifier.
        private static string SanitizeIdentifier(string raw) => string.IsNullOrEmpty(raw) ? "Map" : raw.Replace(" ", "_").Replace("-", "_");
    }
}
