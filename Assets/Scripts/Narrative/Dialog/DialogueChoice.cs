using System;
using Data;

namespace Narrative.Dialog{
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

        /// Optional perk capability required to select this choice.
        public ActionType requiredPerk = ActionType.None;

        /// Visibility gating mode applied when the required perk is missing.
        public PerkRequirementMode perkRequirement = PerkRequirementMode.None;

        /// Name of the destination knot to transition to when selected.
        public string targetKnot;

        /// Optional side effect callback invoked when the player confirms this choice.
        public Action onSelect;

        /// When selected during a tactical Crisis, consumes the acting character's major action (<!> tag).
        public bool consumesAction;

        /// When selected during a tactical Crisis, concludes character's turn budget, consuming action, movement, and sprint (<!!> tag).
        public bool endsTurn;

        /// When selected during a tactical Crisis, consumes movement and burns double-move/sprint opportunity (<M> tag).
        public bool consumesMove;
    }
}
