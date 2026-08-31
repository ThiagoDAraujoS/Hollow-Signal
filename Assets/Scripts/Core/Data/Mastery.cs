using System.Collections.Generic;
using UnityEngine;

namespace Core.Data{
    /// Represents a design-defined Mastery archetype containing localized identity keys and stacking skill bonuses.
    [CreateAssetMenu(fileName = "NewMastery", menuName = "CRPG/Mastery")]
    public class Mastery : ScriptableObject{
        [Header("Identity (Auto-Imported)")]
        [Tooltip("The unique identifier used for save/load serialization. Do not edit manually!")]
        [SerializeField] private string id;
        
        // Hidden in Inspector to protect translation keys from accidental manual typos
        [SerializeField, HideInInspector] private string nameKey;
        [SerializeField, HideInInspector] private string descKey;

        [Header("Visuals")]
        [Tooltip("Drag and drop the visual icon sprite for this mastery here.")]
        [SerializeField] private Sprite icon;

        [Header("Engine Mechanics")]
        [Tooltip("The list of skills this mastery boosts. Duplicate entries represent stacked bonuses (e.g., +2).")]
        [SerializeField] private List<Skill> associatedSkills = new();
        [SerializeField] private List<Skill> penalizedSkills = new();
        
        public                   string      Id => id;
        
        // Dynamic translation resolution on access!
        public string               LocalizedName        => LocalizationManager.Get(nameKey);
        public string               LocalizedDescription => LocalizationManager.Get(descKey);
        public Sprite               Icon                 => icon;
        public IReadOnlyList<Skill> AssociatedSkills     => associatedSkills;
        public IReadOnlyList<Skill> PenalizedSkills      => penalizedSkills;
        
        /// Populates or updates the mastery's parameters during automatic importing.
        public void Initialize(string masteryId, string nameK, string descK, List<Skill> associated, List<Skill> penalized){
            id                    = masteryId;
            nameKey               = nameK;
            descKey               = descK;
            associatedSkills = associated;
            penalizedSkills  = penalized;
        }
    }
}
