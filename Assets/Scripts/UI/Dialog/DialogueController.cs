using System;
using System.Text.RegularExpressions;
using Core.Crisis;
using Narrative.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        public GameObject DialogRoot => dialogRoot;
        public bool       IsOpen     => gameObject.activeSelf && (dialogRoot == null || dialogRoot.activeInHierarchy);

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

            _characterSession.SetKnot(_currentDialogue, knotId);

            _currentNode = _currentDialogue.GetNode(knotId);
            _currentNode.onEnter?.Invoke();

            DisplayPrompt(_currentNode);
            DisplayChoices(_currentNode);
        }

        /// Instantiates and formats a transcript message for the current node.
        private void DisplayPrompt(DialogueNode node){
            if (string.IsNullOrEmpty(node.textKey))
                return;

            string text = _currentDialogue.GetLocalizedString(node.textKey);
            if (string.IsNullOrEmpty(text))
                text = node.textKey;

            string formatted = string.IsNullOrEmpty(node.speakerId)
                ? text
                : $"<b><color=#143447>[{node.speakerId}]</color></b>\n{text}";

            GameObject entry = Instantiate(transcriptPrefab, transcriptContent);
            entry.GetComponent<TextMeshProUGUI>().text = formatted;

            Canvas.ForceUpdateCanvases();
            transcriptScrollRect.verticalNormalizedPosition = 0f;
        }

        /// Clears previous choices and populates active options with tactical crisis costs and availability.
        private void DisplayChoices(DialogueNode node){
            ClearOptions();
            HideTooltip();
            int index = 1;

            bool isCrisis = CrisisManager.Instance != null && CrisisManager.Instance.IsCrisis;
            CrisisTurn turn = isCrisis ? _characterSession.Character.sheet.CrisisTurn : null;

            foreach (DialogueChoice choice in node.choices){
                if (choice.isVisible != null && !choice.isVisible())
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
                string tooltipText = null;

                if (isCrisis && turn != null){
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
                            tooltipText = "Unavailable: Action already used (Ends Turn)";
                        else if (choiceConsumesAction)
                            tooltipText = "Unavailable: Action already used this turn";
                        else if (choiceConsumesMove)
                            tooltipText = "Unavailable: Movement already used this turn";
                    }
                    else{
                        isInteractable = true;
                        string costColor = DialogueColorTheme.ChoiceDefaultTextColor;
                        bool isResourceChoice = false;

                        if (choiceEndsTurn){
                            costColor = DialogueColorTheme.EndTurnCostColor;
                            isResourceChoice = true;
                            tooltipText = "Ends Turn (Consumes Action, Move, and Dash)";
                        }
                        else if (choiceConsumesAction){
                            costColor = DialogueColorTheme.ActionCostColor;
                            isResourceChoice = true;
                            tooltipText = choiceConsumesMove
                                ? "Consumes Action & Move (Dash Disabled)"
                                : "Consumes Major Action";
                        }
                        else if (choiceConsumesMove){
                            costColor = DialogueColorTheme.MoveCostColor;
                            isResourceChoice = true;
                            tooltipText = "Consumes Movement (Dash / Double Move disabled)";
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
                    tooltipText = null;
                }

                DialogueChoice capturedChoice = choice;
                option.Initialize(
                    label,
                    () => SelectChoice(capturedChoice),
                    isInteractable,
                    tooltipText,
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

            if (CrisisManager.Instance != null && CrisisManager.Instance.IsCrisis){
                CrisisTurn turn = _characterSession.Character.sheet.CrisisTurn;
                if (choice.endsTurn)
                    turn.EndTurn();
                else if (choice.consumesAction)
                    turn.ConsumeAction();

                if (choice.consumesMove && !choice.endsTurn)
                    turn.ConsumeMoveAndBurnSprint();
            }

            choice.onSelect?.Invoke();
            GoToKnot(choice.targetKnot);
        }

        /// Strips <!>, <!!>, <M> tags from dialogue choice text and updates corresponding tactical cost flags.
        public static void ExtractTags(ref string text, ref bool consumesAction, ref bool endsTurn, ref bool consumesMove){
            if (string.IsNullOrEmpty(text))
                return;

            if (text.Contains("<!!>")){
                endsTurn = true;
                text = text.Replace("<!!>", "");
            }
            else if (text.Contains("<!>")){
                consumesAction = true;
                text = text.Replace("<!>", "");
            }

            if (Regex.IsMatch(text, @"<[mM]>")){
                consumesMove = true;
                text = Regex.Replace(text, @"<[mM]>", "");
            }

            text = text.Trim();
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
        public void ClearTranscript(){
            foreach (Transform child in transcriptContent)
                Destroy(child.gameObject);
        }
    }
}
