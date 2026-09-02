using System;
using System.Collections.Generic;
using Actors.Player;
using UnityEngine;

namespace Data{
    public static class RequirementsEvaluator{
        // 1. The Expression Dictionary
        // It maps a string command (like "has_level") to an evaluation function (predicate).
        // Each predicate takes the target Character and the raw string argument (everything after the colon).
        private static readonly Dictionary<string, Func<CharacterSheet, string[], bool>> PREDICATE_DICT = new(){
            { "has_background",  (character, arg) => true }, // character.Background.Equals(arg, StringComparison.OrdinalIgnoreCase) }
            { "has_witnessed",   (character, arg) => true }, //Blackboard.GetBool(arg) }
            { "has_skill",       (character, arg) => true },
            { "is_rich:",        (character, arg) => true },
            { "has_any_mastery", (character, arg) => true }
        };

        // 2. The Sequence Evaluation Method
        // Performs a sequence of logical ANDs across all requirements on the mastery.
        public static bool MeetsPrerequisites(CharacterSheet character, Mastery mastery){
            //TODO: release this code once character is done.
            //if (character.Level < mastery.Level)
            //   return false;
            
            if (mastery.Prerequisites == null || mastery.Prerequisites.Count == 0)
                return true;

            foreach (RequirementRule rule in mastery.Prerequisites){
                if (string.IsNullOrEmpty(rule.key)) continue;
                if (!PREDICATE_DICT.TryGetValue(rule.key, out Func<CharacterSheet, string[], bool> predicate)){
                    Debug.LogWarning($"[RequirementsEvaluator] Unknown requirement key '{rule.key}' found on Mastery '{mastery.Id}'");
                    return false;
                }
                if (!predicate.Invoke(character, rule.args))
                    return false;
            }
            return true;
        }
    }
}