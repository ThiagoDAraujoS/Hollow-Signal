using System;
using System.Text.RegularExpressions;
using Core.Crisis;
using Data;
using Narrative.Dialog;
using Narrative.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using World.Actors.Player;

namespace UI.Dialog{
    /// Controls dialogue window presentation, transcript logging, choice selection, and tactical turn resource costs.
    [DisallowMultipleComponent]
    public class DialogueController : MonoBehaviour{
        public static event Action<bool> OnDialogueActiveChanged;

        [Header("Containers")]
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private RectTransform transcriptContent;
        [SerializeField] private RectTransform optionsContent;

        [Header("Scroll Rects")]
        [SerializeField] private ScrollRect transcriptScrollRect;
        [SerializeField] private ScrollRect optionsScrollRect;

        [Header("Prefabs")]
        [SerializeField] private GameObject transcriptPrefab;
        [SerializeField] private DialogueOptionUI optionPrefab;

        private DialogueBehaviour        _currentDialogue;
        private DialogueNode             _currentNode;
        private CharacterDialogueSession _characterSession;

        // Dynamic Tooltip UI
        private GameObject      _tooltipRoot;
        private RectTransform   _tooltipRect;
        private TextMeshProUGUI _tooltipText;

        public static DialogueController Instance{ get; private set; }

        public GameObject DialogRoot => dialogRoot;
        public bool       IsOpen     => gameObject.activeSelf && (dialogRoot == null || dialogRoot.activeInHierarchy);

        /// Assigns singleton instance.
        private void Awake() => Instance = this;

        /// Dismisses active tooltip on disable.
        private void OnDisable() => HideTooltip();

        /// Shows or hides the dialogue screen without clearing history.
        public void SetVisible(bool visible){
            gameObject.SetActive(visible);

            if (!visible)
                HideTooltip();

            if (dialogRoot == null || dialogRoot == gameObject)
                return;

            if (visible)
                dialogRoot.SetActive(true);
            else if (dialogRoot.GetComponentsInChildren<DialogueController>(false).Length == 0)
                dialogRoot.SetActive(false);
        }

        /// Starts dialogue session on this screen and navigates to the entry knot.
        public void BeginDialogue(DialogueBehaviour dialogue, string startingKnot, CharacterDialogueSession session){
            _currentDialogue  = dialogue;
            _characterSession = session;

            SetVisible(true);
            OnDialogueActiveChanged?.Invoke(true);

            ClearTranscript();
            ClearOptions();
            HideTooltip();

            GoToKnot(startingKnot);
        }

        /// Closes dialogue session, clears options, and destroys transcript entries to free memory.
        public void EndDialogue(){
            HideTooltip();
            ClearOptions();
            ClearTranscript();

            if (_characterSession != null){
                _characterSession.Clear();
                _characterSession = null;
            }

            _currentDialogue = null;
            _currentNode     = null;

            SetVisible(false);
            OnDialogueActiveChanged?.Invoke(false);
        }

        /// Navigates to a specific knot in the active dialogue graph.
        public void GoToKnot(string knotId){
            if (knotId == DialogueNode.End){
                EndDialogue();
                return;
            }

            if (_characterSession != null)
                _characterSession.SetKnot(_currentDialogue, knotId);

            _currentNode = _currentDialogue != null ? _currentDialogue.GetNode(knotId) : null;
            if (_currentNode == null){
                Debug.LogError($"[DialogueController] Could not find knot '{knotId}' in active dialogue.");
                return;
            }

            _currentNode.onEnter?.Invoke();

            DisplayPrompt(_currentNode);

            if (_currentNode.problem != null)
                DisplayProblemChoices(_currentNode.problem);
            else if (_currentNode.skillCheck != null)
                ExecuteSkillCheck(_currentNode.skillCheck);
            else
                DisplayChoices(_currentNode);
        }

        /// Appends an arbitrary formatted rich-text entry directly into the transcript log.
        public void AddTranscriptEntry(string formattedText){
            GameObject entry = Instantiate(transcriptPrefab, transcriptContent);
            entry.GetComponent<TextMeshProUGUI>().text = formattedText;
            Canvas.ForceUpdateCanvases();
            transcriptScrollRect.verticalNormalizedPosition = 0f;
        }

