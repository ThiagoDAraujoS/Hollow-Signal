using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data{
    [Serializable]
    public struct RequirementRule{
        public string key;
        public string[] args;

        public RequirementRule(List<string> partsList){
            if (partsList == null || partsList.Count == 0){
                key  = string.Empty;
                args = Array.Empty<string>();
                return;
            }
            key  = partsList[0].Trim().ToLower();
            args = new string[partsList.Count - 1];
            for (int i = 1; i < partsList.Count; i++)
                args[i - 1] = partsList[i].Trim();
        }
    }

    /// Represents a design-defined Mastery archetype containing localized identity keys and stacking skill bonuses.
    [CreateAssetMenu(fileName = "NewMastery", menuName = "CRPG/Mastery")]
    public class Mastery : ScriptableObject{
        [Header("Identity (Auto-Imported)")] 
        [Tooltip("The unique identifier used for save/load serialization. Do not edit manually!")] 
        [SerializeField]
        private string id;

        [SerializeField, HideInInspector] 
        private string 
            nameKey, 
            descKey;

        [Header("Visuals")] 
        [Tooltip("Drag and drop the visual icon sprite for this mastery here.")] [SerializeField]
        private Sprite icon;

        [Header("Engine Mechanics")] 
        [Tooltip("The list of skills this mastery boosts. Duplicate entries represent stacked bonuses (e.g., +2).")] [SerializeField]
        private List<Skill> associatedSkills = new();

        [SerializeField] 
        private List<Skill> penalizedSkills = new();

        [Header("Prerequisites (Auto-Imported)")] 
        [Tooltip("Requirements strings (e.g., 'has_level:4', 'has_skill:LockPick:2') evaluated at level-up.")]
        [SerializeField, HideInInspector] 
        private List<RequirementRule> prerequisites = new();

        public string Id                   => id;
        public string LocalizedName        => LocalizationManager.Get(nameKey);
        public string LocalizedDescription => LocalizationManager.Get(descKey);
        public Sprite Icon                 => icon;
        
        public IReadOnlyList<Skill>           AssociatedSkills     => associatedSkills;
        public IReadOnlyList<Skill>           PenalizedSkills      => penalizedSkills;
        public IReadOnlyList<RequirementRule> Prerequisites        => prerequisites;

        /// Populates or updates the mastery's parameters during automatic importing.
        public void Initialize(string masteryId, string nameK, string descK, List<Skill> associated, List<Skill> penalized, List<RequirementRule> prerequisitesList){
            id      = masteryId;
            nameKey = nameK;
            descKey = descK;
            
            associatedSkills = associated;
            penalizedSkills  = penalized;
            prerequisites    = prerequisitesList;
        }
    }
}