using System;
using System.Collections.Generic;
using Core.State;
using World.Actors;
using World.Actors.Player;

namespace Data.Effects{
    [Serializable]
    public class EffectContext{
        public string targetId;
        [NonSerialized] public Dictionary<string, object> data = new();
        [NonSerialized] public Sheet target;

        public CharacterSheet Character => target as CharacterSheet;

        public EffectContext(){}

        public EffectContext(Sheet target){
            this.target = target;
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
