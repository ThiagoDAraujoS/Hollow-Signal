using Core.Crisis;
using UnityEngine;
using World.Threats;

namespace Data.Effects.Leaves{
    /// Inflicts damage against a ThreatSheet's integrity pool.
    [CreateAssetMenu(fileName = "DamageThreatEffect", menuName = "CRPG/Effects/Leaves/Damage Threat Effect")]
    public class DamageThreatEffect : EffectNode{
        [SerializeField] private int amount = 1;

        /// Damages the integrity pool of the context threat or active crisis threat.
        public override void Run(EffectContext context){
            ThreatSheet threat = context.Threat ?? CrisisManager.Instance.ActiveThreat;
            threat.Integrity.Damage(amount);
        }
    }
}
