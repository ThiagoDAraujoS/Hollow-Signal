using System.Collections.Generic;
using Core.State;
using UnityEngine;
using World.Anchors;

namespace Core.Managers{
    public class MapManager : TrackedBehaviour{
        [Header("Spawn Settings")]
        [SerializeField] private List<Transform> spawnPoints = new();

        [Header("Camera Bounds")]
        [SerializeField] private World.Anchors.Bounds cameraBounds = new();

        [Header("Editor Visuals")]
        [SerializeField] private bool showGizmos = true;

        public Transform DefaultSpawnPoint => GetSpawnPoint(0);
        public World.Anchors.Bounds CameraBounds => cameraBounds;

        /// Returns the spawn point at the specified index, defaulting to index 0.
        public Transform GetSpawnPoint(int index = 0) =>
            index >= 0 && index < spawnPoints.Count ? spawnPoints[index] : spawnPoints[0];

        private void Start() => GameSessionManager.LoadingMapFinished(this);

        /// Draws green flag gizmo icons over each registered spawn point and camera boundary wireframes in the editor.
        private void OnDrawGizmos(){
            if (!showGizmos) return;
            foreach (Transform pt in spawnPoints)
                if (pt != null)
                    Gizmos.DrawIcon(pt.position + Vector3.up * 1.5f, "SpawnPointIcon.png", true);

            if (cameraBounds == null) return;
            Gizmos.color = Color.yellow;
            (Vector3 center, Vector3 size) = cameraBounds.GetCenterAndSize(transform.position.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
