using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Config;
using Narrative.Localization;
using UI.Shared.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Managers{
    /// Central manager governing additive scene transitions across the application lifecycle.
    /// Lives permanently in the persistent Boot scene and coordinates transitions between
    /// title diorama scenes, game sessions, and local map areas.
    [DisallowMultipleComponent]
    public class SceneCoordinator : MonoBehaviour{
        public const string TitleSceneName       = "BootMenuScene";
        public const string GameSessionSceneName = "GameSession";

        public static SceneCoordinator Instance{ get; private set; }

        [SerializeField] private bool                    autoLoadTitleOnStart = true;
        [SerializeField] private SceneDependencyDatabase dependencyDatabase;

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
        }

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
        /// and additively loads the persistent GameSession scene.
        public static async Task StartGameSessionAsync(string saveSlotName){
            OnTransitionStarted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeInAsync();

            if (SceneManager.GetSceneByName(TitleSceneName).isLoaded){
                await SceneManager.UnloadSceneAsync(TitleSceneName);
                OnTitleSceneUnloaded?.Invoke();

                GC.Collect();
                await Resources.UnloadUnusedAssets();
            }

            SaveSystem.InitializeSession(saveSlotName);

            AsyncOperation sessionOp = SceneManager.LoadSceneAsync(GameSessionSceneName, LoadSceneMode.Additive);
            if (sessionOp != null){
                sessionOp.allowSceneActivation = false;

                await SaveSystem.LoadFiles(SaveSystem.Instance.coreFileNames);

                sessionOp.allowSceneActivation = true;
                await sessionOp;
            }

            OnTransitionCompleted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Transitions between two map zones to a target spawn index, committing memory and streaming new scene.
        public async Task TransitionToMapAsync(string newMapName, int spawnIndex = 0){
            GameSessionManager.PendingSpawnIndex                   = spawnIndex;
            GameSessionManager.Instance.currentMapName.Value = newMapName;
            OnTransitionStarted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeInAsync();

            // 1. Unload old map scene, commit partitions to disk, and purge its RAM tables
            if (!string.IsNullOrEmpty(ActiveMapScene) && SceneManager.GetSceneByName(ActiveMapScene).isLoaded){
                await SceneManager.UnloadSceneAsync(ActiveMapScene);

                List<string> oldDeps = dependencyDatabase.GetSceneDependencies(ActiveMapScene);
                foreach (string file in oldDeps)
                    await SaveSystem.CommitAndReleaseFileAsync(file);

                await SaveSystem.AutosaveAsync();

                GC.Collect();
                await Resources.UnloadUnusedAssets();
            }

            // 2. Stream new 3D scene assets in background, holding activation
            ActiveMapScene = newMapName;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(newMapName, LoadSceneMode.Additive);
            if (loadOp != null){
                loadOp.allowSceneActivation = false;

                // 3. Read and parse incoming save data from active session
                List<string> newDeps = dependencyDatabase.GetSceneDependencies(newMapName);
                await SaveSystem.LoadFiles(newDeps);

                // 4. Blackboard is ready! Allow map to activate and wake up entities
                loadOp.allowSceneActivation = true;
                await loadOp;
            }

            OnTransitionCompleted?.Invoke();
            await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

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
