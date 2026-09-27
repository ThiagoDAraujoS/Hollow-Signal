using UnityEngine;
using World.Actors.Player;

namespace World.Tactical{
    /// Immortal planner entity mirroring the active hero and recording turn traversal plans.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Character))]
    public class GhostActor : MonoBehaviour{
        public static GhostActor Instance{ get; private set; }

        public Character         GhostCharacter{ get; private set; }
        public CharacterMovement Movement      => GhostCharacter.movement;
        public Character         ChannelingHero{ get; private set; }
        public TurnPlanTrack     PlanTrack     { get; } = new();

        /// Initializes singleton instance and caches required components.
        private void Awake(){
            Instance       = this;
            GhostCharacter = GetComponent<Character>();
            GhostCharacter.EnsureInitialized();
            SetVisualsActive(false);
        }

        /// Cleans up singleton instance on destroy.
        private void OnDestroy() => Instance = Instance == this ? null : Instance;

        /// Binds the ghost to the active hero and warps to their current position.
        public void Channel(Character hero){
            ChannelingHero = hero;
            PlanTrack.Clear();
            GhostCharacter.EnsureInitialized();
            SetVisualsActive(true);
            GhostCharacter.movement.WarpTo(hero.WorldPosition);
        }

        /// Clears channeling hero and hides ghost visuals.
        public void Dismiss(){
            ChannelingHero = null;
            PlanTrack.Clear();
            SetVisualsActive(false);
        }

        /// Toggles ghost model and movement components.
        public void SetVisualsActive(bool active){
            GhostCharacter.body.SetActive(active);
            GhostCharacter.nmAgent.enabled  = active;
            GhostCharacter.movement.enabled = active;
        }
    }
}
