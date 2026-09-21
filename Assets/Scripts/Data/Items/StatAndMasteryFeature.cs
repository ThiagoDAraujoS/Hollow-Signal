using UnityEngine;
using World.Actors.Player;

namespace Data.Items{
    /// Passive equipment feature that boosts maximum vitality pools and grants a mastery while equipped.
    [CreateAssetMenu(fileName = "NewStatAndMasteryFeature", menuName = "CRPG/Items/Features/Stat & Mastery")]
    public class StatAndMasteryFeature : EquipFeature{
        [SerializeField] private int maxFleshBonus = 2;
        [SerializeField] private int maxComposureBonus = 0;
        [SerializeField] private Mastery grantedMastery;

        public override void OnEquip(CharacterSheet user){
            if (grantedMastery != null)
                user.EquippedConditions.TryAdd(grantedMastery);

            user.ModifyMaxVitality(maxFleshBonus, maxComposureBonus);
        }

        public override void OnUnequip(CharacterSheet user){
            if (grantedMastery != null)
                user.EquippedConditions.TryRemove(grantedMastery);

            user.ModifyMaxVitality(-maxFleshBonus, -maxComposureBonus);
        }
    }
}
