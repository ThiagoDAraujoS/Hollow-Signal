using Core.Managers;
using UnityEngine;

namespace World.Actors.Brains{
    public class MapTransition : MonoBehaviour{
        [SerializeField] private string targetMapName;
        [SerializeField] private int    targetSpawnIndex = 0;

        [Header("Editor Visuals")]
        [SerializeField] private bool showGizmo = true;

        /// Triggers transition to target map at the configured spawn index.
        public void Transition() => _ = SceneCoordinator.Instance.TransitionToMapAsync(targetMapName, targetSpawnIndex);

        /// Triggers transition when a player entity enters the collider volume.
        private void OnTriggerEnter(Collider other){
            if (other.CompareTag("Player"))
                Transition();
        }

        /// Draws the yellow flag gizmo icon in the editor scene view when enabled.
        private void OnDrawGizmos(){
            if (showGizmo)
                Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "MapTransitionIcon.png", true);
        }
    }
}
