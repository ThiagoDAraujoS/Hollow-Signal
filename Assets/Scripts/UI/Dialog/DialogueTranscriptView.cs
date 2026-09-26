using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog{
    /// Manages transcript entry instantiation, speaker styling, autoscroll, and typewriter integration.
    [DisallowMultipleComponent]
    public class DialogueTranscriptView : MonoBehaviour{
        [SerializeField] private RectTransform transcriptContent;
        [SerializeField] private ScrollRect transcriptScrollRect;
        [SerializeField] private GameObject transcriptPrefab;
        [SerializeField] private DialogueTypewriter typewriter;
        [SerializeField] private float charactersPerSecond = 45f;

        /// Discovers scroll rect, content rect, and typewriter on awake if unassigned.
        private void Awake(){
            if (!transcriptScrollRect) transcriptScrollRect = GetComponent<ScrollRect>() ?? GetComponentInChildren<ScrollRect>();
            if (!transcriptContent && transcriptScrollRect) transcriptContent = transcriptScrollRect.content;
            if (!typewriter) typewriter = GetComponentInParent<DialogueTypewriter>() ?? GetComponentInChildren<DialogueTypewriter>() ?? gameObject.AddComponent<DialogueTypewriter>();
        }

        /// Configures runtime references directly.
        public void Configure(RectTransform content, ScrollRect scrollRect, GameObject prefab, DialogueTypewriter tw, float speed = 45f){
            transcriptContent = content;
            transcriptScrollRect = scrollRect;
            transcriptPrefab = prefab;
            typewriter = tw;
            charactersPerSecond = speed;
        }

        /// Appends a speaker-formatted dialogue line into the transcript.
        public void AppendEntry(string speakerId, string text, Action onComplete = null){
            string formatted = string.IsNullOrEmpty(speakerId) ? text : $"<b><color={DialogueColorTheme.SpeakerHeaderColor}>[{speakerId}]</color></b>\n{text}";
            AppendRaw(formatted, onComplete);
        }

        /// Appends an arbitrary pre-formatted rich-text line into the transcript.
        public void AppendRaw(string formattedText, Action onComplete = null){
            GameObject entry = Instantiate(transcriptPrefab, transcriptContent);
            TextMeshProUGUI tmp = entry.GetComponent<TextMeshProUGUI>();
            tmp.text = formattedText;
            Canvas.ForceUpdateCanvases();
            ScrollToBottom();

            if (charactersPerSecond <= 0f || !typewriter){
                onComplete?.Invoke();
                return;
            }

            typewriter.Play(tmp, charactersPerSecond, () => {
                ScrollToBottom();
                onComplete?.Invoke();
            });
        }

        /// Forces transcript scroll rect to bottom position.
        public void ScrollToBottom() => transcriptScrollRect.verticalNormalizedPosition = 0f;

        /// Destroys all existing transcript entries.
        public void Clear(){
            if (typewriter) typewriter.Stop();
            foreach (Transform child in transcriptContent) Destroy(child.gameObject);
        }
    }
}
