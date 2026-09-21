using UnityEngine;

namespace Data.Effects.Leaves{
    public enum HealTargetPool{ Flesh, Composure }

    [CreateAssetMenu(fileName = "HealEffect", menuName = "CRPG/Effects/Leaves/Heal Effect")]
    public class HealEffect : EffectNode{
        [SerializeField] private HealTargetPool pool;
        [SerializeField] private int amount;

        /// Heals the target character sheet pool according to configured parameters.
        public override void Run(EffectContext context){
            if (pool == HealTargetPool.Flesh)
                context.Character.Flesh.Heal(amount);
            else
                context.Character.Composure.Heal(amount);
        }
    }
}
