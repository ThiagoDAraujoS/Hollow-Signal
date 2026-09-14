using System.Collections.Generic;

namespace Editor.Dialog.Parser{
    /// Root Abstract Syntax Tree container representing a fully parsed .dialog file.
    public class DialogScriptAst{
        /// Base name of the script without extension (e.g. "MedicalBayConsole").
        public string scriptName;

        /// Optional name of the map/scene this dialogue is bound to (e.g. "MedicalBay").
        public string mapName;

        /// Target localization file name (without extension, e.g. "MedicalBay_terminals" or "DrVance").
        /// Multiple dialogues can share the same locFileName and will be batched into the same .txt file.
        public string locFileName;

        /// Collection of all variable declarations found in the script header.
        public List<DialogVarDef> variables = new();

        /// Collection of all conversation knots contained in the script.
        public List<DialogKnotDef> knots = new();
    }
}