        /// Evaluates knot skill challenge and displays transcript before advancing dialogue.
        private void ExecuteSkillCheck(DialogueSkillCheck skillCheck){
            ClearOptions();
            HideTooltip();

            ActionStyle style = skillCheck.GetEffectiveActionStyle();
            CharacterSheet actor = _characterSession != null && _characterSession.Character != null
                ? _characterSession.Character.sheet
                : null;

            if (!style.CanAttempt(actor)){
                AddTranscriptEntry($"<b><color={DialogueColorTheme.CheckFailedColor}>[CANNOT ATTEMPT: Requires {style.actionType} Perk]</color></b>");
                DialogueOutcome failOutcome = skillCheck.onFailure;
                if (failOutcome != null){
                    failOutcome.onExecute?.Invoke();
                    if (!string.IsNullOrEmpty(failOutcome.textKey)){
                        string text = _currentDialogue.GetLocalizedString(failOutcome.textKey);
                        if (string.IsNullOrEmpty(text))
                            text = failOutcome.textKey;

                        string formatted = string.IsNullOrEmpty(failOutcome.speakerId)
                            ? text
                            : $"<b><color={DialogueColorTheme.SpeakerHeaderColor}>[{failOutcome.speakerId}]</color></b>\n{text}";

                        AddTranscriptEntry(formatted);
                    }

                    if (!string.IsNullOrEmpty(failOutcome.targetKnot))
                        GoToKnot(failOutcome.targetKnot);
                }
                return;
            }

            SkillEvaluator.Evaluate(actor, style, result => {
                string transcript = SkillCheckTranscriptFormatter.Format(result);
                AddTranscriptEntry(transcript);

                DialogueOutcome outcome = result.passed ? skillCheck.onSuccess : skillCheck.onFailure;
                if (outcome != null){
                    outcome.onExecute?.Invoke();
                    if (!string.IsNullOrEmpty(outcome.textKey)){
                        string text = _currentDialogue.GetLocalizedString(outcome.textKey);
                        if (string.IsNullOrEmpty(text))
                            text = outcome.textKey;

                        string formatted = string.IsNullOrEmpty(outcome.speakerId)
                            ? text
                            : $"<b><color={DialogueColorTheme.SpeakerHeaderColor}>[{outcome.speakerId}]</color></b>\n{text}";

                        AddTranscriptEntry(formatted);
                    }

                    if (!string.IsNullOrEmpty(outcome.targetKnot))
                        GoToKnot(outcome.targetKnot);
                }
            });
        }

        /// Displays dynamically resolved problem choices generated from a ProblemArchetype blueprint.
        private void DisplayProblemChoices(DialogueProblem problem){
            ClearOptions();
            HideTooltip();

            if (problem == null || string.IsNullOrEmpty(problem.archetypeId)){
                Debug.LogError("[DialogueController] Invalid or missing Problem definition in knot.");
                return;
            }

            ProblemArchetypeDatabase archetypeDb = ProblemArchetypeDatabase.Instance;
            if (archetypeDb == null){
                Debug.LogError("[DialogueController] ProblemArchetypeDatabase.Instance could not be found or loaded.");
                return;
            }

            ProblemArchetype archetype = archetypeDb.Get(problem.archetypeId);
            if (archetype == null){
                Debug.LogError($"[DialogueController] Problem archetype '{problem.archetypeId}' not found in ProblemArchetypeDatabase.");
                return;
            }

            int index = 1;
            CharacterSheet actor = _characterSession != null && _characterSession.Character != null
                ? _characterSession.Character.sheet
                : null;

            ActionDatabase actionDb = ActionDatabase.Instance;

            foreach (ProblemActionEntry entry in archetype.Actions){
                ActionDefinition actionDef = actionDb != null ? actionDb.Get(entry.actionType) : null;
                if (actionDef == null){
                    Debug.LogWarning($"[DialogueController] ActionDefinition for '{entry.actionType}' not found in ActionDatabase.");
                    continue;
                }

                int effectiveLevel = Mathf.Max(1, problem.baseLevel + entry.levelOffset);
                int targetDc = effectiveLevel * 3;

                bool lacksPerk = actionDef.perkRequirement != PerkRequirementMode.None
                                 && (actor == null || !actor.HasPerk(entry.actionType));

                if (lacksPerk && actionDef.perkRequirement == PerkRequirementMode.HiddenWhenLocked)
                    continue;

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
                else
                    label = $"<b><color={DialogueColorTheme.ChoiceIndexColor}>[{index}]</color></b> <color={DialogueColorTheme.ChoiceDefaultTextColor}>{approachName}</color> {dcTag}";

                ProblemActionEntry capturedEntry = entry;
                ActionDefinition capturedDef = actionDef;
                int capturedDc = targetDc;

                option.Initialize(
                    label,
                    () => ExecuteProblemAction(problem, capturedEntry, capturedDef, capturedDc),
                    isInteractable,
                    tooltipMsg,
                    ShowTooltip,
                    HideTooltip
                );
                index++;
            }

            Canvas.ForceUpdateCanvases();
            optionsScrollRect.verticalNormalizedPosition = 1f;
        }

