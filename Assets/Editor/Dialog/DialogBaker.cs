using System;
using System.IO;
using Editor.Dialog.Emitter;
using Editor.Dialog.Parser;
using UnityEditor;
using UnityEngine;

namespace Editor.Dialog{
    /// Coordinates the full compilation lifecycle of .dialog source files:
    /// Parsing -> AST -> Map Sync -> C# Generation -> Localization Generation.
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
                string mapSyncPath = Editor.Dialog.Emitter.DialogMapVariableSynchronizer.Synchronize(ast);

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

        /// Scans the entire project for .dialog files and bakes all of them.
        [MenuItem("Tools/CRPG/Bake All Dialogues")]
        public static void BakeAllDialogues(){
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            int total = 0;
            int succeeded = 0;

            foreach (string guid in guids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".dialog", StringComparison.OrdinalIgnoreCase)){
                    total++;
                    if (BakeFile(path))
                        succeeded++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"<color=#00ff88><b>[DialogBaker]</b></color> Completed batch bake: {succeeded}/{total} dialogues compiled.");
        }
    }
}
