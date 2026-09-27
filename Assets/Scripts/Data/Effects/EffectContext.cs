using System;
using System.Collections.Generic;
using Core.State;
using World.Actors;
using World.Actors.Player;
using World.Tactical;
using World.Threats;

namespace Data.Effects{
    [Serializable]
    public class EffectContext{
        public string targetId;
        [NonSerialized] public Dictionary<string, object> data = new();
        [NonSerialized] public Sheet target;
        [NonSerialized] public TacticalZone zone;

        public CharacterSheet Character => target as CharacterSheet;
        public ThreatSheet Threat => target as ThreatSheet;

        public EffectContext(){}

        public EffectContext(Sheet target, TacticalZone zone = null){
            this.target = target;
            this.zone = zone;
            if (target != null)
                targetId = target.GetComponent<UniqueId>().Id;
        }

        /// Retrieves or assigns metadata stored within this context payload.
        public object this[string key]{
            get => data[key];
            set => data[key] = value;
        }
    }
}
