using System;
using System.Collections.Generic;
using Data;

namespace Narrative.Dialog{
    /// Outcome branch of a skill test executed within a dialogue knot.
    [Serializable]
    public class DialogueOutcome{
        /// Identifier of the speaker delivering this outcome line.
        public string speakerId;

        /// Localization string key looked up via TextRegistry.
        public string textKey;

        /// Side effect action executed when transitioning into this outcome.
        public Action onExecute;

        /// Destination knot to segue into after this outcome is displayed.
        public string targetKnot;
    }

    /// Represents a die skill check evaluation required by a dialogue knot.
    public class DialogueSkillCheck{
        /// Legacy direct skill definition for backwards compatibility with generated scripts.
        public Skill skill = Skill.None;

        /// Action style / perk category required for this check.
        public ActionType actionType = ActionType.None;

        /// Tri-state perk requirement visibility gating for this check.
        public PerkRequirementMode perkRequirement = PerkRequirementMode.None;

        /// Target Difficulty Class (Cypher DC = difficulty * 3).
        public int targetDc;

        /// Specific skills that can contribute to this check, evaluated with max(). If empty, defaults to actionType.
        public List<Skill> applicableSkills = new();

        /// Optional overrides for action success quip keys on this specific knot.
        public List<string> successQuipKeys = new();

        /// Optional overrides for action failure quip keys on this specific knot.
        public List<string> failureQuipKeys = new();

        /// Branch executed when check succeeds.
        public DialogueOutcome onSuccess;

        /// Branch executed when check fails.
        public DialogueOutcome onFailure;

        /// Branch executed on critical success (natural 20). Falls back to onSuccess if null.
        public DialogueOutcome onCriticalSuccess;

        /// Branch executed on critical failure (natural 1). Falls back to onFailure if null.
        public DialogueOutcome onCriticalFailure;

        /// Synthesizes an ActionStyle instance from this knot check configuration.
        public ActionStyle GetEffectiveActionStyle(){
            ActionDefinition def = ActionDatabase.Instance.Get(actionType);
            List<Skill> skills = applicableSkills.Count > 0 ? applicableSkills : def.applicableSkills;

            return new(){
                actionType = actionType,
                perkRequirement = perkRequirement,
                targetDc = targetDc,
                applicableSkills = skills,
                successQuipKeys = successQuipKeys.Count > 0 ? successQuipKeys : def.successQuipKeys,
                failureQuipKeys = failureQuipKeys.Count > 0 ? failureQuipKeys : def.failureQuipKeys
            };
        }
    }
}
