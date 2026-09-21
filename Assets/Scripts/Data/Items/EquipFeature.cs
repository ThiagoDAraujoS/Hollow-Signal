using UnityEngine;
using World.Actors.Player;

namespace Data.Items{
    /// Abstract contract for passive features applied while an item is equipped.
    public abstract class EquipFeature : ScriptableObject{
        public abstract void OnEquip(CharacterSheet user);
        public abstract void OnUnequip(CharacterSheet user);
    }
}
