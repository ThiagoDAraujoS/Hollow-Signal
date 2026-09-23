using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;

namespace Data.Items{
    /// Grants action type perks to the character while equipped.
    [CreateAssetMenu(fileName = "NewPerkFeature", menuName = "CRPG/Items/Perk Feature")]
    public class PerkFeature : EquipFeature{
        [SerializeField] private List<ActionType> grantedPerks = new();

        public IReadOnlyList<ActionType> GrantedPerks => grantedPerks;

        /// Increments perk reference counts on the character sheet on equip.
        public override void OnEquip(CharacterSheet user){
            foreach (ActionType perk in grantedPerks)
                user.GrantPerk(perk);
        }

        /// Decrements perk reference counts on the character sheet on unequip.
        public override void OnUnequip(CharacterSheet user){
            foreach (ActionType perk in grantedPerks)
                user.RevokePerk(perk);
        }
    }
}
