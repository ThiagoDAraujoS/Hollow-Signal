using UnityEngine;
using World.Threats;

namespace Data.Effects.Leaves{
    /// Triggers engagement of a threat, initiating crisis combat if in exploration.
    [CreateAssetMenu(fileName = "EngageThreatEffect", menuName = "CRPG/Effects/Leaves/Engage Threat Effect")]
    public class EngageThreatEffect : EffectNode{
        [SerializeField] private ThreatSheet threat;

        /// Engages configured threat or context threat.
        public override void Run(EffectContext context) =>
            (threat ?? context.Threat).Engage();
    }
}
