using System;

namespace Core.Dialog{
    /// Represents a player choice option within a dialogue node.
    public class DialogueChoice{
        /// Unique stable identifier for this choice used to track consumption.
        public string choiceId;

        /// Localization string key looked up via TextRegistry.
        public string textKey;

        /// True if this choice can only be selected once and disappears after being picked.
        public bool isOneShot;

        /// Evaluated boolean predicate determining if this choice should be displayed.
        public Func<bool> isVisible;

        /// Optional item identifier required in the player's inventory to select this choice.
        public string requiredItemId;

        /// Quantity of the required item needed to select this choice.
        public int requiredItemAmount;

        /// Name of the destination knot to transition to when selected.
        public string targetKnot;

        /// Optional side effect callback invoked when the player confirms this choice.
        public Action onSelect;
    }
}
