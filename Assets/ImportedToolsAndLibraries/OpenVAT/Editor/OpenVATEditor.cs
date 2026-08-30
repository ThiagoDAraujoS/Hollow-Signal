// OpenVATEditor.cs
// Author: Luke Stilson

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#endif
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

public class OpenVATEditor : EditorWindow
{
    private string folderPath = "Assets/OpenVATContent";

    // --- Shader selection (predefined dropdown) ---
    private static readonly string[] SHADER_CHOICES = new[]
    {
        "Shader Graphs/openvat-advanced_packed",
        "Shader Graphs/openvat-advanced_separate",
        "Shader Graphs/openvat-advanced-pbr_packed",
        "Shader Graphs/openvat-advanced-pbr_separate",
        "Shader Graphs/openvat-simple",
        "Shader Graphs/openvat-simple-pbr",
        // more
    };
    private int selectedShaderIndex;
    private const string PREF_KEY_SHADER_INDEX = "OpenVATEditor.SelectedShaderIndex";

    [MenuItem("Tools/OpenVAT/OpenVAT Setup")]
    public static void ShowWindow()
    {
        GetWindow<OpenVATEditor>("OpenVAT Setup");
    }

    private void OnEnable()
    {
        selectedShaderIndex = EditorPrefs.GetInt(PREF_KEY_SHADER_INDEX, 0);
        if (selectedShaderIndex < 0 || selectedShaderIndex >= SHADER_CHOICES.Length)
            selectedShaderIndex = 0;
    }

