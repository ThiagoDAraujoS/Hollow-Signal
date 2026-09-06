using UnityEngine;

namespace Core.Managers{
    public class MapManager : TrackedBehaviour{
        [Header("Spawn Settings")] [SerializeField]
        private Transform defaultSpawnPoint;

        public Transform DefaultSpawnPoint => defaultSpawnPoint;

        private void Start() => GameSessionManager.LoadingMapFinished(this);
    }
}
