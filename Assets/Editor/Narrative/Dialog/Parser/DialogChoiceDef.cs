namespace Editor.Dialog.Parser{
    /// Structural AST representation of a choice option belonging to a dialogue knot.
    public class DialogChoiceDef{
        /// Stable identifier assigned during compilation (e.g. KNOTNAME_C00).
        public string choiceId;

        /// Raw display text before tag stripping.
        public string text;

        /// Raw conditional expression guarding visibility (e.g. !is_locked).
        public string condition;

        /// True if this choice is consumed and disappears after first selection.
        public bool isOneShot;

        /// Optional event name emitted when selected (e.g. [Option](OnEventName) -> Knot).
        public string eventName;

        /// Optional item identifier required in the party inventory to pick this choice.
        public string requiredItemId;

        /// Quantity of the required item needed.
        public int requiredItemAmount;

        /// Name of the target knot this choice transitions into.
        public string targetKnot;

        /// True if this choice consumes the player's major action (<!>).
        public bool consumesAction;

        /// True if this choice immediately concludes the player's turn (<!!>).
        public bool endsTurn;

        /// True if this choice consumes movement and burns sprint/dash (<M>).
        public bool consumesMove;

        /// True if this choice frees the workstation slot and relocates to a fallback slot (<F>).
        public bool freesSlot;
    }
}