    private void OnGUI()
    {
        GUILayout.Label("OpenVAT Content Importer", EditorStyles.boldLabel);

        // Folder path field (no Browse button)
        folderPath = EditorGUILayout.TextField("Folder or Zip Path", folderPath);
        EditorGUILayout.HelpBox("Tip: drag a folder or .zip onto this window, or paste a path above. Zips will be extracted automatically to Assets/OpenVATContentTemp when processed.", MessageType.Info);

        // Drag & drop support (retain prior 'browse' functionality without drawing the button)
        var evt = Event.current;
        var dropRect = GUILayoutUtility.GetLastRect();
        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropRect.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    // Prefer first dropped path
                    string p = DragAndDrop.paths[0];
                    if (!string.IsNullOrEmpty(p))
                    {
                        folderPath = p;
                        Repaint();
                    }
                }
            }
            Event.current.Use();
        }

        // Shader dropdown
        var shaderDisplay = SHADER_CHOICES
            .Select(s => s.StartsWith("Shader Graphs/") ? s.Substring("Shader Graphs/".Length) : s)
            .ToArray();
        selectedShaderIndex = EditorGUILayout.Popup("Material Shader", selectedShaderIndex, shaderDisplay);

        // Preview availability
        string chosenPath = SHADER_CHOICES[Mathf.Clamp(selectedShaderIndex, 0, SHADER_CHOICES.Length - 1)];
        Shader found = Shader.Find(chosenPath);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Found Shader", found, typeof(Shader), false);
        }

        if (GUILayout.Button("Process OpenVAT Content"))
        {
            EditorPrefs.SetInt(PREF_KEY_SHADER_INDEX, selectedShaderIndex);
            ProcessOpenVATContent();
        }
    }

    private static string ToAssetsRelative(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        string dataPath = Application.dataPath.Replace('\\', '/');
        path = path.Replace('\\', '/');
        if (path.StartsWith(dataPath))
            return "Assets" + path.Substring(dataPath.Length);
        return path; // external path or already relative
    }

    private string PrepareInputFolder(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        // If it's a zip, extract to a temp under Assets
        if (Path.GetExtension(input).ToLowerInvariant() == ".zip" && File.Exists(input))
        {
            string tempDir = "Assets/OpenVATContentTemp";
            if (Directory.Exists(tempDir))
                FileUtil.DeleteFileOrDirectory(tempDir);
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(input, tempDir);
            AssetDatabase.Refresh();
            return tempDir;
        }

        // If it's an absolute folder inside the project, convert to Assets-relative
        if (Directory.Exists(input))
        {
            string maybeAssets = ToAssetsRelative(input);
            return Directory.Exists(maybeAssets) ? maybeAssets : input;
        }

        // If it's already Assets-relative and exists, pass through
        if (input.StartsWith("Assets") && Directory.Exists(input))
            return input;

        Debug.LogError($"Invalid input path: {input}");
        return null;
    }

    private void EnsureModelWeldOff(string modelAssetPath)
    {
        var importer = AssetImporter.GetAtPath(modelAssetPath) as ModelImporter;
        if (importer != null)
        {
#if UNITY_2021_3_OR_NEWER
            // ModelImporter.weldVertices exists for FBX/OBJ and many model importers.
            // For glTF depending on importer, this may be ignored; we set it when available.
#endif
            if (importer.weldVertices != false)
            {
                importer.weldVertices = false;
                importer.SaveAndReimport();
            }
        }
        else
        {
            // Not a ModelImporter (possible for some custom glTF importers). Best-effort note.
            Debug.LogWarning($"Model importer not found or not a ModelImporter for: {modelAssetPath}. Could not force Weld Vertices OFF.");
        }
    }

    private void ProcessOpenVATContent()
    {
#if HAS_NEWTONSOFT_JSON
        // Normalize / prepare folder (handles .zip too)
        string preparedFolder = PrepareInputFolder(folderPath);
        if (string.IsNullOrEmpty(preparedFolder) || !Directory.Exists(preparedFolder))
        {
            Debug.LogError("Folder does not exist: " + preparedFolder);
            return;
        }

        // Collect model, textures, json
        string[] modelFiles = Directory.GetFiles(preparedFolder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".fbx") || f.EndsWith(".glb") || f.EndsWith(".gltf"))
            .Select(ToAssetsRelative) // ensure AssetDatabase can resolve
            .ToArray();

        string[] vatTextures = Directory.GetFiles(preparedFolder, "*_vat.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".png") || f.EndsWith(".exr"))
            .Select(ToAssetsRelative)
            .ToArray();

        string[] vnrmTextures = Directory.GetFiles(preparedFolder, "*_vnrm.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".png") || f.EndsWith(".exr"))
            .Select(ToAssetsRelative)
            .ToArray();

        string[] jsonFiles = Directory.GetFiles(preparedFolder, "*.json", SearchOption.TopDirectoryOnly)
            .Select(ToAssetsRelative)
            .ToArray();

        if (modelFiles.Length == 0 || vatTextures.Length == 0 || jsonFiles.Length == 0)
        {
            Debug.LogError("Required files are missing in the folder.");
            return;
        }

        string modelPath = modelFiles[0];
        string vatTexturePath = vatTextures[0];
        string vnrmTexturePath = vnrmTextures.Length > 0 ? vnrmTextures[0] : null;
        string jsonPath = jsonFiles[0];

        // Force model importer: Weld Vertices OFF (before loading the asset)
        EnsureModelWeldOff(modelPath);

        ImportTexture(vatTexturePath, false);
        if (!string.IsNullOrEmpty(vnrmTexturePath))
            ImportTexture(vnrmTexturePath, false);

        // --- Create material using the selected shader ---
        string matPath = Path.Combine(preparedFolder, Path.GetFileNameWithoutExtension(modelPath) + "_mat.mat");
        string shaderPath = SHADER_CHOICES[Mathf.Clamp(selectedShaderIndex, 0, SHADER_CHOICES.Length - 1)];
        bool isSimpleShader = shaderPath.ToLowerInvariant().Contains("simple");

        Shader chosenShader = Shader.Find(shaderPath);
        if (chosenShader == null)
        {
            Debug.LogError($"Selected shader not found: \"{shaderPath}\". Falling back to \"Shader Graphs/openvat-basis\".");
            chosenShader = Shader.Find("Shader Graphs/openvat-basis");
            if (chosenShader == null)
            {
                Debug.LogError("Fallback shader \"Shader Graphs/openvat-basis\" not found either. Aborting.");
                return;
            }
            isSimpleShader = false; // fallback is not simple
        }

        Material mat = new Material(chosenShader);
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();

        // ------------------- JSON parse -------------------
        string json = File.ReadAllText(jsonPath);
        JObject root = JObject.Parse(json);

        // os-remap bounds
        Vector3 minBounds = Vector3.zero, maxBounds = Vector3.one;
        if (root["os-remap"] != null)
        {
            JToken osRemap = root["os-remap"];
            var minArr = osRemap["Min"] as JArray;
            var maxArr = osRemap["Max"] as JArray;
            if (minArr != null && maxArr != null && minArr.Count == 3 && maxArr.Count == 3)
            {
                minBounds = new Vector3((float)minArr[0], (float)minArr[1], (float)minArr[2]);
                maxBounds = new Vector3((float)maxArr[0], (float)maxArr[1], (float)maxArr[2]);
            }
        }

        // Frames (needed for 'simple' shaders)
        int framesFromJson = 0;
        JToken framesTok = root["os-remap"]?["Frames"] ?? root["Frames"];
        if (framesTok != null)
        {
            framesFromJson = Mathf.Max(0, (int)framesTok);
        }

        // Textures & YRes
        Texture2D positionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(vatTexturePath);
        Texture2D normalTex = !string.IsNullOrEmpty(vnrmTexturePath)
            ? AssetDatabase.LoadAssetAtPath<Texture2D>(vnrmTexturePath)
            : null;
        int yResolution = positionTex ? positionTex.height : 0;

        // ---- Instantiate, assign material ----
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab == null)
        {
            Debug.LogError("Failed to load model at path: " + modelPath);
            return;
        }

        GameObject modelObj = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        modelObj.transform.position = Vector3.zero;
        var renderer = modelObj.GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.sharedMaterial = mat;

        // ---- Common material properties (both shader types) ----
        if (mat.HasProperty("_PosTexA")) mat.SetTexture("_PosTexA", positionTex);
        if (!string.IsNullOrEmpty(vnrmTexturePath) && mat.HasProperty("_NrmTexA")) mat.SetTexture("_NrmTexA", normalTex);
        if (mat.HasProperty("_MinA")) mat.SetVector("_MinA", minBounds);
        if (mat.HasProperty("_MaxA")) mat.SetVector("_MaxA", maxBounds);
        if (mat.HasProperty("_YResA")) mat.SetFloat("_YResA", yResolution);

        // ---- Extra property for 'simple' shaders: _FrameEndA from JSON Frames ----
        if (isSimpleShader)
        {
            int frameEnd = framesFromJson > 0 ? framesFromJson : (yResolution > 0 ? yResolution : 1);
            if (mat.HasProperty("_FrameEndA")) mat.SetFloat("_FrameEndA", frameEnd);

            // Save prefab WITHOUT controller/animation data
            string prefabPathSimple = Path.Combine(preparedFolder, Path.GetFileNameWithoutExtension(modelPath) + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(modelObj, prefabPathSimple);
            DestroyImmediate(modelObj);
            EditorSceneManager.MarkAllScenesDirty();
            return; // done for 'simple' shaders
        }

        // ---------------- Animations + controller (non-'simple' only) ----------------
        string animDataPath = Path.Combine(preparedFolder, Path.GetFileNameWithoutExtension(modelPath) + "_VATAnimationData.asset");
        VATAnimationData animData = ScriptableObject.CreateInstance<VATAnimationData>();
        animData.animations = new List<VATAnimationData.VATAnimation>();

        var animsToken = root["animations"];
        bool hasAnimations = (animsToken != null && animsToken.Type == JTokenType.Object && animsToken.HasValues);

        if (hasAnimations)
        {
            foreach (var animPair in (JObject)animsToken)
            {
                string animName = animPair.Key;
                JObject animInfo = animPair.Value as JObject;
                if (animInfo == null) continue;

                var anim = new VATAnimationData.VATAnimation
                {
                    name = animName,
                    frameStart = animInfo["startFrame"] != null ? (int)animInfo["startFrame"] : 0,
                    frameEnd = animInfo["endFrame"] != null ? (int)animInfo["endFrame"] : 0,
                    framerate = animInfo["framerate"] != null ? (float)animInfo["framerate"] : 30f,
                    looping = animInfo["looping"] != null && (bool)animInfo["looping"],
                };
                animData.animations.Add(anim);
            }
        }
        else
        {
            int frames = framesFromJson;
            if (frames <= 0) frames = 1;

            var defaultAnim = new VATAnimationData.VATAnimation
            {
                name = "Default",
                frameStart = 1,
                frameEnd = frames,
                framerate = 30f,
                looping = true
            };
            animData.animations.Add(defaultAnim);

            Debug.LogWarning($"No animations found in JSON. Created default animation: 1–{frames} (looping).");
        }

        AssetDatabase.CreateAsset(animData, animDataPath);
        AssetDatabase.SaveAssets();

        var controller = modelObj.AddComponent<VATController>();
        controller.animationData = animData;

        string prefabPath = Path.Combine(preparedFolder, Path.GetFileNameWithoutExtension(modelPath) + ".prefab");
        PrefabUtility.SaveAsPrefabAsset(modelObj, prefabPath);
        DestroyImmediate(modelObj);

        EditorSceneManager.MarkAllScenesDirty();
#endif
    }

    // Texture import settings
    private void ImportTexture(string path, bool isNormalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.None;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            importer.maxTextureSize = Mathf.Max(tex.width, tex.height);
        }

        importer.SaveAndReimport();
    }

    [System.Serializable]
    private class RemapData { public OsRemap os_remap; }
    [System.Serializable]
    private class OsRemap { public float[] Min; public float[] Max; public float Frames; }
}
