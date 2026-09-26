using System.Collections.Generic;
using Core.State;
using UnityEngine;
using World.Anchors;

namespace Core.Managers{
    public class MapManager : TrackedBehaviour{
        [SerializeField] private List<Transform> spawnPoints = new();
        [SerializeField] private World.Anchors.Bounds cameraBounds = new();
        [SerializeField] private bool showGizmos = true;

        public Transform DefaultSpawnPoint => GetSpawnPoint(0);
        public World.Anchors.Bounds CameraBounds => cameraBounds;

        /// Returns the spawn point at the specified index, defaulting to index 0 or local transform if empty.
        public Transform GetSpawnPoint(int index = 0) =>
            spawnPoints.Count > 0 ? (index >= 0 && index < spawnPoints.Count ? spawnPoints[index] : spawnPoints[0]) : transform;

        private void Start() => GameSessionManager.LoadingMapFinished(this);

        /// Draws green flag gizmo icons over each registered spawn point and camera boundary wireframes in the editor.
        private void OnDrawGizmos(){
            if (!showGizmos) return;
            foreach (Transform pt in spawnPoints)
                if (pt) Gizmos.DrawIcon(pt.position + Vector3.up * 1.5f, "SpawnPointIcon.png", true);

            if (cameraBounds == null) return;
            Gizmos.color = Color.yellow;
            (Vector3 center, Vector3 size) = cameraBounds.GetCenterAndSize(transform.position.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
