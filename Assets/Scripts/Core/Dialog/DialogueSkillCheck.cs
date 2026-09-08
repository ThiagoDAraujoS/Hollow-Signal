using System;
using Data;

namespace Core.Dialog{
    /// Outcome branch of a skill test executed within a dialogue knot.
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
        /// Skill tested by this challenge.
        public Skill skill = Skill.None;

        /// Target difficulty check rating needed to pass.
        public int targetDc;

        /// Outcome branch taken when the roll meets or exceeds targetDc.
        public DialogueOutcome onSuccess;

        /// Outcome branch taken when the roll fails to meet targetDc.
        public DialogueOutcome onFailure;

        /// Optional outcome branch taken on a critical success roll.
        public DialogueOutcome onCriticalSuccess;

        /// Optional outcome branch taken on a critical failure roll.
        public DialogueOutcome onCriticalFailure;
    }
}
