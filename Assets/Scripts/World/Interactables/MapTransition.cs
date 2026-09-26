using Core.Managers;
using Data.Effects;
using UnityEngine;

namespace World.Interactables{
    /// Level component that triggers scene transitions to target maps and spawn points.
    public class MapTransition : MonoBehaviour, IEffect{
        [Header("Transition Target")]
        [SerializeField] private string targetMapName;
        [SerializeField] private int targetSpawnIndex;

        [Header("Editor Visuals")]
        [SerializeField] private bool showGizmo = true;

        public string TargetMapName => targetMapName;
        public int TargetSpawnIndex => targetSpawnIndex;

        /// Executes map transition when triggered as an IEffect.
        public void Run(EffectContext context) => Transition();

        /// Asynchronously transitions the game to the configured target map and spawn index.
        public void Transition() =>
            _ = SceneCoordinator.Instance.TransitionToMapAsync(targetMapName, targetSpawnIndex);

        /// Draws the map transition icon gizmo in the editor scene view.
        private void OnDrawGizmos(){
            if (showGizmo)
                Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "MapTransitionIcon.png", true);
        }
    }
}
