using Data.Effects;
using UnityEngine;

namespace World.Tactical{
    /// Records a planned effect occurrence at a specific world coordinate along the turn track.
    public struct PlannedEffectMilestone{
        public Vector3       Position;
        public IEffect       Effect;
        public EffectContext Context;

        public PlannedEffectMilestone(Vector3 position, IEffect effect, EffectContext context){
            Position = position;
            Effect   = effect;
            Context  = context;
        }
    }
}
