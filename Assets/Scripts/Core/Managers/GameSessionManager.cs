using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actors.Brains;
using Actors.Player;
using Core.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Core.Managers {
    /// Persistent proxy session manager.
    /// Loaded on session start, fetches core files using the pre-configured SaveSystem,
    /// tracks active party characters across scenes, and bootstraps the correct gameplay level scene.
    public class GameSessionManager : TrackedBehaviour {
        public static GameSessionManager Instance { get; private set; }

        [SerializeField] private SceneDependencyDatabase dependencyDatabase;

        public Tracked<string> currentMapName = new("CurrentMapName", "test_map");

        [Header("Party Heroes Roster")]
        [Tooltip("The parent transform containing all persistent playable hero GameObjects.")]
        [SerializeField] private Transform heroesContainer;

        [Tooltip("List of Unique IDs of all currently active party characters.")]
        [SerializeField] private List<string> activeCharacterIds = new();

        public IReadOnlyList<string> ActiveCharacterIds => activeCharacterIds;

        public static event Action<MapManager> OnMapLoaded;

        public static void LoadingMapFinished(MapManager manager) => OnMapLoaded?.Invoke(manager);

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

        private async void Start() {
            try {
                List<string> deps = dependencyDatabase.GetSceneDependencies(currentMapName);
                await SaveSystem.LoadFiles(deps,
                    _ => { /*TODO: Restore the game to its main menu state and show an error message */ });

                AsyncOperation op = SceneManager.LoadSceneAsync(currentMapName.Value, LoadSceneMode.Additive);
                Debug.Log($"{currentMapName.Value}, {SaveSystem.CurrentSaveSlot}");
                while (op is { isDone: false })
                    await Task.Yield();

                InitializeParty();
            }
            catch (Exception e) {
                Debug.LogError($"Could not load scene {currentMapName}. Details: {e}");
                /*TODO: Restore the game to its main menu state and show an error message */
            }
        }

        public void InitializeParty() {
            Character[] allHeroes = heroesContainer.GetComponentsInChildren<Character>(includeInactive: true);
            PlayerBrain.ClearPartyMembers();

            foreach (Character hero in allHeroes) {
                string heroId = hero.GetComponent<UniqueId>().Id;
                bool shouldBeActive = activeCharacterIds.Contains(heroId);

                hero.SetVisualsActive(shouldBeActive);

                if (shouldBeActive)
                    PlayerBrain.AddPartyMember(hero);
            }
        }

        public bool IsCharacterActive(string characterId) =>
            !string.IsNullOrEmpty(characterId) && activeCharacterIds.Contains(characterId);

        public void SetCharacterActive(string characterId, bool isActive) {
            if (string.IsNullOrEmpty(characterId)) return;

            if (isActive && !activeCharacterIds.Contains(characterId))
                activeCharacterIds.Add(characterId);
            else if (!isActive)
                activeCharacterIds.Remove(characterId);
        }

        public override void OnSaveState(Partition state) {
            base.OnSaveState(state);
            state["active_character_ids"] = new List<string>(activeCharacterIds);
        }

        public override void OnLoadState(Partition state) {
            base.OnLoadState(state);
            if (state == null || !state.TryGetValue("active_character_ids", out object rawList)) return;
            activeCharacterIds.Clear();
            if (rawList is not IEnumerable enumerable) return;
            foreach (object item in enumerable) {
                if (item != null)
                    activeCharacterIds.Add(item.ToString());
            }
        }
    }
}