        /// Executes selected problem action style challenge and advances dialogue to success or failure knot.
        private void ExecuteProblemAction(DialogueProblem problem, ProblemActionEntry entry, ActionDefinition actionDef, int targetDc){
            ClearOptions();
            HideTooltip();

            ActionStyle style = new(){
                actionType = entry.actionType,
                perkRequirement = actionDef.perkRequirement,
                targetDc = targetDc,
                applicableSkills = actionDef.applicableSkills,
                successQuipKeys = actionDef.successQuipKeys,
                failureQuipKeys = actionDef.failureQuipKeys
            };

            CharacterSheet actor = _characterSession != null && _characterSession.Character != null
                ? _characterSession.Character.sheet
                : null;

            SkillEvaluator.Evaluate(actor, style, result => {
                string transcript = SkillCheckTranscriptFormatter.Format(result);
                AddTranscriptEntry(transcript);

                DialogueOutcome outcome = result.passed ? problem.onSuccess : problem.onFailure;
                if (outcome == null)
                    return;

                outcome.onExecute?.Invoke();
                if (!string.IsNullOrEmpty(outcome.textKey)){
                    string text = _currentDialogue.GetLocalizedString(outcome.textKey);
                    string formatted = string.IsNullOrEmpty(outcome.speakerId)
                        ? text
                        : $"<b><color={DialogueColorTheme.SpeakerHeaderColor}>[{outcome.speakerId}]</color></b>\n{text}";

                    AddTranscriptEntry(formatted);
                }

                if (!string.IsNullOrEmpty(outcome.targetKnot))
                    GoToKnot(outcome.targetKnot);
            });
        }

        /// Instantiates and formats a transcript message for the current node.
        private void DisplayPrompt(DialogueNode node){
            string text = !string.IsNullOrEmpty(node.textKey)
                ? _currentDialogue.GetLocalizedString(node.textKey)
                : node.problem != null && ProblemArchetypeDatabase.Instance != null && ProblemArchetypeDatabase.Instance.Get(node.problem.archetypeId) != null
                    ? ProblemArchetypeDatabase.Instance.Get(node.problem.archetypeId).LocalizedDescription
                    : null;

            if (string.IsNullOrEmpty(text))
                return;

            string formatted = string.IsNullOrEmpty(node.speakerId)
                ? text
                : $"<b><color={DialogueColorTheme.SpeakerHeaderColor}>[{node.speakerId}]</color></b>\n{text}";

            AddTranscriptEntry(formatted);
        }

        /// Clears previous choices and populates active options with tactical crisis costs and availability.
        private void DisplayChoices(DialogueNode node){
            ClearOptions();
            HideTooltip();
            int index = 1;

            bool isCrisis = CrisisManager.Instance != null && CrisisManager.Instance.IsCrisis;
            CharacterSheet actor = _characterSession != null && _characterSession.Character != null
                ? _characterSession.Character.sheet
                : null;
            CrisisTurn turn = isCrisis && actor != null ? actor.CrisisTurn : null;

            foreach (DialogueChoice choice in node.choices){
                if (choice.isVisible != null && !choice.isVisible())
                    continue;

                bool lacksPerk = choice.perkRequirement != PerkRequirementMode.None
                                 && choice.requiredPerk != ActionType.None
                                 && (actor == null || !actor.HasPerk(choice.requiredPerk));

                if (lacksPerk && choice.perkRequirement == PerkRequirementMode.HiddenWhenLocked)
                    continue;

                DialogueOptionUI option = Instantiate(optionPrefab, optionsContent);
                string rawText = _currentDialogue.GetLocalizedString(choice.textKey);
                if (string.IsNullOrEmpty(rawText))
                    rawText = choice.textKey;

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
                    if ((choiceEndsTurn || choiceConsumesAction) && !actionAvailable)
                        canAfford = false;
                    if (choiceConsumesMove && !moveAvailable)
                        canAfford = false;

                    if (!canAfford){
                        isInteractable = false;
                        label = $"<b><color={DialogueColorTheme.DisabledChoiceColor}>[{index}]</color></b> <color={DialogueColorTheme.DisabledChoiceColor}>{cleanText}</color>";

                        if (choiceEndsTurn)
                            tooltipMsg = "Unavailable: Action already used (Ends Turn)";
                        else if (choiceConsumesAction)
                            tooltipMsg = "Unavailable: Action already used this turn";
                        else if (choiceConsumesMove)
                            tooltipMsg = "Unavailable: Movement already used this turn";
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
                            tooltipMsg = choiceConsumesMove
                                ? "Consumes Action & Move (Dash Disabled)"
                                : "Consumes Major Action";
                        }
                        else if (choiceConsumesMove){
                            costColor = DialogueColorTheme.MoveCostColor;
                            isResourceChoice = true;
                            tooltipMsg = "Consumes Movement (Dash / Double Move disabled)";
                        }

                        string textFormatted = isResourceChoice
                            ? $"<b><color={costColor}>{cleanText}</color></b>"
                            : $"<color={costColor}>{cleanText}</color>";

                        label = $"<b><color={DialogueColorTheme.ChoiceIndexColor}>[{index}]</color></b> {textFormatted}";
                    }
                }
                else{
                    isInteractable = true;
                    label = $"<b><color={DialogueColorTheme.ChoiceIndexColor}>[{index}]</color></b> <color={DialogueColorTheme.ChoiceDefaultTextColor}>{cleanText}</color>";
                    tooltipMsg = null;
                }

