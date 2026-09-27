using System.Collections.Generic;
using Data.Effects;
using UnityEngine;

namespace World.Tactical{
    /// Records the planned path, zone transition milestones, and destination slot for a hero's turn.
    public class TurnPlanTrack{
        public readonly List<Vector3>                Waypoints  = new();
        public readonly List<PlannedEffectMilestone> Milestones = new();
        public          TacticalSlot                 TargetSlot;

        /// Resets the recorded track for a new turn plan.
        public void Clear(){
            Waypoints.Clear();
            Milestones.Clear();
            TargetSlot = null;
        }

        /// Appends an effect milestone at the specified track coordinate.
        public void AddMilestone(Vector3 position, IEffect effect, EffectContext context) =>
            Milestones.Add(new PlannedEffectMilestone(position, effect, context));
    }
}
