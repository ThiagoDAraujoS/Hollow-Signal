using System;
using System.Collections.Generic;
using Narrative.Localization;
using UnityEngine;
using World.Actors.Player;
using Random = UnityEngine.Random;

namespace Data{
    /// Catalog definition of an action style / perk, its associated skills, gating, and quip pools.
    [Serializable]
    public class ActionDefinition{
        public ActionType actionType;
        public string nameKey;
        public PerkRequirementMode perkRequirement = PerkRequirementMode.ShownWhenLocked;
        public List<Skill> applicableSkills = new();
        public List<string> successQuipKeys = new();
        public List<string> failureQuipKeys = new();

        /// Retrieves localized display name.
        public string LocalizedName => LocalizationManager.Get("actions", nameKey);

        /// Evaluates whether the acting hero satisfies the perk tool requirement.
        public bool CanAttempt(CharacterSheet sheet) =>
            perkRequirement == PerkRequirementMode.None || sheet.HasPerk(actionType);

        /// Drafts a random success quip from the available pool.
        public string GetRandomSuccessQuip() =>
            LocalizationManager.Get("actions", successQuipKeys[Random.Range(0, successQuipKeys.Count)]);

        /// Drafts a random failure quip from the available pool.
        public string GetRandomFailureQuip() =>
            LocalizationManager.Get("actions", failureQuipKeys[Random.Range(0, failureQuipKeys.Count)]);
    }
}