                DialogueChoice capturedChoice = choice;
                option.Initialize(
                    label,
                    () => SelectChoice(capturedChoice),
                    isInteractable,
                    tooltipMsg,
                    ShowTooltip,
                    HideTooltip
                );
                index++;
            }

            Canvas.ForceUpdateCanvases();
            optionsScrollRect.verticalNormalizedPosition = 1f;
        }

        /// Executes side effects for selected choice, consumes turn resources during crisis, and transitions to target knot.
        private void SelectChoice(DialogueChoice choice){
            if (choice.isOneShot)
                _currentDialogue.ConsumeChoice(choice.choiceId);

            if (CrisisManager.Instance != null && CrisisManager.Instance.IsCrisis && _characterSession != null && _characterSession.Character != null){
                CrisisTurn turn = _characterSession.Character.sheet.CrisisTurn;
                if (turn != null){
                    if (choice.endsTurn)
                        turn.EndTurn();
                    else if (choice.consumesAction)
                        turn.ConsumeAction();

                    if (choice.consumesMove && !choice.endsTurn)
                        turn.ConsumeMoveAndBurnSprint();
                }
            }

            choice.onSelect?.Invoke();
            GoToKnot(choice.targetKnot);
        }

        /// Extracts tactical gameplay tags (<!>, <!!>, <M>) from raw choice text.
        public static void ExtractTags(ref string text, ref bool consumesAction, ref bool endsTurn, ref bool consumesMove){
            if (string.IsNullOrEmpty(text))
                return;

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

        /// Displays hover tooltip near the cursor.
        public void ShowTooltip(Vector2 screenPosition, string text){
            if (string.IsNullOrEmpty(text))
                return;
            EnsureTooltipCreated();

            _tooltipText.text = text;
            _tooltipRoot.SetActive(true);
            _tooltipRoot.transform.SetAsLastSibling();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay){
                Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPosition, cam, out Vector2 localPos))
                    _tooltipRect.anchoredPosition = localPos + new Vector2(15f, 20f);
            }
            else
                _tooltipRect.position = screenPosition + new Vector2(15f, 20f);
        }

        /// Hides active hover tooltip.
        public void HideTooltip(){
            if (_tooltipRoot != null)
                _tooltipRoot.SetActive(false);
        }

        /// Creates dynamic tooltip GameObject hierarchy on the dialogue canvas.
        private void EnsureTooltipCreated(){
            if (_tooltipRoot != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parentTransform = canvas != null ? canvas.transform : transform;

            _tooltipRoot = new GameObject("DialogueChoiceTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            _tooltipRoot.transform.SetParent(parentTransform, false);
            _tooltipRect = _tooltipRoot.GetComponent<RectTransform>();
            _tooltipRect.pivot = new Vector2(0f, 0f);

            CanvasGroup group = _tooltipRoot.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Image bg = _tooltipRoot.GetComponent<Image>();
            bg.color = DialogueColorTheme.TooltipBackgroundColor;

            Outline outline = _tooltipRoot.AddComponent<Outline>();
            outline.effectColor = DialogueColorTheme.TooltipBorderColor;
            outline.effectDistance = new Vector2(1, -1);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(_tooltipRoot.transform, false);

            _tooltipText = textObj.GetComponent<TextMeshProUGUI>();
            _tooltipText.fontSize = 17;
            _tooltipText.alignment = TextAlignmentOptions.Center;
            _tooltipText.color = new Color(0.96f, 0.84f, 0.43f, 1f);

            HorizontalLayoutGroup layout = _tooltipRoot.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = _tooltipRoot.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _tooltipRoot.SetActive(false);
        }

        /// Removes all existing option GameObjects from the options container.
        private void ClearOptions(){
            HideTooltip();
            foreach (Transform child in optionsContent)
                Destroy(child.gameObject);
        }

        /// Removes all existing transcript entries from the transcript container.
        private void ClearTranscript(){
            foreach (Transform child in transcriptContent)
                Destroy(child.gameObject);
        }
    }
}
