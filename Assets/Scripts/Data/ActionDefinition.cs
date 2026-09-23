using System;
using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;
using Random = UnityEngine.Random;

namespace Data{
    /// Catalog definition of an action style / perk, its associated skills, gating, and quip pools.
    [Serializable]
    public class ActionDefinition{
        public ActionType actionType;
        public string displayName;
        public PerkRequirementMode perkRequirement = PerkRequirementMode.ShownWhenLocked;
        public List<Skill> applicableSkills = new();

        [TextArea(2, 3)]
        public List<string> successQuips = new();

        [TextArea(2, 3)]
        public List<string> failureQuips = new();

        /// Evaluates whether the acting hero satisfies the perk tool requirement.
        public bool CanAttempt(CharacterSheet sheet) =>
            perkRequirement == PerkRequirementMode.None || sheet.HasPerk(actionType);

        /// Drafts a random success quip from the available list.
        public string GetRandomSuccessQuip() =>
            successQuips.Count > 0 ? successQuips[Random.Range(0, successQuips.Count)] : string.Empty;

        /// Drafts a random failure quip from the available list.
        public string GetRandomFailureQuip() =>
            failureQuips.Count > 0 ? failureQuips[Random.Range(0, failureQuips.Count)] : string.Empty;
    }
}
