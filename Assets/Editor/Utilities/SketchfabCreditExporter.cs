using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HollowSignal.Editor.Utilities
{
    [Serializable]
    public class SketchfabCreditData
    {
        public string assetName;
        public string author;
        public string authorUrl;
        public string sourceUrl;
        public string license;
        public string modifications;
        public string category;
        public string dateAdded;
    }

    public static class SketchfabCreditExporter
    {
        private const string RootFolderPath = "Assets/GameArt/Sketchfab";
        private const string OutputMarkdownPath = "Assets/GameArt/Sketchfab/CREDITS_ALL.md";
        private const string CreditFileName = "TXT_Credit.json";

        /// Exports all TXT_Credit.json files into a single master markdown file.
        [MenuItem("Tools/Sketchfab Credits/Export All Credits to File")]
        public static void ExportCreditsToFile()
        {
            List<SketchfabCreditData> credits = CollectAllCredits();
            string content = BuildCreditsMarkdown(credits);
            string fullOutputPath = Path.Combine(Application.dataPath, OutputMarkdownPath.Substring("Assets/".Length));
            
            File.WriteAllText(fullOutputPath, content, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Sketchfab Credits", $"Exported {credits.Count} credits to {OutputMarkdownPath}", "OK");
        }

        /// Copies compiled markdown credits directly to system clipboard.
        [MenuItem("Tools/Sketchfab Credits/Copy All Credits to Clipboard")]
        public static void CopyCreditsToClipboard()
        {
            GUIUtility.systemCopyBuffer = BuildCreditsMarkdown(CollectAllCredits());
            EditorUtility.DisplayDialog("Sketchfab Credits", "Copied credits to clipboard.", "OK");
        }

        /// Creates a new TXT_Credit.json template in the active Project folder.
        [MenuItem("Assets/Create/Sketchfab Credit File", false, 80)]
        public static void CreateCreditFileInSelectedFolder()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!Directory.Exists(selectedPath))
                selectedPath = Path.GetDirectoryName(selectedPath);

            string targetFile = Path.Combine(selectedPath, CreditFileName);
            var template = new SketchfabCreditData
            {
                assetName = Path.GetFileName(selectedPath),
                author = "Author Name",
                authorUrl = "https://sketchfab.com/author_username",
                sourceUrl = "https://sketchfab.com/3d-models/...",
                license = "CC-BY-4.0",
                modifications = "Optimized mesh, converted textures to URP Lit.",
                category = "Props",
                dateAdded = DateTime.Now.ToString("yyyy-MM-dd")
            };

            File.WriteAllText(targetFile, JsonUtility.ToJson(template, true), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetFile));
        }

        /// Collects and deserializes all TXT_Credit.json files found under the root folder.
        private static List<SketchfabCreditData> CollectAllCredits()
        {
            var creditsList = new List<SketchfabCreditData>();
            string fullRoot = Path.Combine(Application.dataPath, RootFolderPath.Substring("Assets/".Length));
            string[] foundFiles = Directory.GetFiles(fullRoot, CreditFileName, SearchOption.AllDirectories);

            foreach (string file in foundFiles)
            {
                if (file.Replace('\\', '/').Contains("/_Template/"))
                    continue;

                creditsList.Add(JsonUtility.FromJson<SketchfabCreditData>(File.ReadAllText(file, Encoding.UTF8)));
            }

            creditsList.Sort((a, b) => string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase));
            return creditsList;
        }

        /// Builds formatted markdown text from credits collection.
        private static string BuildCreditsMarkdown(List<SketchfabCreditData> credits)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Third-Party 3D Assets & Attributions (Sketchfab)\n");
            
            string currentCategory = null;
            foreach (var item in credits)
            {
                if (item.category != currentCategory)
                {
                    currentCategory = item.category;
                    sb.AppendLine($"## {currentCategory}\n");
                }

                sb.AppendLine($"- **[{item.assetName}]({item.sourceUrl})** by [{item.author}]({item.authorUrl})");
                sb.AppendLine($"  - **License:** {item.license}");
                sb.AppendLine($"  - **Modifications:** {item.modifications}");
                sb.AppendLine($"  - **Added:** {item.dateAdded}\n");
            }

            sb.AppendLine("---\n### Plain-Text Summary");
            sb.AppendLine("```text");
            foreach (var item in credits)
                sb.AppendLine($"\"{item.assetName}\" by {item.author} ({item.license})");
            sb.AppendLine("```");

            return sb.ToString();
        }
    }
}
