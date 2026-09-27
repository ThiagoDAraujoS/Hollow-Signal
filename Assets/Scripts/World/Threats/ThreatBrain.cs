using System;
using UnityEngine;

namespace World.Threats{
    /// Base controller for Threat AI behavior during tactical combat rounds.
    [RequireComponent(typeof(ThreatSheet))]
    public abstract class ThreatBrain : MonoBehaviour{
        public ThreatSheet Threat { get; private set; }

        protected virtual void Awake() => Threat = GetComponent<ThreatSheet>();

        /// Evaluates tactical state, drafts action cards, and executes turn logic during EnemyPhase.
        public abstract void ExecuteTurn(Action onTurnComplete);

        /// Processes an incoming impact payload against this threat's integrity, areas, and tags.
        public virtual void ProcessImpact(ImpactPayload payload){
            int totalDamage = payload.globalDamage;
            foreach (AreaImpact impact in payload.areaImpacts)
                totalDamage += impact.amount;

            if (totalDamage > 0)
                Threat.Integrity.Damage(totalDamage);

            foreach (string tag in payload.globalTags)
                Threat.AddTag(tag);
        }
    }
}
