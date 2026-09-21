using UnityEngine;

namespace Data.Effects.Leaves{
    [CreateAssetMenu(fileName = "RemoveConditionEffect", menuName = "CRPG/Effects/Leaves/Remove Condition Effect")]
    public class RemoveConditionEffect : EffectNode{
        [SerializeField] private Mastery condition;

        /// Removes the condition mastery from the target temporary conditions bucket.
        public override void Run(EffectContext context) =>
            context.Character.TemporaryConditions.TryRemove(condition);
    }
}
