using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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

        [TextArea(2, 3)]
        public List<string> successQuips = new();

        [TextArea(2, 3)]
        public List<string> failureQuips = new();

        /// Evaluates whether the acting character satisfies the tool perk requirement.
        public bool CanAttempt(World.Actors.Player.CharacterSheet sheet) =>
            perkRequirement == PerkRequirementMode.None || sheet.HasPerk(actionType);

        /// Drafts a random success quip from the available list.
        public string GetRandomSuccessQuip() =>
            successQuips.Count > 0 ? successQuips[Random.Range(0, successQuips.Count)] : string.Empty;

        /// Drafts a random failure quip from the available list.
        public string GetRandomFailureQuip() =>
            failureQuips.Count > 0 ? failureQuips[Random.Range(0, failureQuips.Count)] : string.Empty;
    }

    /// Represents an obstacle archetype with a collection of allowed actions and difficulty offsets.
    [CreateAssetMenu(fileName = "NewProblemArchetype", menuName = "CRPG/Problem Archetype")]
    public class ProblemArchetype : ScriptableObject{
        [SerializeField] private string archetypeId;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private List<ProblemActionEntry> actions = new();
        [SerializeField] private List<ActionStyle> actionStyles = new();

        public string Id => archetypeId;
        public string Description => description;
        public IReadOnlyList<ProblemActionEntry> Actions => actions;
        public IReadOnlyList<ActionStyle> ActionStyles => actionStyles;

        /// Returns the action style matching the given action type.
        public ActionStyle GetActionStyle(ActionType type){
            foreach (ActionStyle style in actionStyles)
                if (style.actionType == type)
                    return style;
            return null;
        }
    }
}
