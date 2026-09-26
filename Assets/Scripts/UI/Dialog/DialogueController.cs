using System;
using Core.Crisis;
using Data;
using Narrative.Dialog;
using Narrative.Skills;
using UI.Shared.Transitions;
using UnityEngine;
using World.Actors.Player;

namespace UI.Dialog{
    /// Orchestrates dialogue graph navigation, screen presentation, and challenge evaluation.
    [DisallowMultipleComponent]
    public class DialogueController : MonoBehaviour{
        public static event Action<bool> OnDialogueActiveChanged;

        [Header("Containers")]
        [SerializeField] private GameObject dialogRoot;

        [Header("Views")]
        [SerializeField] private DialogueTranscriptView transcriptView;
        [SerializeField] private DialogueOptionPresenter optionPresenter;

        [Header("Transition")]
        [SerializeField] private CanvasTransitionController transition;

        private DialogueBehaviour        _currentDialogue;
        private DialogueNode             _currentNode;
        private CharacterDialogueSession _characterSession;

        public static DialogueController Instance{ get; private set; }

        public GameObject DialogRoot => dialogRoot;
        public bool       IsOpen     => (transition ? transition.IsVisible : gameObject.activeSelf) && (!dialogRoot || dialogRoot.activeInHierarchy);

        /// Assigns singleton instance and discovers transition component if missing.
        private void Awake(){
            Instance = this;
            if (!transition) transition = GetComponentInParent<CanvasTransitionController>();
            if (!transcriptView) transcriptView = GetComponentInChildren<DialogueTranscriptView>();
            if (!optionPresenter) optionPresenter = GetComponentInChildren<DialogueOptionPresenter>();
        }

        /// Shows or hides dialogue screen with optional transition.
        public void SetVisible(bool visible, bool immediate = false){
            if (transition){
                int heroIndex = _characterSession != null ? _characterSession.ScreenIndex : 0;
                if (immediate) transition.SetInstant(visible, heroIndex);
                else if (visible) transition.ShowHero(heroIndex);
                else transition.Hide();
                return;
            }

            gameObject.SetActive(visible);
            if (!dialogRoot || dialogRoot == gameObject) return;
            if (visible) dialogRoot.SetActive(true);
            else if (dialogRoot.GetComponentsInChildren<DialogueController>(false).Length == 0) dialogRoot.SetActive(false);
        }

        /// Starts dialogue session on this screen and navigates to the entry knot.
        public void BeginDialogue(DialogueBehaviour dialogue, string startingKnot, CharacterDialogueSession session){
            _currentDialogue  = dialogue;
            _characterSession = session;

            SetVisible(true);
            OnDialogueActiveChanged?.Invoke(true);

            transcriptView.Clear();
            optionPresenter.Clear();

            GoToKnot(startingKnot);
        }

        /// Closes dialogue session, clears views, and notifies listeners.
        public void EndDialogue(){
            optionPresenter.Clear();
            transcriptView.Clear();

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

            if (_characterSession != null) _characterSession.SetKnot(_currentDialogue, knotId);

            _currentNode = _currentDialogue.GetNode(knotId);
            _currentNode.onEnter?.Invoke();

            optionPresenter.Clear();
            DisplayPrompt(_currentNode, () => {
                CharacterSheet actor = _characterSession != null && _characterSession.Character ? _characterSession.Character.sheet : null;

                if (_currentNode.problem != null){
                    ProblemArchetype archetype = ProblemArchetypeDatabase.Instance.Get(_currentNode.problem.archetypeId);
                    optionPresenter.PresentProblemOptions(archetype, _currentNode.problem, actor, (entry, def, dc) => ExecuteProblemAction(_currentNode.problem, entry, def, dc));
                }
                else if (_currentNode.skillCheck != null) ExecuteSkillCheck(_currentNode.skillCheck);
                else optionPresenter.PresentChoices(_currentNode, _currentDialogue, actor, SelectChoice);
            });
        }

        /// Appends an arbitrary raw entry directly into the transcript log.
        public void AddTranscriptEntry(string formattedText, Action onComplete = null) =>
            transcriptView.AppendRaw(formattedText, onComplete);

