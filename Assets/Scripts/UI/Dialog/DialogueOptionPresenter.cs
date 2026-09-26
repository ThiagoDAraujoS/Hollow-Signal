using System;
using System.Text.RegularExpressions;
using Core.Crisis;
using Data;
using Narrative.Dialog;
using UnityEngine;
using UnityEngine.UI;
using World.Actors.Player;

namespace UI.Dialog{
    /// Evaluates dialogue choice availability, tactical crisis resource costs, perks, and populates option UI buttons.
    [DisallowMultipleComponent]
    public class DialogueOptionPresenter : MonoBehaviour{
        [SerializeField] private RectTransform optionsContent;
        [SerializeField] private ScrollRect optionsScrollRect;
        [SerializeField] private DialogueOptionUI optionPrefab;
        [SerializeField] private DialogueTooltipUI tooltip;

        /// Discovers scroll rect, content rect, and tooltip on awake if unassigned.
        private void Awake(){
            if (!optionsScrollRect) optionsScrollRect = GetComponent<ScrollRect>() ?? GetComponentInChildren<ScrollRect>();
            if (!optionsContent && optionsScrollRect) optionsContent = optionsScrollRect.content;
            if (!tooltip) tooltip = GetComponentInParent<DialogueTooltipUI>() ?? GetComponentInChildren<DialogueTooltipUI>() ?? gameObject.AddComponent<DialogueTooltipUI>();
        }

        /// Configures runtime references directly.
        public void Configure(RectTransform content, ScrollRect scrollRect, DialogueOptionUI prefab, DialogueTooltipUI tt){
            optionsContent = content;
            optionsScrollRect = scrollRect;
            optionPrefab = prefab;
            tooltip = tt;
        }

        /// Populates regular dialogue choices with tactical cost formatting and availability.
        public void PresentChoices(DialogueNode node, DialogueBehaviour dialogue, CharacterSheet actor, Action<DialogueChoice> onChoiceSelected){
            Clear();
            int index = 1;
            bool isCrisis = CrisisManager.Instance && CrisisManager.Instance.IsCrisis;
            CrisisTurn turn = isCrisis && actor != null ? actor.CrisisTurn : null;

            foreach (DialogueChoice choice in node.choices){
                if (choice.isVisible != null && !choice.isVisible()) continue;

                bool lacksPerk = choice.perkRequirement != PerkRequirementMode.None
                                 && choice.requiredPerk != ActionType.None
                                 && (actor == null || !actor.HasPerk(choice.requiredPerk));

                if (lacksPerk && choice.perkRequirement == PerkRequirementMode.HiddenWhenLocked) continue;

                string rawText = dialogue.GetLocalizedString(choice.textKey);
                if (string.IsNullOrEmpty(rawText)) rawText = choice.textKey;

                bool choiceConsumesAction = choice.consumesAction;
                bool choiceEndsTurn = choice.endsTurn;
                bool choiceConsumesMove = choice.consumesMove;

                string cleanText = rawText;
                ExtractTags(ref cleanText, ref choiceConsumesAction, ref choiceEndsTurn, ref choiceConsumesMove);

                choice.consumesAction = choiceConsumesAction;
                choice.endsTurn = choiceEndsTurn;
                choice.consumesMove = choiceConsumesMove;

                bool isInteractable = true;
                string label;
                string tooltipMsg = null;

                if (lacksPerk && choice.perkRequirement == PerkRequirementMode.ShownWhenLocked){
                    isInteractable = false;
                    label = $"<b><color={DialogueColorTheme.DisabledChoiceColor}>[{index}]</color></b> <color={DialogueColorTheme.DisabledChoiceColor}>{cleanText}</color>";
                    tooltipMsg = $"Requires Perk: {choice.requiredPerk}";
                }
                else if (isCrisis && turn != null){
                    bool actionAvailable = !turn.HasActed;
                    bool moveAvailable = !turn.HasMoved;

                    bool canAfford = true;
                    if ((choiceEndsTurn || choiceConsumesAction) && !actionAvailable) canAfford = false;
                    if (choiceConsumesMove && !moveAvailable) canAfford = false;

                    if (!canAfford){
                        isInteractable = false;
                        label = $"<b><color={DialogueColorTheme.DisabledChoiceColor}>[{index}]</color></b> <color={DialogueColorTheme.DisabledChoiceColor}>{cleanText}</color>";
                        if (choiceEndsTurn) tooltipMsg = "Unavailable: Action already used (Ends Turn)";
                        else if (choiceConsumesAction) tooltipMsg = "Unavailable: Action already used this turn";
                        else if (choiceConsumesMove) tooltipMsg = "Unavailable: Movement already used this turn";
                    }
                    else{
                        isInteractable = true;
                        string costColor = DialogueColorTheme.ChoiceDefaultTextColor;
                        bool isResourceChoice = false;

                        if (choiceEndsTurn){
                            costColor = DialogueColorTheme.EndTurnCostColor;
                            isResourceChoice = true;
                            tooltipMsg = "Ends Turn (Consumes Action, Move, and Dash)";
                        }
                        else if (choiceConsumesAction){
                            costColor = DialogueColorTheme.ActionCostColor;
                            isResourceChoice = true;
                            tooltipMsg = choiceConsumesMove ? "Consumes Action & Move (Dash Disabled)" : "Consumes Major Action";
                        }
                        else if (choiceConsumesMove){
                            costColor = DialogueColorTheme.MoveCostColor;
                            isResourceChoice = true;
                            tooltipMsg = "Consumes Movement (Dash / Double Move disabled)";
                        }

                        string textFormatted = isResourceChoice ? $"<b><color={costColor}>{cleanText}</color></b>" : $"<color={costColor}>{cleanText}</color>";
                        label = $"<b><color={DialogueColorTheme.ChoiceIndexColor}>[{index}]</color></b> {textFormatted}";
                    }
                }
                else{
                    isInteractable = true;
                    label = $"<b><color={DialogueColorTheme.ChoiceIndexColor}>[{index}]</color></b> <color={DialogueColorTheme.ChoiceDefaultTextColor}>{cleanText}</color>";
                }

                DialogueOptionUI option = Instantiate(optionPrefab, optionsContent);
                DialogueChoice capturedChoice = choice;
                option.Initialize(label, () => onChoiceSelected(capturedChoice), isInteractable, tooltipMsg, ShowTooltip, HideTooltip);
                index++;
            }

            if (optionsContent) LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContent);
            Canvas.ForceUpdateCanvases();
            if (optionsScrollRect) optionsScrollRect.verticalNormalizedPosition = 1f;
        }

