using System.Collections.Generic;

namespace Editor.Dialog.Parser{
    /// Represents an individual conversation knot block within a .dialog script.
    public class DialogKnotDef{
        /// Unique name identifier for this knot.
        public string knotId;

        /// Identifier of the speaker delivering the prompt line.
        public string speakerId;

        /// Optional emotional mood tag for portrait display.
        public string portraitMood;

        /// Spoken dialogue prompt text.
        public string promptText;

        /// Collection of player options available from this knot.
        public List<DialogChoiceDef> choices = new();

        /// Optional skill challenge evaluated when entering this knot.
        public DialogSkillCheckDef skillCheck;

        /// In-line commands or mutations executed upon entering this knot.
        public List<string> inLineCommands = new();
    }
}
