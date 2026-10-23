using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Shared.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;
using BlackboardSaveSystem;
using LocalizationSystem;

namespace Core.Managers{
    /// Central manager governing additive scene transitions across the application lifecycle.
    /// Lives permanently in the persistent Boot scene and coordinates transitions between
    /// title diorama scenes, game sessions, and local map areas.
    [DisallowMultipleComponent]
    public class SceneCoordinator : MonoBehaviour{
        public const string TitleSceneName       = "BootMenuScene";
        public const string GameSessionSceneName = "GameSession";

        public static SceneCoordinator Instance{ get; private set; }

        [SerializeField] private bool autoLoadTitleOnStart = true;

        public static event Action        OnTransitionStarted;
        public static event Action        OnTransitionCompleted;
        public static event Action<Scene> OnTitleSceneLoaded;
        public static event Action        OnTitleSceneUnloaded;

        public string ActiveMapScene{ get; set; }

        private void Awake(){
            if (Instance != null && Instance != this){
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SaveSystem.MetadataProvider        = ResolveSaveMetadata; 
        }

        /// Builds the key-value metadata dictionary for the active Hollow Signal session.
        public static Dictionary<string, string> ResolveSaveMetadata() => new(){
            ["location"]      = GameSessionManager.Instance != null && !string.IsNullOrEmpty(GameSessionManager.Instance.currentMapName.Value)
                ? GameSessionManager.Instance.currentMapName.Value
                : Instance?.ActiveMapScene,
            ["characterName"] = GameSessionManager.Instance?.mainCharacterName.Value,
            ["region"]        = "Station Outpost"
        };

        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }

