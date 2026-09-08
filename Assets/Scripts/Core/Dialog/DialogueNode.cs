using System;
using System.Collections.Generic;

namespace Core.Dialog{
    /// Represents an active conversation state/knot in a dialogue graph.
    public class DialogueNode{
        /// Standard termination constant signaling dialogue should close.
        public const string End = "END";

        /// Unique name identifier for this knot.
        public string knotId;

        /// Identifier of the speaker delivering the prompt line.
        public string speakerId;

        /// Optional emotional mood tag for portrait display.
        public string portraitMood;

        /// Localization string key looked up via TextRegistry for the prompt line.
        public string textKey;

        /// Side effect action executed immediately when entering this knot.
        public Action onEnter;

        /// Collection of player options available from this knot.
        public List<DialogueChoice> choices = new();

        /// Optional skill challenge evaluated when entering this knot.
        public DialogueSkillCheck skillCheck;
    }
}
