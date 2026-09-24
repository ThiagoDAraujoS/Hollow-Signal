using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.Dialog{
    /// Interactive editor window allowing developers to inspect, multi-select, and batch-bake specific .dialog files.
    public class DialogueBakerWindow : EditorWindow{
        private class DialogEntry{
            public string path;
            public string scriptName;
            public bool isSelected = true;
        }

        private readonly List<DialogEntry> entries = new();
        private Vector2 scrollPos;
        private string searchFilter = "";

        [MenuItem("Tools/CRPG/Dialogue Baker Window...", false, 1)]
        public static void OpenWindow(){
            DialogueBakerWindow window = GetWindow<DialogueBakerWindow>("Dialogue Baker");
            window.minSize = new Vector2(420, 360);
            window.RefreshList();
            window.Show();
        }

        private void OnEnable(){
            RefreshList();
        }

        private void RefreshList(){
            entries.Clear();
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            foreach (string guid in guids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".dialog", StringComparison.OrdinalIgnoreCase)){
                    entries.Add(new DialogEntry{
                        path = path,
                        scriptName = Path.GetFileNameWithoutExtension(path),
                        isSelected = true
                    });
                }
            }
        }

        private void OnGUI(){
            DrawHeader();
            DrawToolbar();
            DrawFileList();
            DrawFooter();
        }

        private void DrawHeader(){
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)){
                EditorGUILayout.LabelField("<b>Dialogue Compiler & Baker</b>\nSelect files to compile into C# and localization tables.", new GUIStyle(EditorStyles.label) { richText = true });
            }
            EditorGUILayout.Space(4);
        }

        private void DrawToolbar(){
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)){
                if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(75))){
                    foreach (var entry in entries) entry.isSelected = true;
                }
                if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton, GUILayout.Width(85))){
                    foreach (var entry in entries) entry.isSelected = false;
                }
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65))){
                    RefreshList();
                }

                GUILayout.FlexibleSpace();
                searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(160));
                if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton)){
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawFileList(){
            if (entries.Count == 0){
                EditorGUILayout.HelpBox("No '.dialog' files found in Assets folder.", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var entry in entries){
                if (!string.IsNullOrEmpty(searchFilter) && !entry.scriptName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase).ToString().Equals("-1", StringComparison.Ordinal) && !entry.scriptName.ToLowerInvariant().Contains(searchFilter.ToLowerInvariant()))
                    continue;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)){
                    entry.isSelected = EditorGUILayout.Toggle(entry.isSelected, GUILayout.Width(22));

                    EditorGUILayout.LabelField(entry.scriptName, EditorStyles.boldLabel, GUILayout.Width(180));
                    EditorGUILayout.LabelField(entry.path, EditorStyles.miniLabel);

                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45))){
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.path);
                        if (obj != null) EditorGUIUtility.PingObject(obj);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawFooter(){
            EditorGUILayout.Space(4);
            int selectedCount = 0;
            foreach (var e in entries) if (e.isSelected) selectedCount++;

            EditorGUILayout.LabelField($"Selected: {selectedCount} of {entries.Count} dialog file(s)", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope()){
                GUI.enabled = selectedCount > 0;
                if (GUILayout.Button($"Bake Selected ({selectedCount})", GUILayout.Height(32))){
                    List<string> targets = new();
                    foreach (var e in entries){
                        if (e.isSelected) targets.Add(e.path);
                    }
                    DialogBaker.BakeFiles(targets);
                }
                GUI.enabled = true;

                if (GUILayout.Button("Bake All", GUILayout.Height(32), GUILayout.Width(90))){
                    DialogBaker.BakeAllDialogues();
                }
            }
            EditorGUILayout.Space(6);
        }
    }
}