        private async void Start(){
            try{
                if (autoLoadTitleOnStart)
                    await LoadTitleSceneAsync();
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }

        /// Additively loads the title diorama scene and parses core localization concurrently before unveiling.
        public static async Task LoadTitleSceneAsync(){
            OnTransitionStarted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeInAsync();

            Task locTask = LocalizationManager.LoadBaseStringsAsync(LocalizationManager.CurrentLanguage);
            await SceneManager.LoadSceneAsync(TitleSceneName, LoadSceneMode.Additive);
            await locTask;

            Scene loadedScene = SceneManager.GetSceneByName(TitleSceneName);
            SceneManager.SetActiveScene(loadedScene);

            OnTitleSceneLoaded?.Invoke(loadedScene);
            OnTransitionCompleted?.Invoke();

            await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Unloads the title scene, initializes the active working session, loads core blackboard data,
        /// and additively loads the persistent GameSession scene and initial map under the loading curtain.
        public static async Task StartGameSessionAsync(string saveSlotName = null){
            OnTransitionStarted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeInAsync();

            if (SceneManager.GetSceneByName(TitleSceneName).isLoaded){
                await SceneManager.UnloadSceneAsync(TitleSceneName);
                OnTitleSceneUnloaded?.Invoke();

                GC.Collect();
                await Resources.UnloadUnusedAssets();
            }

            SaveSystem.ClearActiveMemory();
            SaveSystem.InitializeSession(saveSlotName);

            AsyncOperation sessionOp = SceneManager.LoadSceneAsync(GameSessionSceneName, LoadSceneMode.Additive);
            if (sessionOp != null){
                sessionOp.allowSceneActivation = false;

                await SaveSystem.LoadFiles(SaveSystem.Instance.coreFileNames);

                sessionOp.allowSceneActivation = true;
                await sessionOp;
            }

            if (GameSessionManager.Instance != null)
                await GameSessionManager.Instance.LoadInitialMapAsync();

            OnTransitionCompleted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Transitions between two map zones to a target spawn index, committing memory and streaming a new scene.
        public async Task TransitionToMapAsync(string newMapName, int spawnIndex = 0){
            GameSessionManager.PendingSpawnIndex             = spawnIndex;
            GameSessionManager.Instance.currentMapName.Value = newMapName;
            OnTransitionStarted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeInAsync();

            Scene oldScene = !string.IsNullOrEmpty(ActiveMapScene)
                ? SceneManager.GetSceneByName(ActiveMapScene)
                : default;

            // 1. Flush & disable all BlackboardClients in the old scene and turn off all non-scope root GameObjects.
            // This immediately disables entities, lights, physics, audio, and cameras while keeping the
            // BlackboardScope holder active so its files remain leased in RAM until the new scene loads.
            if (oldScene.IsValid() && oldScene.isLoaded){
                GameObject[] oldRoots = oldScene.GetRootGameObjects();

                foreach (GameObject root in oldRoots){
                    if (root == null) continue;
                    foreach (BlackboardClient client in root.GetComponentsInChildren<BlackboardClient>(includeInactive: false)){
                        client.enabled = false;
                    }
                }

                foreach (GameObject root in oldRoots){
                    if (root == null) continue;
                    if (root.GetComponentInChildren<BlackboardScope>(includeInactive: false) != null ||
                        root.GetComponentInChildren<LocalizationScope>(includeInactive: false) != null) continue;
                    root.SetActive(false);
                }
            }

            // 2. Load the new scene additively.
            // With [DefaultExecutionOrder(-1000)], Unity runs the new scene's BlackboardScope.OnEnable() first
            // (acquiring files or bumping refCount to 2 for shared files), followed by BlackboardClient.OnEnable() (0)
            // to hydrate entities.
            ActiveMapScene = newMapName;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(newMapName, LoadSceneMode.Additive);
            if (loadOp != null)
                await loadOp;

            Scene newScene = SceneManager.GetSceneByName(newMapName);
            if (newScene.IsValid() && newScene.isLoaded)
                SceneManager.SetActiveScene(newScene);

            // 3. Unload the old map scene in controlled order
            // (remaining BlackboardClients flush first, BlackboardScopes release last: refCount drops 2 -> 1 or 1 -> 0).
            if (oldScene.IsValid() && oldScene.isLoaded){
                await UnloadSceneWithScopeOrderAsync(oldScene);
                await SaveSystem.AutosaveAsync();

                GC.Collect();
                await Resources.UnloadUnusedAssets();
            }

            OnTransitionCompleted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Unloads a scene while guaranteeing that all BlackboardClient entities flush and disabled
        /// before any BlackboardScope components release their file leases.
        public static async Task UnloadSceneWithScopeOrderAsync(Scene scene){
            if (!scene.IsValid() || !scene.isLoaded) return;

            GameObject[] roots = scene.GetRootGameObjects();

            // 1. Flush & disable all BlackboardClient components first while scopes are still active
            foreach (GameObject root in roots){
                if (root == null) continue;
                foreach (BlackboardClient client in root.GetComponentsInChildren<BlackboardClient>(includeInactive: false)){
                    client.enabled = false;
                }
            }

            // 2. Release all BlackboardScope and LocalizationScope components
            foreach (GameObject root in roots){
                if (root == null) continue;
                foreach (BlackboardScope scope in root.GetComponentsInChildren<BlackboardScope>(includeInactive: false)){
                    await scope.ReleaseScopeAsync();
                    scope.enabled = false;
                }
                foreach (LocalizationScope locScope in root.GetComponentsInChildren<LocalizationScope>(includeInactive: false)){
                    locScope.UnloadAll();
                    locScope.enabled = false;
                }
            }

            // 3. Unload the Unity scene
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(scene);
            if (unloadOp != null)
                await unloadOp;
        }

        /// Unloads a scene by name using the controlled BlackboardClient -> BlackboardScope teardown order.
        public static Task UnloadSceneWithScopeOrderAsync(string sceneName) =>
            UnloadSceneWithScopeOrderAsync(SceneManager.GetSceneByName(sceneName));

        /// Closes active gameplay, unloads all non-boot scenes, purges memory,
        /// and restores the title diorama scene.
        public async Task ReturnToTitleMenuAsync(){
            OnTransitionStarted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeInAsync();

            SaveSystem.ClearActiveMemory();
            await UnloadAllNonBootScenesAsync();
            ActiveMapScene = null;

            GC.Collect();
            await Resources.UnloadUnusedAssets();

            await LoadTitleSceneAsync();
        }

        /// Unloads all currently loaded scenes except for the persistent Boot scene.
        public static async Task UnloadAllNonBootScenesAsync(){
            Scene                bootScene = SceneManager.GetSceneAt(0);
            List<AsyncOperation> unloadOps = new();

            for (int i = 0; i < SceneManager.sceneCount; i++){
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != bootScene && scene.isLoaded)
                    unloadOps.Add(SceneManager.UnloadSceneAsync(scene));
            }

            await Task.WhenAll(unloadOps.Select(op => op.AsTask()));
        }
    }

    /// Extension utility to await Unity's AsyncOperation directly.
    public static class AsyncOperationExtensions{
        public static Task AsTask(this AsyncOperation asyncOp){
            TaskCompletionSource<bool> tcs = new();
            asyncOp.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }
    }
}
