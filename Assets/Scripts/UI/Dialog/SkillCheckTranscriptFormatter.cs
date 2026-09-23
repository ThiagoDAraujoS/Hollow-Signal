using System.Text;
using Narrative.Skills;

namespace UI.Dialog{
    /// Formats skill evaluation outcomes and mastery context into rich-text dialogue transcript blocks.
    public static class SkillCheckTranscriptFormatter{
        /// Assembles formatted rich-text string from a skill check result according to DialogueColorTheme.
        public static string Format(SkillEvaluationResult result){
            StringBuilder sb = new();
            sb.AppendLine($"<b><color={DialogueColorTheme.ActionTagColor}>[ACTION: {result.context.style.actionType.ToString().ToUpperInvariant()}]</color></b>");

            if (result.context.contributingMastery != null){
                sb.AppendLine($"<b><color={DialogueColorTheme.RelevantMasteryColor}>:: RELEVANT MASTERY: {result.context.contributingMastery.LocalizedName.ToUpperInvariant()} ::</color></b>");
                sb.AppendLine($"<i><color={DialogueColorTheme.MasteryDescriptionColor}>{result.context.contributingMastery.LocalizedDescription}</color></i>");
            }

            string outcomeTag = result.passed
                ? $"<color={DialogueColorTheme.CheckPassedColor}>[PASSED]</color>"
                : $"<color={DialogueColorTheme.CheckFailedColor}>[FAILED]</color>";

            sb.AppendLine($"<b>{outcomeTag} <color={DialogueColorTheme.CheckRollDetailsColor}>// Roll: {string.Join(" + ", result.dice)} + {result.context.skillBonus} = {result.finalTotal} vs DC {result.context.targetDc}</color></b>");

            if (!string.IsNullOrEmpty(result.selectedQuip))
                sb.AppendLine($"<color={DialogueColorTheme.QuipTextColor}>\"{result.selectedQuip}\"</color>");

            return sb.ToString();
        }
    }
}
