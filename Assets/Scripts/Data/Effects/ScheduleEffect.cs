using Core.Crisis;
using UnityEngine;

namespace Data.Effects{
    [CreateAssetMenu(fileName = "ScheduleEffect", menuName = "CRPG/Effects/Schedule Effect")]
    public class ScheduleEffect : EffectNode{
        [SerializeField] private float durationMinutes = 1f;
        [SerializeField] private EffectNode effectToSchedule;

        /// Schedules the delayed effect to run after the duration passes on the world clock.
        public override void Run(EffectContext context){
            float executionTime = CrisisManager.Instance.ElapsedWorldTime + (durationMinutes * 60f);
            CrisisManager.Instance.Scheduler.Schedule(executionTime, effectToSchedule, context);
        }
    }
}
