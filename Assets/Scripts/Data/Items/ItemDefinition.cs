using System.Collections.Generic;
using Data.Effects;
using UnityEngine;
using World.Actors.Player;

namespace Data.Items{
    [CreateAssetMenu(fileName = "NewItem", menuName = "CRPG/Items/Item Definition")]
    public class ItemDefinition : ScriptableObject{
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string nameKey;
        [SerializeField] private string descKey;
        [SerializeField] private Sprite icon;

        [Header("Item Flow")]
        [SerializeField] private bool isConsumable;

        [Header("Scriptable Capabilities")]
        [SerializeField] private EffectNode onUseEffect;
        [SerializeField] private List<EquipFeature> equipFeatures = new();

        public string Id => id;
        public Sprite Icon => icon;
        public bool IsConsumable => isConsumable;
        public EffectNode OnUseEffect => onUseEffect;
        public IReadOnlyList<EquipFeature> EquipFeatures => equipFeatures;

        /// Executes the root on-use effect cluster on the target character.
        public void Use(CharacterSheet user){
            EffectContext context = new(user);
            onUseEffect.Run(context);
        }

        /// Applies all passive features when placed into an equipment slot.
        public void Equip(CharacterSheet user){
            foreach (EquipFeature feature in equipFeatures)
                feature.OnEquip(user);
        }

        /// Reverts all passive features when removed from an equipment slot.
        public void Unequip(CharacterSheet user){
            foreach (EquipFeature feature in equipFeatures)
                feature.OnUnequip(user);
        }
    }
}
