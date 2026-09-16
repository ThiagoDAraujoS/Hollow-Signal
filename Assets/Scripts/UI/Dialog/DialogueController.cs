using Narrative.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog{
    /// Controls dialogue window presentation, transcript logging, and choice selection.
    [DisallowMultipleComponent]
    public class DialogueController : MonoBehaviour{
        public static DialogueController Instance{ get; private set; }

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

        private DialogueBehaviour _currentDialogue;
        private DialogueNode _currentNode;

        private void Awake() => Instance = this;

        private void OnDestroy() => Instance = Instance == this ? null : Instance;

        /// Initiates dialogue playback from the specified dialogue state machine and starting knot.
        public static void StartDialogue(DialogueBehaviour dialogue, string startingKnot = "Main") => Instance.BeginDialogue(dialogue, startingKnot);

        /// Closes the active dialogue window and clears pending options.
        public static void CloseDialogue() => Instance.EndDialogue();

        /// Starts dialogue session and navigates to the entry knot.
        public void BeginDialogue(DialogueBehaviour dialogue, string startingKnot){
            _currentDialogue = dialogue;
            dialogRoot.SetActive(true);
            ClearTranscript();
            GoToKnot(startingKnot);
        }

        /// Closes dialogue session and clears active options.
        public void EndDialogue(){
            ClearOptions();
            dialogRoot.SetActive(false);
            _currentDialogue = null;
            _currentNode = null;
        }

        /// Navigates to a specific knot in the active dialogue graph.
        public void GoToKnot(string knotId){
            if (knotId == DialogueNode.End){
                EndDialogue();
                return;
            }

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

        /// Clears previous choices and populates active options for the current node.
        private void DisplayChoices(DialogueNode node){
            ClearOptions();
            int index = 1;
            foreach (DialogueChoice choice in node.choices){
                if (choice.isVisible != null && !choice.isVisible())
                    continue;

                DialogueOptionUI option = Instantiate(optionPrefab, optionsContent);
                string choiceText = _currentDialogue.GetLocalizedString(choice.textKey);
                if (string.IsNullOrEmpty(choiceText))
                    choiceText = choice.textKey;

                string label = $"<b><color=#854205>[{index}]</color></b> <color=#1C1814>{choiceText}</color>";
                DialogueChoice capturedChoice = choice;
                option.Initialize(label, () => SelectChoice(capturedChoice));
                index++;
            }

            Canvas.ForceUpdateCanvases();
            optionsScrollRect.verticalNormalizedPosition = 1f;
        }

        /// Executes side effects for selected choice and transitions to the target knot.
        private void SelectChoice(DialogueChoice choice){
            if (choice.isOneShot)
                _currentDialogue.ConsumeChoice(choice.choiceId);

            choice.onSelect?.Invoke();
            GoToKnot(choice.targetKnot);
        }

        /// Removes all existing option GameObjects from the options container.
        private void ClearOptions(){
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