        /// Populates problem approach options generated from a problem archetype blueprint.
        public void PresentProblemOptions(ProblemArchetype archetype, DialogueProblem problem, CharacterSheet actor, Action<ProblemActionEntry, ActionDefinition, int> onSelect){
            Clear();
            int index = 1;
            ActionDatabase actionDb = ActionDatabase.Instance;

            foreach (ProblemActionEntry entry in archetype.Actions){
                ActionDefinition actionDef = actionDb.Get(entry.actionType);
                int effectiveLevel = Mathf.Max(1, problem.baseLevel + entry.levelOffset);
                int targetDc = effectiveLevel * 3;

                bool lacksPerk = actionDef.perkRequirement != PerkRequirementMode.None && (actor == null || !actor.HasPerk(entry.actionType));
                if (lacksPerk && actionDef.perkRequirement == PerkRequirementMode.HiddenWhenLocked) continue;

                DialogueOptionUI option = Instantiate(optionPrefab, optionsContent);
                string approachName = actionDef.LocalizedName;
                string dcTag = $"<color={DialogueColorTheme.CheckRollDetailsColor}>[DC {targetDc}]</color>";

                bool isInteractable = true;
                string label;
                string tooltipMsg = null;

                if (lacksPerk && actionDef.perkRequirement == PerkRequirementMode.ShownWhenLocked){
                    isInteractable = false;
                    label = $"<b><color={DialogueColorTheme.DisabledChoiceColor}>[{index}]</color></b> <color={DialogueColorTheme.DisabledChoiceColor}>{approachName} {dcTag}</color>";
                    tooltipMsg = $"Requires Perk: {entry.actionType}";
                }
                else label = $"<b><color={DialogueColorTheme.ChoiceIndexColor}>[{index}]</color></b> <color={DialogueColorTheme.ChoiceDefaultTextColor}>{approachName}</color> {dcTag}";

                ProblemActionEntry capturedEntry = entry;
                ActionDefinition capturedDef = actionDef;
                int capturedDc = targetDc;

                option.Initialize(label, () => onSelect(capturedEntry, capturedDef, capturedDc), isInteractable, tooltipMsg, ShowTooltip, HideTooltip);
                index++;
            }

            if (optionsContent) LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContent);
            Canvas.ForceUpdateCanvases();
            if (optionsScrollRect) optionsScrollRect.verticalNormalizedPosition = 1f;
        }

        /// Destroys all current option GameObjects.
        public void Clear(){
            HideTooltip();
            if (optionsContent){
                foreach (Transform child in optionsContent) Destroy(child.gameObject);
            }
        }

        /// Displays hover tooltip.
        private void ShowTooltip(Vector2 pos, string text){
            if (tooltip) tooltip.Show(pos, text);
        }

        /// Hides active tooltip.
        private void HideTooltip(){
            if (tooltip) tooltip.Hide();
        }

        /// Extracts tactical gameplay tags (<!>, <!!>, <M>) from raw choice text.
        public static void ExtractTags(ref string text, ref bool consumesAction, ref bool endsTurn, ref bool consumesMove){
            if (string.IsNullOrEmpty(text)) return;

            if (text.Contains("<!!>")){
                endsTurn = true;
                text     = text.Replace("<!!>", "").Trim();
            }
            else if (text.Contains("<!>")){
                consumesAction = true;
                text           = text.Replace("<!>", "").Trim();
            }

            Match moveMatch = Regex.Match(text, @"<[mM]>");
            if (moveMatch.Success){
                consumesMove = true;
                text         = Regex.Replace(text, @"<[mM]>", "").Trim();
            }
        }
    }
}
