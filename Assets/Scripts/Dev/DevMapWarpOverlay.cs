using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Managers;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dev{
    /// In-game developer GUI overlay for jumping heroes to specific map scenes and spawn anchors,
    /// allowing fast generation and verification of scene-routed save files.
    public class DevMapWarpOverlay : MonoBehaviour{
        private static DevMapWarpOverlay _instance;

        [Header("State")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private Rect windowRect = new(15, 45, 340, 480);

        private readonly List<string> _availableMaps = new();
        private int _selectedMapIndex = 0;
        private int _selectedSpawnIndex = 0;
        private string _targetSaveSlotName = "template_custom";

        private bool _isMapDropdownOpen = false;
        private bool _isAnchorDropdownOpen = false;
        private Vector2 _mapScrollPos;
        private Vector2 _anchorScrollPos;
        private bool _isTransitioning = false;
        private string _statusMessage = string.Empty;
        private float _statusMessageTimer = 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// Automatically instantiates a persistent overlay instance on game launch.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize(){
            if (_instance != null || FindAnyObjectByType<DevMapWarpOverlay>() != null) return;
            GameObject go = new("[DevMapWarpOverlay]");
            DontDestroyOnLoad(go);
            go.AddComponent<DevMapWarpOverlay>();
        }
#endif

        private void Awake(){
            if (_instance != null && _instance != this){
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            PopulateMapList();
        }

        private void Update(){
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.backquoteKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame)){
                isOpen = !isOpen;
            }
#endif

            if (_statusMessageTimer > 0f){
                _statusMessageTimer -= Time.unscaledDeltaTime;
                if (_statusMessageTimer <= 0f) _statusMessage = string.Empty;
            }
        }

        /// Gathers all available map scenes from the project.
        public void PopulateMapList(){
            _availableMaps.Clear();

            // Default known fallback scenes
            string[] defaultScenes = {
                "GameStart",
                "OutsideAbandonedHouse",
                "GenericMap",
                "VerticalInside",
                "VerticalOutside",
                "VerticalCrisis",
                "DialogTest",
                "BlankMap"
            };

            foreach (string scene in defaultScenes)
                if (!_availableMaps.Contains(scene))
                    _availableMaps.Add(scene);

#if UNITY_EDITOR
            // Discover all unity scenes located within map folders
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[]{ "Assets/Scenes/Maps", "Assets/Scenes/TestMaps" });
            foreach (string guid in guids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(sceneName) && !_availableMaps.Contains(sceneName))
                    _availableMaps.Add(sceneName);
            }
