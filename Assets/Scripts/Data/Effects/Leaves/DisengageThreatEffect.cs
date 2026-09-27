using Core.Crisis;
using UnityEngine;
using World.Threats;

namespace Data.Effects.Leaves{
    /// Disengages a threat from active crisis combat, restoring exploration if no threats remain.
    [CreateAssetMenu(fileName = "DisengageThreatEffect", menuName = "CRPG/Effects/Leaves/Disengage Threat Effect")]
    public class DisengageThreatEffect : EffectNode{
        [SerializeField] private ThreatSheet threat;

        /// Disengages configured threat or active crisis threat.
        public override void Run(EffectContext context){
            if (threat)
                threat.Disengage();
            else
                CrisisManager.Instance.DisengageThreat();
        }
    }
}
