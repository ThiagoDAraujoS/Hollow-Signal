using UnityEngine;

namespace Data.Effects.Leaves{
    /// Grants an ActionType perk to the target character sheet.
    [CreateAssetMenu(fileName = "GrantPerkEffect", menuName = "CRPG/Effects/Leaves/Grant Perk Effect")]
    public class GrantPerkEffect : EffectNode{
        [SerializeField] private ActionType perk;

        /// Grants the perk to the target character.
        public override void Run(EffectContext context) =>
            context.Character.GrantPerk(perk);
    }
}
