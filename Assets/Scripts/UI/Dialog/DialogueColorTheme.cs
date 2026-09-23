using System;
using UnityEngine;

namespace UI.Dialog{
    /// Central palette compendium for dialogue styling, tactical crisis resource costs, and transcripts.
    public static class DialogueColorTheme{
        [Header("Default Exploration Choice Colors")]
        public static string ChoiceIndexColor = "#854205";
        public static string ChoiceDefaultTextColor = "#1C1814";

        [Header("Tactical Crisis Resource Consumption Colors")]
        public static string ActionCostColor = "#E67E22";      // Orange (<!> - Consumes Action)
        public static string EndTurnCostColor = "#E74C3C";     // Red (<!!> - Ends Turn)
        public static string MoveCostColor = "#2980B9";        // Blue (<M> - Consumes Move & Burns Dash)
        public static string DisabledChoiceColor = "#7F8C8D";  // Gray (Unavailable / Resource Spent)

        [Header("Hover Tooltip Colors")]
        public static Color TooltipBackgroundColor = new(0.1f, 0.09f, 0.08f, 0.95f);
        public static Color TooltipBorderColor = new(0.52f, 0.26f, 0.02f, 0.9f);
        public static string TooltipTextColor = "#F5D76E";
        public static string TooltipDisabledTextColor = "#E74C3C";

        [Header("Transcript & Speaker Styling")]
        public static string SpeakerHeaderColor = "#143447";      // Deep Prussian Navy
        public static string ActionTagColor = "#143447";          // Action style bracket
        public static string RelevantMasteryColor = "#854205";    // Amber Gold banner
        public static string MasteryDescriptionColor = "#38322B"; // Vintage Charcoal Sepia
        public static string CheckPassedColor = "#114A27";        // Emerald Forest Pass
        public static string CheckFailedColor = "#8B0000";        // Crimson Rust Fail
        public static string CheckRollDetailsColor = "#5C5248";   // Muted dice arithmetic
        public static string QuipTextColor = "#1C1814";           // In-universe speech/quip
    }
}
