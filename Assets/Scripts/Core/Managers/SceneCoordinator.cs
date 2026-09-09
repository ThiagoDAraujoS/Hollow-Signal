using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Config;
using Core.Localization;
using Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Managers{
    /// Central manager governing additive scene transitions across the application lifecycle.
    /// Lives permanently in the persistent Boot scene and coordinates transitions between
    /// title diorama scenes, game sessions, and local map areas.
    [DisallowMultipleComponent]
    public class SceneCoordinator : MonoBehaviour{
        public const string TitleSceneName       = "CoolMenuScene";
        public const string GameSessionSceneName = "GameSession";

        public static SceneCoordinator Instance{ get; private set; }

        [SerializeField] private bool                    autoLoadTitleOnStart = true;
        [SerializeField] private SceneDependencyDatabase dependencyDatabase;

        public static event Action        OnTransitionStarted;
        public static event Action        OnTransitionCompleted;
        public static event Action<Scene> OnTitleSceneLoaded;
        public static event Action        OnTitleSceneUnloaded;

        public string ActiveMapScene{ get; private set; }

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
            if (autoLoadTitleOnStart)
                await LoadTitleSceneAsync();
        }

        /// Additively loads the title diorama scene and parses core localization concurrently before unveiling.
        public static async Task LoadTitleSceneAsync(){
            OnTransitionStarted?.Invoke();
            if (LoadingScreenCurtain.Instance != null)
                await LoadingScreenCurtain.Instance.FadeInAsync();

            Task locTask = LocalizationManager.LoadBaseStringsAsync(LocalizationManager.CurrentLanguage);
            await SceneManager.LoadSceneAsync(TitleSceneName, LoadSceneMode.Additive);
            await locTask;

            OnTitleSceneLoaded?.Invoke(SceneManager.GetSceneByName(TitleSceneName));
            OnTransitionCompleted?.Invoke();

            if (LoadingScreenCurtain.Instance != null)
                await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Unloads the title scene, sets the active save slot, loads core blackboard data,
        /// and additively loads the persistent GameSession scene.
        public static async Task StartGameSessionAsync(string saveSlotName){
            OnTransitionStarted?.Invoke();
            if (LoadingScreenCurtain.Instance != null)
                await LoadingScreenCurtain.Instance.FadeInAsync();

            if (SceneManager.GetSceneByName(TitleSceneName).isLoaded){
                await SceneManager.UnloadSceneAsync(TitleSceneName);
                OnTitleSceneUnloaded?.Invoke();

                GC.Collect();
                await Resources.UnloadUnusedAssets();
            }

            SaveSystem.SetSaveSlot(saveSlotName);

            AsyncOperation sessionOp = SceneManager.LoadSceneAsync(GameSessionSceneName, LoadSceneMode.Additive);
            if (sessionOp != null){
                sessionOp.allowSceneActivation = false;

                await SaveSystem.LoadFiles(SaveSystem.Instance.coreFileNames);

                sessionOp.allowSceneActivation = true;
                await sessionOp;
            }

            OnTransitionCompleted?.Invoke();

            if (LoadingScreenCurtain.Instance != null)
                await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Transitions between two map zones: unloads old map, purges its blackboard partitions and localization,
        /// loads new dependencies concurrently, and activates the new map.
        public async Task TransitionToMapAsync(string newMapName){
            OnTransitionStarted?.Invoke();
            if (LoadingScreenCurtain.Instance != null)
                await LoadingScreenCurtain.Instance.FadeInAsync();

            // 1. Unload old map scene & purge its resources if an active map exists
            if (!string.IsNullOrEmpty(ActiveMapScene) && SceneManager.GetSceneByName(ActiveMapScene).isLoaded){
                await SceneManager.UnloadSceneAsync(ActiveMapScene);

                List<string> oldDeps = dependencyDatabase.GetSceneDependencies(ActiveMapScene);
                foreach (string file in oldDeps)
                    SaveSystem.ReleaseFile(file);

                List<string> oldLocTables = dependencyDatabase.GetSceneLocalizationTables(ActiveMapScene);
                foreach (string table in oldLocTables)
                    LocalizationManager.UnloadTable(table);

                GC.Collect();
                await Resources.UnloadUnusedAssets();
            }

            // 2. Stream new 3D scene assets in background, holding activation
            ActiveMapScene = newMapName;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(newMapName, LoadSceneMode.Additive);
            if (loadOp != null){
                loadOp.allowSceneActivation = false;

                // 3. Concurrently read and parse incoming save data and localization tables
                List<string> newDeps      = dependencyDatabase.GetSceneDependencies(newMapName);
                List<string> newLocTables = dependencyDatabase.GetSceneLocalizationTables(newMapName);

                Task saveTask = SaveSystem.LoadFiles(newDeps);
                Task locTask  = Task.WhenAll(newLocTables.Select(LocalizationManager.LoadTableAsync));
                await Task.WhenAll(saveTask, locTask);

                // 4. Blackboard and localization are ready! Allow map to activate and wake up entities
                loadOp.allowSceneActivation = true;
                await loadOp;
            }

            OnTransitionCompleted?.Invoke();

            if (LoadingScreenCurtain.Instance != null)
                await LoadingScreenCurtain.Instance.FadeOutAsync();
        }

        /// Closes active gameplay, unloads all non-boot scenes, purges memory,
        /// and restores the title diorama scene.
        public async Task ReturnToTitleMenuAsync(){
            OnTransitionStarted?.Invoke();
            if (LoadingScreenCurtain.Instance != null)
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
            Scene bootScene  = SceneManager.GetSceneAt(0);
            int   sceneCount = SceneManager.sceneCount;

            List<AsyncOperation> unloadOps = new();
            for (int i = sceneCount - 1; i >= 0; i--){
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene == bootScene) continue;
                unloadOps.Add(SceneManager.UnloadSceneAsync(scene));
            }

            foreach (AsyncOperation op in unloadOps){
                while (op is{ isDone: false })
                    await Task.Yield();
            }
        }
    }
}
