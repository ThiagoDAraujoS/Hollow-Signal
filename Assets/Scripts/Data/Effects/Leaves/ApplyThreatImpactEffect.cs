using System.Collections.Generic;
using Core.Crisis;
using UnityEngine;
using World.Tactical;
using World.Threats;

namespace Data.Effects.Leaves{
    /// Delivers an impact payload to the active threat or context threat.
    [CreateAssetMenu(fileName = "ApplyThreatImpactEffect", menuName = "CRPG/Effects/Leaves/Apply Threat Impact Effect")]
    public class ApplyThreatImpactEffect : EffectNode{
        [SerializeField] private int globalDamage;
        [SerializeField] private List<string> globalDamageTypes = new();
        [SerializeField] private List<string> globalTags = new();

        [Header("Area Specific (Prefers context zone, falls back to threat current zone)")]
        [SerializeField] private int areaAmount;
        [SerializeField] private List<string> areaImpactTypes = new();

        /// Constructs the impact payload and ships it to the threat.
        public override void Run(EffectContext context){
            ThreatSheet threat = context.Threat ?? CrisisManager.Instance.ActiveThreat;
            List<AreaImpact> areaImpacts = new();

            TacticalZone targetZone = context.zone ? context.zone : threat.CurrentZone;
            if (targetZone && areaAmount > 0)
                areaImpacts.Add(new AreaImpact(targetZone, areaAmount, areaImpactTypes));

            ImpactPayload payload = new(globalDamage, globalDamageTypes, globalTags, areaImpacts);
            threat.ReceiveImpact(payload);
        }
    }
}
