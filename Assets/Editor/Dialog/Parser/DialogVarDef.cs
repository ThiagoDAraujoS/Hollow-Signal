namespace Editor.Dialog.Parser{
    /// Represents an individual variable definition declared at the top of a .dialog file.
    public class DialogVarDef{
        /// Scope of the variable:
        /// "local" (entity instance partition),
        /// "map" (current level/area partition),
        /// "global" (game session persistent partition).
        public string scope;

        /// Data type name ("bool", "int", "float", "string").
        public string typeName;

        /// Identifier name of the variable.
        public string name;

        /// Default literal value string as declared in the script.
        public string defaultValue;
    }
}
