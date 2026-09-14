using Data;

namespace Editor.Dialog.Parser{
    /// Outcome branch of a skill test defined within a dialogue knot.
    public class DialogOutcomeDef{
        /// Identifier of the speaker delivering this outcome line.
        public string speakerId;

        /// Spoken line text delivered when this outcome occurs.
        public string text;

        /// In-line commands or mutations executed upon taking this branch.
        public string command;

        /// Target destination knot to transition to after this outcome.
        public string targetKnot;
    }

    /// Represents a die skill check evaluation required by a dialogue knot.
    public class DialogSkillCheckDef{
        /// Skill tested by this challenge.
        public Skill skill = Skill.None;

        /// Target difficulty check rating needed to pass.
        public int targetDc;

        /// Outcome branch taken when the roll meets or exceeds targetDc.
        public DialogOutcomeDef onSuccess;

        /// Outcome branch taken when the roll fails to meet targetDc.
        public DialogOutcomeDef onFailure;

        /// Optional outcome branch taken on a critical success roll.
        public DialogOutcomeDef onCriticalSuccess;

        /// Optional outcome branch taken on a critical failure roll.
        public DialogOutcomeDef onCriticalFailure;
    }
}
