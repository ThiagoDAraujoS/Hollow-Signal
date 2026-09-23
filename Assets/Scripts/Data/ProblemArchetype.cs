using System;
using System.Collections.Generic;
using Narrative.Localization;
using UnityEngine;

namespace Data{
    /// Specifies an allowed action approach and its difficulty level offset relative to base problem level.
    [Serializable]
    public class ProblemActionEntry{
        public ActionType actionType;
        public int levelOffset;
    }

    [Serializable]
    public class ActionStyle{
        public ActionType actionType;
        public PerkRequirementMode perkRequirement = PerkRequirementMode.ShownWhenLocked;
        public int targetDc;
        public List<Skill> applicableSkills = new();
        public List<string> successQuipKeys = new();
        public List<string> failureQuipKeys = new();

        /// Evaluates whether the acting character satisfies the tool perk requirement.
        public bool CanAttempt(World.Actors.Player.CharacterSheet sheet) =>
            perkRequirement == PerkRequirementMode.None || sheet.HasPerk(actionType);

        /// Drafts a random success quip from the available pool.
        public string GetRandomSuccessQuip() =>
            LocalizationManager.Get("actions", successQuipKeys[UnityEngine.Random.Range(0, successQuipKeys.Count)]);

        /// Drafts a random failure quip from the available pool.
        public string GetRandomFailureQuip() =>
            LocalizationManager.Get("actions", failureQuipKeys[UnityEngine.Random.Range(0, failureQuipKeys.Count)]);
    }

    /// Represents an obstacle archetype with a collection of allowed actions and difficulty offsets.
    [Serializable]
    public class ProblemArchetype{
        public string archetypeId;
        public string nameKey;
        public string descKey;
        public List<ProblemActionEntry> actions = new();

        public string Id => archetypeId;
        public string NameKey => nameKey;
        public string DescKey => descKey;
        public IReadOnlyList<ProblemActionEntry> Actions => actions;

        /// Retrieves localized display name.
        public string LocalizedName => LocalizationManager.Get("archetypes", nameKey);

        /// Retrieves localized description.
        public string LocalizedDescription => LocalizationManager.Get("archetypes", descKey);
    }
}