        /// Evaluates knot skill challenge and displays transcript before advancing dialogue.
        private void ExecuteSkillCheck(DialogueSkillCheck skillCheck){
            optionPresenter.Clear();
            ActionStyle style = skillCheck.GetEffectiveActionStyle();
            CharacterSheet actor = _characterSession != null && _characterSession.Character ? _characterSession.Character.sheet : null;

            if (!style.CanAttempt(actor)){
                transcriptView.AppendRaw($"<b><color={DialogueColorTheme.CheckFailedColor}>[CANNOT ATTEMPT: Requires {style.actionType} Perk]</color></b>");
                DialogueOutcome failOutcome = skillCheck.onFailure;
                if (failOutcome != null){
                    failOutcome.onExecute?.Invoke();
                    if (!string.IsNullOrEmpty(failOutcome.textKey)){
                        string text = _currentDialogue.GetLocalizedString(failOutcome.textKey);
                        if (string.IsNullOrEmpty(text)) text = failOutcome.textKey;
                        transcriptView.AppendEntry(failOutcome.speakerId, text);
                    }
                    if (!string.IsNullOrEmpty(failOutcome.targetKnot)) GoToKnot(failOutcome.targetKnot);
                }
                return;
            }

            SkillEvaluator.Evaluate(actor, style, result => {
                string transcript = SkillCheckTranscriptFormatter.Format(result);
                transcriptView.AppendRaw(transcript, () => {
                    DialogueOutcome outcome = result.passed ? skillCheck.onSuccess : skillCheck.onFailure;
                    if (outcome == null) return;

                    outcome.onExecute?.Invoke();
                    if (!string.IsNullOrEmpty(outcome.textKey)){
                        string text = _currentDialogue.GetLocalizedString(outcome.textKey);
                        if (string.IsNullOrEmpty(text)) text = outcome.textKey;
                        transcriptView.AppendEntry(outcome.speakerId, text, () => {
                            if (!string.IsNullOrEmpty(outcome.targetKnot)) GoToKnot(outcome.targetKnot);
                        });
                    }
                    else if (!string.IsNullOrEmpty(outcome.targetKnot)) GoToKnot(outcome.targetKnot);
                });
            });
        }

        /// Executes selected problem action style challenge and advances dialogue to success or failure knot.
        private void ExecuteProblemAction(DialogueProblem problem, ProblemActionEntry entry, ActionDefinition actionDef, int targetDc){
            optionPresenter.Clear();
            ActionStyle style = new(){
                actionType = entry.actionType,
                perkRequirement = actionDef.perkRequirement,
                targetDc = targetDc,
                applicableSkills = actionDef.applicableSkills,
                successQuipKeys = actionDef.successQuipKeys,
                failureQuipKeys = actionDef.failureQuipKeys
            };

            CharacterSheet actor = _characterSession != null && _characterSession.Character ? _characterSession.Character.sheet : null;

            SkillEvaluator.Evaluate(actor, style, result => {
                string transcript = SkillCheckTranscriptFormatter.Format(result);
                transcriptView.AppendRaw(transcript, () => {
                    DialogueOutcome outcome = result.passed ? problem.onSuccess : problem.onFailure;
                    if (outcome == null) return;

                    outcome.onExecute?.Invoke();
                    if (!string.IsNullOrEmpty(outcome.textKey)){
                        string text = _currentDialogue.GetLocalizedString(outcome.textKey);
                        transcriptView.AppendEntry(outcome.speakerId, text, () => {
                            if (!string.IsNullOrEmpty(outcome.targetKnot)) GoToKnot(outcome.targetKnot);
                        });
                    }
                    else if (!string.IsNullOrEmpty(outcome.targetKnot)) GoToKnot(outcome.targetKnot);
                });
            });
        }

        /// Instantiates prompt transcript message for the current node.
        private void DisplayPrompt(DialogueNode node, Action onPromptComplete){
            string text = !string.IsNullOrEmpty(node.textKey)
                ? _currentDialogue.GetLocalizedString(node.textKey)
                : node.problem != null && ProblemArchetypeDatabase.Instance != null && ProblemArchetypeDatabase.Instance.Get(node.problem.archetypeId) != null
                    ? ProblemArchetypeDatabase.Instance.Get(node.problem.archetypeId).LocalizedDescription
                    : null;

            if (string.IsNullOrEmpty(text)){
                onPromptComplete?.Invoke();
                return;
            }

            transcriptView.AppendEntry(node.speakerId, text, onPromptComplete);
        }

        /// Executes side effects for selected choice, consumes turn resources during crisis, and transitions to target knot.
        private void SelectChoice(DialogueChoice choice){
            if (choice.isOneShot) _currentDialogue.ConsumeChoice(choice.choiceId);

            if (CrisisManager.Instance && CrisisManager.Instance.IsCrisis && _characterSession != null && _characterSession.Character){
                CrisisTurn turn = _characterSession.Character.sheet.CrisisTurn;
                if (turn != null){
                    if (choice.endsTurn) turn.EndTurn();
                    else if (choice.consumesAction) turn.ConsumeAction();
                    if (choice.consumesMove && !choice.endsTurn) turn.ConsumeMoveAndBurnSprint();
                }
            }

            choice.onSelect?.Invoke();
            GoToKnot(choice.targetKnot);
        }
    }
}
