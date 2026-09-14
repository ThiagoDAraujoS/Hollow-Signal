namespace Editor.Dialog.Parser{
    /// Represents a parsed choice option within a conversation knot.
    public class DialogChoiceDef{
        /// Stable deterministic identifier used to track consumption of this choice.
        public string choiceId;

        /// True if this choice can only be picked once and disappears (*), false if sticky (+).
        public bool isOneShot;

        /// Raw conditional expression string governing visibility (e.g. "!is_door_open.Value").
        public string condition;

        /// Localized text string displayed on the choice button.
        public string text;

        /// Optional item identifier required in the party inventory to pick this choice.
        public string requiredItemId;

        /// Quantity of the required item needed.
        public int requiredItemAmount;

        /// Name of the target knot this choice transitions into.
        public string targetKnot;
    }
}
