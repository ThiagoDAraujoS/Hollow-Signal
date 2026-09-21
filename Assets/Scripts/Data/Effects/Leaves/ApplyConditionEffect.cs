using UnityEngine;

namespace Data.Effects.Leaves{
    [CreateAssetMenu(fileName = "ApplyConditionEffect", menuName = "CRPG/Effects/Leaves/Apply Condition Effect")]
    public class ApplyConditionEffect : EffectNode{
        [SerializeField] private Mastery condition;

        /// Injects the condition mastery into the target temporary conditions bucket.
        public override void Run(EffectContext context) =>
            context.Character.TemporaryConditions.TryAdd(condition);
    }
}
