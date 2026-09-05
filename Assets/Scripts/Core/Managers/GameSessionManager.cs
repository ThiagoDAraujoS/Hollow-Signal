using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Managers {
    /// Persistent proxy session manager.
    /// Loaded on session start, fetches core files using the pre-configured SaveSystem,
    /// and bootstraps the correct gameplay level scene.
    public class GameSessionManager : TrackedBehaviour {
        public static GameSessionManager Instance { get; private set; }
   
        [SerializeField] private SceneDependencyDatabase dependencyDatabase;

        public Tracked<string> currentMapName = new("CurrentMapName", "test_map");

        protected override void OnAwake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy() {
            if (Instance == this)
                Instance = null;
        }

        private async void Start(){
            try{
                List<string> deps = dependencyDatabase.GetSceneDependencies(currentMapName);
                await SaveSystem.LoadFiles(deps, 
                                           _ => { /*TODO: Restore the game to its main menu state and show an error message */ });
                AsyncOperation op = SceneManager.LoadSceneAsync(currentMapName.Value, LoadSceneMode.Additive);
                Debug.Log($"{currentMapName.Value}, {SaveSystem.CurrentSaveSlot}");
                while (op is { isDone: false })
                    await Task.Yield();
            }
            catch (Exception){
                Debug.LogError("Could not load scene " + currentMapName);
                /*TODO: Restore the game to its main menu state and show an error message */
            }
        }
    }
}
