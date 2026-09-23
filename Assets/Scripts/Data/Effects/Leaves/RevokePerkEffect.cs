using UnityEngine;

namespace Data.Effects.Leaves{
    /// Revokes an ActionType perk from the target character sheet.
    [CreateAssetMenu(fileName = "RevokePerkEffect", menuName = "CRPG/Effects/Leaves/Revoke Perk Effect")]
    public class RevokePerkEffect : EffectNode{
        [SerializeField] private ActionType perk;

        /// Revokes the perk from the target character.
        public override void Run(EffectContext context) =>
            context.Character.RevokePerk(perk);
    }
}
