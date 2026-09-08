using System.Collections.Generic;

namespace Editor.Dialog.Parser{
    /// Root Abstract Syntax Tree container representing a fully parsed .dialog file.
    public class DialogScriptAst{
        /// Base name of the script without extension (e.g. "MedicalBayConsole").
        public string scriptName;

        /// Optional name of the map/scene this dialogue is bound to (e.g. "MedicalBay").
        /// When specified, localization is directed into Assets/StreamingAssets/Localization/Scenes/<mapName>_en.txt.
        public string mapName;

        /// Collection of all variable declarations found in the script header.
        public List<DialogVarDef> variables = new();

        /// Collection of all conversation knots contained in the script.
        public List<DialogKnotDef> knots = new();
    }
}
