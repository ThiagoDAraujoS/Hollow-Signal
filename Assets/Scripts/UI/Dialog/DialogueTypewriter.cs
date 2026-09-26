using System;
using System.Collections;
using TMPro;
using UnityEngine;
using World.Actors.Brains;

namespace UI.Dialog{
    /// Controls tag-safe character reveal animation and skip handling for dialogue text.
    [DisallowMultipleComponent]
    public class DialogueTypewriter : MonoBehaviour{
        private Coroutine       _typewriterRoutine;
        private TextMeshProUGUI _activeTmp;
        private Action          _onComplete;

        public bool IsTyping => _typewriterRoutine != null;

        /// Subscribes to global pointer input to trigger skip.
        private void OnEnable() => PlayerGestureController.OnPointerPressed += Skip;

        /// Unsubscribes pointer input and stops active typing routine on disable.
        private void OnDisable(){
            PlayerGestureController.OnPointerPressed -= Skip;
            Stop();
        }

        /// Starts typewriter animation on target TextMeshProUGUI.
        public void Play(TextMeshProUGUI tmp, float charsPerSecond, Action onComplete = null){
            Stop();
            _activeTmp = tmp;
            _onComplete = onComplete;
            _typewriterRoutine = StartCoroutine(TypeRoutine(tmp, charsPerSecond));
        }

        /// Instantly skips typewriter animation to reveal all characters.
        public void Skip(){
            if (_typewriterRoutine == null) return;
            StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = null;
            if (_activeTmp) _activeTmp.maxVisibleCharacters = int.MaxValue;
            _activeTmp = null;
            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        /// Halts active typewriter animation without invoking complete callback.
        public void Stop(){
            if (_typewriterRoutine != null){
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }
            _activeTmp = null;
            _onComplete = null;
        }

        /// Coroutine driving progressive character visibility.
        private IEnumerator TypeRoutine(TextMeshProUGUI tmp, float charsPerSecond){
            tmp.ForceMeshUpdate();
            int total = tmp.textInfo.characterCount;
            tmp.maxVisibleCharacters = 0;
            float delay = 1f / Mathf.Max(1f, charsPerSecond);

            for (int i = 0; i <= total; i++){
                tmp.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(delay);
            }

            tmp.maxVisibleCharacters = int.MaxValue;
            _typewriterRoutine = null;
            _activeTmp = null;
            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }
    }
}
