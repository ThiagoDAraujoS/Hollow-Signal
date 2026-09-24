using System;
using System.Collections.Generic;
using System.IO;
using Editor.Dialog.Emitter;
using Editor.Dialog.Parser;
using UnityEditor;
using UnityEngine;

namespace Editor.Dialog{
    /// Coordinates the full compilation lifecycle of .dialog source files:
    /// Parsing -> AST -> Map Sync -> C# Generation -> Batched Localization Generation.
    public static class DialogBaker{
        /// Bakes an individual .dialog file located at project-relative assetPath.
        public static bool BakeFile(string assetPath){
            if (!File.Exists(assetPath))
                throw new FileNotFoundException($"Dialog source file not found: {assetPath}");

            string scriptName = Path.GetFileNameWithoutExtension(assetPath);
            string scriptContent = File.ReadAllText(assetPath);

            try{
                // 1. Parse into AST
                DialogScriptAst ast = DialogParser.Parse(scriptName, scriptContent);

                // 2. Synchronize Map Variables into <MapName>Variables.cs
                string mapSyncPath = DialogMapVariableSynchronizer.Synchronize(ast);

                // 3. Emit Localization txt
                string locPath = DialogLocEmitter.Emit(ast, "en");

                // 4. Emit C# DialogueBehaviour class
                string codePath = DialogCodeEmitter.Emit(ast);

                string mapLog = mapSyncPath != null ? $" [MAP: {mapSyncPath}]" : "";
                Debug.Log($"<color=#5ce1e6><b>[DialogBaker]</b></color> Successfully baked '<b>{scriptName}</b>' -> [CS: {codePath}] [LOC: {locPath}]{mapLog}");
                return true;
            }
            catch (Exception ex){
                Debug.LogError($"<color=#ff4d4d><b>[DialogBaker] Compilation Error in '{scriptName}':</b></color>\n{ex.Message}");
                return false;
            }
        }

        /// Bakes a collection of .dialog file paths in batch.
        public static void BakeFiles(IReadOnlyList<string> dialogPaths){
            if (dialogPaths == null || dialogPaths.Count == 0){
                Debug.LogWarning("[DialogBaker] No .dialog files specified to bake.");
                return;
            }

            List<DialogScriptAst> parsedAsts = new();
            int failed = 0;

            // Step 1: Parse all files into ASTs
            foreach (string path in dialogPaths){
                string scriptName = Path.GetFileNameWithoutExtension(path);
                try{
                    string content = File.ReadAllText(path);
                    DialogScriptAst ast = DialogParser.Parse(scriptName, content);
                    parsedAsts.Add(ast);
                }
                catch (Exception ex){
                    Debug.LogError($"<color=#ff4d4d><b>[DialogBaker] Error parsing '{scriptName}':</b></color>\n{ex.Message}");
                    failed++;
                }
            }

            if (parsedAsts.Count == 0){
                Debug.LogWarning("[DialogBaker] None of the specified dialogues could be parsed successfully.");
                return;
            }

            // Step 2: Batch Emit Localization files
            var locBatchResults = DialogLocEmitter.EmitBatch(parsedAsts, "en");
            foreach (var kvp in locBatchResults){
                Debug.Log($"<color=#5ce1e6><b>[DialogBaker]</b></color> Batched {kvp.Value} dialog(s) into LOC: '<b>{Path.GetFileName(kvp.Key)}</b>'");
            }

            // Step 3: Synchronize Map variables and emit C# classes
            int csSuccess = 0;
            foreach (DialogScriptAst ast in parsedAsts){
                try{
                    DialogMapVariableSynchronizer.Synchronize(ast);
                    DialogCodeEmitter.Emit(ast);
                    csSuccess++;
                }
                catch (Exception ex){
                    Debug.LogError($"<color=#ff4d4d><b>[DialogBaker] Error generating code for '{ast.scriptName}':</b></color>\n{ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"<color=#00ff88><b>[DialogBaker]</b></color> Batch bake complete: {csSuccess}/{dialogPaths.Count} dialogues generated across {locBatchResults.Count} localization table(s). Failed: {failed}");
        }

        /// Scans the entire project for .dialog files and bakes all of them in batches.
        [MenuItem("Tools/CRPG/Bake All Dialogues")]
        public static void BakeAllDialogues(){
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            List<string> dialogPaths = new();

            foreach (string guid in guids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".dialog", StringComparison.OrdinalIgnoreCase))
                    dialogPaths.Add(path);
            }

            if (dialogPaths.Count == 0){
                Debug.LogWarning("[DialogBaker] No .dialog files found in Assets folder.");
                return;
            }

            BakeFiles(dialogPaths);
        }

        /// Bakes all .dialog files currently selected in Unity's Project view.
        [MenuItem("Tools/CRPG/Bake Selected Dialogues")]
        [MenuItem("Assets/Bake Selected Dialogue(s)", false, 20)]
        public static void BakeSelectedDialogues(){
            List<string> selectedPaths = GetSelectedDialogPaths();
            if (selectedPaths.Count == 0){
                EditorUtility.DisplayDialog("Dialogue Baker", "No .dialog files or folders containing .dialog files are currently selected in the Project view.\n\nPlease select one or more .dialog files or use 'Tools > CRPG > Dialogue Baker Window' to choose files.", "OK");
                return;
            }

            BakeFiles(selectedPaths);
        }

        [MenuItem("Assets/Bake Selected Dialogue(s)", true)]
        public static bool ValidateBakeSelectedDialogues(){
            return GetSelectedDialogPaths().Count > 0;
        }

        /// Collects all .dialog file paths from the current Editor selection.
        public static List<string> GetSelectedDialogPaths(){
            List<string> dialogPaths = new();
            foreach (string guid in Selection.assetGUIDs){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".dialog", StringComparison.OrdinalIgnoreCase)){
                    if (!dialogPaths.Contains(path))
                        dialogPaths.Add(path);
                }
                else if (Directory.Exists(path)){
                    string[] files = Directory.GetFiles(path, "*.dialog", SearchOption.AllDirectories);
                    foreach (string file in files){
                        string normalized = file.Replace('\\', '/');
                        if (!dialogPaths.Contains(normalized))
                            dialogPaths.Add(normalized);
                    }
                }
            }
            return dialogPaths;
        }
    }
}
