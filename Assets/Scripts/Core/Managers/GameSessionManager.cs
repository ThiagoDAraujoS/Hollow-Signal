using System;
using System.Collections;
using System.Collections.Generic;
using Core.Config;
using Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;
using World.Actors.Brains;
using World.Actors.Player;
using World.Anchors;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Core.Managers{
    /// Persistent proxy session manager.
    public class GameSessionManager : TrackedBehaviour{
        public static GameSessionManager Instance{ get; private set; }

        [SerializeField] private SceneDependencyDatabase dependencyDatabase;
        public                   Tracked<string>         mainCharacterName = new("mainCharacterName", "Lucca");
        public                   Tracked<string>         currentMapName    = new("CurrentMapName", "GameStart");

        [Header("Party Heroes Roster")] [Tooltip("The parent transform containing all persistent playable hero GameObjects.")] [SerializeField]
        private Transform heroesContainer;

        [Tooltip("List of Unique IDs of all currently active party characters.")] [SerializeField]
        private List<string> activeCharacterIds = new();

        public IReadOnlyList<string> ActiveCharacterIds => activeCharacterIds;

        public static int                      PendingSpawnIndex{ get; set; } = 0;
        public static MapManager               CurrentMapManager{ get; private set; }
        public static event Action<MapManager> OnMapLoaded;

        /// Assigns active map manager, bootstraps the hero party, and triggers the map loaded event.
        public static void LoadingMapFinished(MapManager manager){
            CurrentMapManager = manager;
            Instance.InitializeParty(manager);
            OnMapLoaded?.Invoke(manager);
        }

        /// Enforces singleton instance across scene loads.
        protected override void OnAwake(){
            if (Instance && Instance != this){
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// Cleans up singleton instance reference on destroy.
        private void OnDestroy(){
            if (Instance == this) Instance = null;
        }

        /// Asynchronously loads save dependencies and additively loads the active map scene.
        private async void Start(){
            List<string> deps = dependencyDatabase.GetSceneDependencies(currentMapName);
            await SaveSystem.LoadFiles(deps, _ => { });

            if (SceneCoordinator.Instance) SceneCoordinator.Instance.ActiveMapScene = currentMapName.Value;
            var op = SceneManager.LoadSceneAsync(currentMapName.Value, LoadSceneMode.Additive);
            if (op != null) await op;
            Debug.Log($"{currentMapName.Value}, {SaveSystem.CurrentSaveSlot}");
        }

        /// Finds a hero character instance by its unique identifier string.
        public Character GetHeroById(string id){
            if (string.IsNullOrEmpty(id)) return null;
            Character[] heroes = heroesContainer.GetComponentsInChildren<Character>(includeInactive: true);
            foreach (Character hero in heroes)
                if (hero.TryGetComponent<UniqueId>(out var uniqueId) && uniqueId.Id == id) return hero;
            return null;
        }

        /// Spawns and configures active party members in formation at the target map spawn point.
        public void InitializeParty(MapManager map){
            Character[] allHeroes = heroesContainer.GetComponentsInChildren<Character>(includeInactive: true);
            PlayerBrain.ClearPartyMembers();

            List<Character> activeHeroes = new();

            foreach (Character hero in allHeroes){
                hero.EnsureInitialized();
                string heroId         = hero.GetComponent<UniqueId>().Id;
                bool   shouldBeActive = activeCharacterIds.Contains(heroId);

                hero.SetVisualsActive(shouldBeActive);

                if (!shouldBeActive) continue;
                PlayerBrain.AddPartyMember(hero);
                hero.nmAgent.enabled  = true;
                hero.movement.enabled = true;
                activeHeroes.Add(hero);
            }

            if (activeHeroes.Count == 0) return;

            Transform spawn = map.GetSpawnPoint(PendingSpawnIndex);
            PendingSpawnIndex = 0;
            Character lead  = activeHeroes[0];
            Dictionary<Character, Vector3> destinations = FormationCalculator.CalculateFormationPositions(
                spawn.position,
                lead,
                activeHeroes,
                overrideFacing: spawn.rotation
            );

            foreach ((Character hero, Vector3 pos) in destinations){
                hero.nmAgent.Warp(pos);
                hero.transform.rotation = spawn.rotation;
            }

            CameraAnchor.SetUpCamera(lead.WorldPosition, map.CameraBounds);
            CameraAnchor.Track(lead.transform);
        }

        /// Checks whether a character ID is currently marked as an active party member.
        public bool IsCharacterActive(string characterId) =>
            !string.IsNullOrEmpty(characterId) && activeCharacterIds.Contains(characterId);

        /// Updates the active tracking status of a character in the session party.
        public void SetCharacterActive(string characterId, bool isActive){
            if (string.IsNullOrEmpty(characterId)) return;

            if (isActive && !activeCharacterIds.Contains(characterId))
                activeCharacterIds.Add(characterId);
            else if (!isActive)
                activeCharacterIds.Remove(characterId);
        }

        /// Gathers active state from children and components.
        public override void OnSaveState(Partition state){
            base.OnSaveState(state);
            state["ActiveParty"]        = new List<string>(activeCharacterIds);
            state["MainCharacterName"]  = (string)mainCharacterName;
            state["CurrentMapName"]     = (string)currentMapName;
        }

        /// Loads and synchronizes persistent state onto local fields.
        public override void OnLoadState(Partition state){
            base.OnLoadState(state);
            if (!state.TryGetValue("ActiveParty", out object rawList)) return;

            activeCharacterIds.Clear();
            if (rawList is IEnumerable<object> objEnum)
                foreach (object item in objEnum)
                    activeCharacterIds.Add(item.ToString());
            else if (rawList is IEnumerable<string> strEnum)
                activeCharacterIds.AddRange(strEnum);

            if (state.TryGetValue("MainCharacterName", out var mainNameObj) && mainNameObj != null)
                mainCharacterName.Value = mainNameObj.ToString();

            if (state.TryGetValue("CurrentMapName", out var mapNameObj) && mapNameObj != null)
                currentMapName.Value = mapNameObj.ToString();
        }
    }
}
