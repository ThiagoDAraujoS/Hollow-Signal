using Core.Managers;
using UnityEngine;

namespace Data.Effects.Leaves{
    [CreateAssetMenu(fileName = "MapTransitionEffect", menuName = "CRPG/Effects/Leaves/Map Transition Effect")]
    public class MapTransitionEffect : EffectNode{
        [SerializeField] private string targetMapName;
        [SerializeField] private int targetSpawnIndex;

        /// Triggers transition to target map at the configured spawn index.
        public override void Run(EffectContext context) =>
            _ = SceneCoordinator.Instance.TransitionToMapAsync(targetMapName, targetSpawnIndex);
    }
}