#endif
            _availableMaps.Sort();
        }

        private void OnGUI(){
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return;
#endif
            // Always-visible corner anchor toggle button
            GUI.color = isOpen ? new Color(1f, 0.6f, 0.6f) : new Color(0.7f, 0.9f, 1f);
            if (GUI.Button(new Rect(15, 12, 110, 26), isOpen ? "✕ Close Dev" : "⚙ Dev Warp")){
                isOpen = !isOpen;
            }
            GUI.color = Color.white;

            if (!isOpen) return;

            windowRect = GUI.Window(99821, windowRect, DrawWindow, "Map Invoker & Save Generator");
        }

        private void DrawWindow(int windowId){
            GUILayout.Space(6);

            // Active Scene & Session Status
            string activeMap = SceneCoordinator.Instance != null && !string.IsNullOrEmpty(SceneCoordinator.Instance.ActiveMapScene)
                ? SceneCoordinator.Instance.ActiveMapScene
                : (GameSessionManager.Instance != null ? GameSessionManager.Instance.currentMapName.Value : "None");

            GUILayout.Label($"<b>Active Map:</b> <color=cyan>{activeMap}</color>", GetRichTextStyle());
            GUILayout.Label($"<b>Current Slot:</b> <color=yellow>{SaveSystem.CurrentSaveSlot ?? "None"}</color>", GetRichTextStyle());

            GUILayout.Space(8);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            // 1. MAP SELECTION DROPDOWN
            GUILayout.Label("<b>1. Target Map Scene:</b>", GetRichTextStyle());
            string selectedMapName = _availableMaps.Count > _selectedMapIndex ? _availableMaps[_selectedMapIndex] : "None";

            GUI.backgroundColor = _isMapDropdownOpen ? new Color(0.8f, 1f, 0.8f) : Color.white;
            if (GUILayout.Button($"Map: [{selectedMapName}] ▼", GUILayout.Height(28))){
                _isMapDropdownOpen = !_isMapDropdownOpen;
                _isAnchorDropdownOpen = false;
            }
            GUI.backgroundColor = Color.white;

            if (_isMapDropdownOpen){
                _mapScrollPos = GUILayout.BeginScrollView(_mapScrollPos, GUILayout.Height(140));
                for (int i = 0; i < _availableMaps.Count; i++){
                    bool isCurrent = (i == _selectedMapIndex);
                    if (GUILayout.Button(isCurrent ? $"► {_availableMaps[i]}" : $"   {_availableMaps[i]}")){
                        _selectedMapIndex = i;
                        _isMapDropdownOpen = false;
                        _targetSaveSlotName = $"template_{_availableMaps[i]}";
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Space(8);

            // 2. ANCHOR / SPAWN POINT DROPDOWN
            GUILayout.Label("<b>2. Target Spawn Anchor:</b>", GetRichTextStyle());

            int maxSpawnIndex = 5;
            if (GameSessionManager.CurrentMapManager != null && GameSessionManager.CurrentMapManager.SpawnPointCount > 0){
                maxSpawnIndex = Mathf.Max(maxSpawnIndex, GameSessionManager.CurrentMapManager.SpawnPointCount);
            }

            string anchorLabel = $"Spawn Anchor: {_selectedSpawnIndex}";
            if (GameSessionManager.CurrentMapManager != null && _selectedSpawnIndex < GameSessionManager.CurrentMapManager.SpawnPoints.Count){
                Transform pt = GameSessionManager.CurrentMapManager.SpawnPoints[_selectedSpawnIndex];
                if (pt != null) anchorLabel += $" ({pt.name})";
            }

            GUI.backgroundColor = _isAnchorDropdownOpen ? new Color(0.8f, 1f, 0.8f) : Color.white;
            if (GUILayout.Button($"{anchorLabel} ▼", GUILayout.Height(28))){
                _isAnchorDropdownOpen = !_isAnchorDropdownOpen;
                _isMapDropdownOpen = false;
            }
            GUI.backgroundColor = Color.white;

            if (_isAnchorDropdownOpen){
                _anchorScrollPos = GUILayout.BeginScrollView(_anchorScrollPos, GUILayout.Height(110));
                for (int i = 0; i < maxSpawnIndex; i++){
                    string label = $"Anchor {i}";
                    if (GameSessionManager.CurrentMapManager != null && i < GameSessionManager.CurrentMapManager.SpawnPoints.Count){
                        Transform pt = GameSessionManager.CurrentMapManager.SpawnPoints[i];
                        if (pt != null) label += $" - {pt.name}";
                    }

                    if (GUILayout.Button(i == _selectedSpawnIndex ? $"► {label}" : $"   {label}")){
                        _selectedSpawnIndex = i;
                        _isAnchorDropdownOpen = false;
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            // 3. WARP BUTTON
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !_isTransitioning && _availableMaps.Count > 0;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button(_isTransitioning ? "Warping Characters..." : "★ Load Characters Into Map ★", GUILayout.Height(32))){
                _ = ExecuteWarpAsync(_availableMaps[_selectedMapIndex], _selectedSpawnIndex);
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = wasEnabled;

            GUILayout.Space(12);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUILayout.Space(4);

            // 4. SAVE EXPORT SECTION
            GUILayout.Label("<b>3. Save Routed Template:</b>", GetRichTextStyle());
            GUILayout.BeginHorizontal();
            GUILayout.Label("Slot Name:", GUILayout.Width(75));
            _targetSaveSlotName = GUILayout.TextField(_targetSaveSlotName);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUI.backgroundColor = new Color(0.4f, 0.95f, 0.4f);
            if (GUILayout.Button($"Save to Slot '{_targetSaveSlotName}'", GUILayout.Height(28))){
                _ = ExecuteSaveAsync(_targetSaveSlotName);
            }
            GUI.backgroundColor = Color.white;

            // Status feedback
            if (!string.IsNullOrEmpty(_statusMessage)){
                GUILayout.Space(6);
                GUI.color = Color.green;
                GUILayout.Label(_statusMessage);
                GUI.color = Color.white;
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private async Task ExecuteWarpAsync(string mapName, int spawnIndex){
            if (_isTransitioning) return;
            _isTransitioning = true;
            _statusMessage = $"Warping to {mapName} (Spawn {spawnIndex})...";

            try{
                if (SceneCoordinator.Instance == null){
                    _statusMessage = "SceneCoordinator not found in scene!";
                    _statusMessageTimer = 3f;
                    _isTransitioning = false;
                    return;
                }

                // If GameSession is not running yet, start session first
                if (GameSessionManager.Instance == null){
                    await SceneCoordinator.StartGameSessionAsync(SaveSystem.DefaultSaveTemplate);
                }

                // Transition to the target map scene with the target anchor index
                await SceneCoordinator.Instance.TransitionToMapAsync(mapName, spawnIndex);

                _statusMessage = $"Warped to {mapName} at Anchor {spawnIndex}!";
                _statusMessageTimer = 4f;
            }
            catch (Exception ex){
                Debug.LogException(ex);
                _statusMessage = $"Warp failed: {ex.Message}";
                _statusMessageTimer = 5f;
            }
            finally{
                _isTransitioning = false;
            }
        }

        private async Task ExecuteSaveAsync(string slotName){
            if (string.IsNullOrWhiteSpace(slotName)){
                _statusMessage = "Slot name cannot be empty!";
                _statusMessageTimer = 3f;
                return;
            }

            try{
                await SaveSystem.SaveGame(slotName);
                _statusMessage = $"Successfully saved slot: {slotName}";
                _statusMessageTimer = 4f;
                Debug.Log($"[DevMapWarpOverlay] Saved current session to slot: '{slotName}'");
            }
            catch (Exception ex){
                Debug.LogException(ex);
                _statusMessage = $"Save failed: {ex.Message}";
                _statusMessageTimer = 5f;
            }
        }

        private GUIStyle _richTextStyle;
        private GUIStyle GetRichTextStyle(){
            if (_richTextStyle == null){
                _richTextStyle = new GUIStyle(GUI.skin.label){
                    richText = true
                };
            }
            return _richTextStyle;
        }
    }
}
