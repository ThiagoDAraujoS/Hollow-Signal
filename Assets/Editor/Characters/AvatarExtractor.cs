using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Editor.Characters{
    public static class AvatarExtractor{
        public enum PromptResult{
            Yes,
            YesToAll,
            No,
            NoToAll
        }

        private class AvatarModalDialog : EditorWindow{
            public  PromptResult Result   = PromptResult.No;
            private string       _heading = "";
            private string       _body    = "";
            private bool         _allowAll;

            public static PromptResult ShowDialog(string windowTitle, string heading, string body, bool allowAll){
                if (!allowAll)
                    return EditorUtility.DisplayDialog(windowTitle, $"{heading}\n\n{body}", "Yes", "No")
                        ? PromptResult.Yes
                        : PromptResult.No;

                var win = CreateInstance<AvatarModalDialog>();
                win.titleContent = new GUIContent(windowTitle);
                win._heading     = heading;
                win._body        = body;
                win._allowAll    = allowAll;
                win.minSize      = new Vector2(500, 220);
                win.maxSize      = new Vector2(500, 220);
                win.ShowModalUtility();
                return win.Result;
            }

            private void OnGUI(){
                EditorGUILayout.Space(12);
                EditorGUILayout.LabelField(_heading, EditorStyles.boldLabel);
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(_body, MessageType.None);
                EditorGUILayout.Space(20);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new Color(0.35f, 0.75f, 0.35f);
                if (GUILayout.Button("Yes", GUILayout.Width(90), GUILayout.Height(28))){
                    Result = PromptResult.Yes;
                    Close();
                }

                if (_allowAll && GUILayout.Button("Yes for All", GUILayout.Width(100), GUILayout.Height(28))){
                    Result = PromptResult.YesToAll;
                    Close();
                }

                GUI.backgroundColor = new Color(0.85f, 0.45f, 0.45f);
                if (GUILayout.Button("No", GUILayout.Width(90), GUILayout.Height(28))){
                    Result = PromptResult.No;
                    Close();
                }

                if (_allowAll && GUILayout.Button("No for All", GUILayout.Width(100), GUILayout.Height(28))){
                    Result = PromptResult.NoToAll;
                    Close();
                }

                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        [MenuItem("Assets/Characters/Convert FBX to Standalone AVT Avatar",         false, 20)]
        [MenuItem("Tools/Characters/Convert Selected FBX to Standalone AVT Avatar", false, 10)]
        public static void ConvertSelectedFbxAvatars(){
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0){
                EditorUtility.DisplayDialog("No Selection", "Please select one or more FBX model files in the Project window.", "OK");
                return;
            }

            var fbxPaths = new List<string>();
            foreach (var obj in selectedObjects){
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    fbxPaths.Add(assetPath);
            }

            if (fbxPaths.Count == 0){
                EditorUtility.DisplayDialog("No FBX Selected", "None of the selected items are FBX files.", "OK");
                return;
            }

            bool          hasMultiple        = fbxPaths.Count > 1;
            PromptResult? globalCreateChoice = null;
            PromptResult? globalDeleteChoice = null;
            int           processedCount     = 0;

            foreach (string fbxPath in fbxPaths){
                bool allowAllNow = hasMultiple && processedCount < fbxPaths.Count - 1;
                ProcessSingleFbx(fbxPath, allowAllNow, ref globalCreateChoice, ref globalDeleteChoice);
                processedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", $"Finished processing {processedCount} FBX file(s).", "OK");
        }

        [MenuItem("Assets/Characters/Convert FBX to Standalone AVT Avatar", true)]
        public static bool ValidateConvertSelectedFbxAvatars() =>
            Selection.objects != null &&
            Selection.objects.Any(obj => {
                string p = AssetDatabase.GetAssetPath(obj);
                return !string.IsNullOrEmpty(p) && p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
            });

        private static void ProcessSingleFbx(
            string            fbxPath,
            bool              allowAll,
            ref PromptResult? globalCreateChoice,
            ref PromptResult? globalDeleteChoice){
            string        fileName = Path.GetFileNameWithoutExtension(fbxPath);
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            string dir = Path.GetDirectoryName(fbxPath).Replace("\\", "/");
            string targetFolder = dir.EndsWith("/Models", StringComparison.OrdinalIgnoreCase)
                ? $"{Path.GetDirectoryName(dir).Replace("\\", "/")}/Avatars"
                : $"{dir}/Avatars";

            if (!Directory.Exists(targetFolder)){
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            string avtBaseName = fileName.StartsWith("FBX_", StringComparison.OrdinalIgnoreCase)
                ? "AVT_" + fileName.Substring(4)
                : fileName.StartsWith("FBX", StringComparison.OrdinalIgnoreCase)
                    ? "AVT_" + fileName.Substring(3)
                    : "AVT_" + fileName;

            string avtAssetPath = $"{targetFolder}/{avtBaseName}.asset";

            bool     hasInternalAvatar = false;
            Object[] subAssets         = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var sub in subAssets)
                if (sub is Avatar){
                    hasInternalAvatar = true;
                    break;
                }

            bool shouldCreateAvt;
            if (globalCreateChoice.HasValue)
                shouldCreateAvt = globalCreateChoice.Value == PromptResult.YesToAll;
            else{
                PromptResult res = AvatarModalDialog.ShowDialog(
                    "Create AVT Standalone Avatar?",
                    $"Model: {fileName}",
                    $"Do you want to create a standalone avatar asset?\n\nTarget location:\n{avtAssetPath}",
                    allowAll
                );

                if (res == PromptResult.YesToAll || res == PromptResult.NoToAll)
                    globalCreateChoice = res;

                shouldCreateAvt = res == PromptResult.Yes || res == PromptResult.YesToAll;
            }

            Avatar finalStandaloneAvatar = null;

            if (shouldCreateAvt){
                if (importer.animationType != ModelImporterAnimationType.Human ||
                    importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel){
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.SaveAndReimport();
                }

                subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                Avatar sourceAvatar = null;
                foreach (var sub in subAssets)
                    if (sub is Avatar av && av.isValid){
                        sourceAvatar = av;
                        break;
                    }

                if (sourceAvatar != null){
                    Avatar newAvatar = Object.Instantiate(sourceAvatar);
                    newAvatar.name = avtBaseName;

                    if (File.Exists(avtAssetPath))
                        AssetDatabase.DeleteAsset(avtAssetPath);

                    AssetDatabase.CreateAsset(newAvatar, avtAssetPath);
                    AssetDatabase.SaveAssets();
                    finalStandaloneAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(avtAssetPath);
                    Debug.Log($"[AvatarExtractor] Created: {avtAssetPath}");
                }
                else
                    Debug.LogError($"[AvatarExtractor] Failed to generate humanoid avatar for {fileName}!");
            }
            else if (File.Exists(avtAssetPath))
                finalStandaloneAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(avtAssetPath);

            if (!hasInternalAvatar && finalStandaloneAvatar == null) return;

            bool shouldDeleteInternal;
            if (globalDeleteChoice.HasValue)
                shouldDeleteInternal = globalDeleteChoice.Value == PromptResult.YesToAll;
            else{
                string targetDesc = finalStandaloneAvatar != null
                    ? $"and set the rig to track '{avtBaseName}.asset'"
                    : "";

                PromptResult res = AvatarModalDialog.ShowDialog(
                    "Remove Internal Sub-Avatar?",
                    $"Model: {fileName}",
                    $"Internal avatar item detected inside this FBX.\n\nDo you want to delete the internal avatar {targetDesc}?\n\n(This prevents the avatar from polluting your FBX search results)",
                    allowAll
                );

                if (res == PromptResult.YesToAll || res == PromptResult.NoToAll)
                    globalDeleteChoice = res;

                shouldDeleteInternal = res == PromptResult.Yes || res == PromptResult.YesToAll;
            }

            if (!shouldDeleteInternal) return;

            if (finalStandaloneAvatar != null){
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup   = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar  = finalStandaloneAvatar;
                importer.SaveAndReimport();
                Debug.Log($"[AvatarExtractor] Switched {fileName} to CopyFromOther ({finalStandaloneAvatar.name}). Internal avatar removed!");
            }
            else{
                importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                importer.SaveAndReimport();
                Debug.Log($"[AvatarExtractor] Cleared internal avatar on {fileName}.");
            }
        }

        [MenuItem("Tools/Characters/Link Standalone Avatars in Active Scene", false, 20)]
        public static void LinkAvatarsInActiveScene(){
            var animators = Object.FindObjectsByType<Animator>();
            if (animators == null || animators.Length == 0){
                EditorUtility.DisplayDialog("No Animators", "No Animator components found in the active scene.", "OK");
                return;
            }

            string[] avatarGuids = AssetDatabase.FindAssets("AVT_ t:Avatar");
            var      avatarList  = new List<Avatar>();
            foreach (var guid in avatarGuids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var    av   = AssetDatabase.LoadAssetAtPath<Avatar>(path);
                if (av != null)
                    avatarList.Add(av);
            }

            int linkedCount = 0;

            foreach (var anim in animators){
                string goName = anim.gameObject.name;

                foreach (var av in avatarList){
                    string cleanName = av.name.Replace("AVT_MIXAMO_", "").Replace("AVT_", "");
                    if (goName.IndexOf(cleanName, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (anim.avatar == null || anim.avatar.name.IndexOf(cleanName, StringComparison.OrdinalIgnoreCase) < 0))
                        continue;

                    SerializedObject   so         = new SerializedObject(anim);
                    SerializedProperty avatarProp = so.FindProperty("m_Avatar");
                    if (avatarProp == null) break;

                    avatarProp.objectReferenceValue = av;
                    so.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(anim);
                    EditorUtility.SetDirty(anim);
                    linkedCount++;
                    Debug.Log($"[AvatarExtractor] Linked {av.name} to {anim.gameObject.name}");
                    break;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Linking Complete",
                                        $"Linked {linkedCount} Animator(s) in the active scene to their standalone AVT avatars!", "OK");
        }
    }
}
