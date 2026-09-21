using System;
using System.Collections.Generic;
using Data.Effects;

namespace Core.Crisis{
    [Serializable]
    public class ScheduledEntry : IComparable<ScheduledEntry>{
        public float executionTime;
        public string effectId;
        public string targetId;
        [NonSerialized] public EffectContext context;

        public ScheduledEntry(float executionTime, string effectId, string targetId, EffectContext context){
            this.executionTime = executionTime;
            this.effectId = effectId;
            this.targetId = targetId;
            this.context = context;
        }

        /// Compares two entries for min-heap ordering by execution time.
        public int CompareTo(ScheduledEntry other) => executionTime.CompareTo(other.executionTime);

        /// Serializes this entry into a Blackboard compatible partition dictionary.
        public Dictionary<string, object> ToDictionary() => new(){
            ["time"] = executionTime,
            ["effect_id"] = effectId,
            ["target_id"] = targetId
        };

        /// Rebuilds an entry from a Blackboard dictionary.
        public static ScheduledEntry FromDictionary(Dictionary<string, object> dict) => new(
            Convert.ToSingle(dict["time"]),
            dict["effect_id"]?.ToString(),
            dict.TryGetValue("target_id", out object target) ? target?.ToString() : string.Empty,
            new EffectContext{ targetId = dict.TryGetValue("target_id", out object t) ? t?.ToString() : string.Empty }
        );
    }
}
