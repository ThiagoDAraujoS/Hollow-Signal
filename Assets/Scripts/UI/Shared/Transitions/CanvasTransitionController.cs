using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared.Transitions{
    /// Coordinates shared canvas reveal and dismissal with a parent text CanvasGroup and background transition shader.
    [DisallowMultipleComponent]
    public class CanvasTransitionController : MonoBehaviour{
        private static readonly int PROP_TRANSITION_PROGRESS = Shader.PropertyToID("_TransitionProgress");

        [Header("Background Transition (Shader)")]
        [SerializeField] private Graphic backgroundGraphic;
        [SerializeField] private Renderer backgroundRenderer;
        [SerializeField] private float backgroundDuration = 0.3f;
        [SerializeField] private AnimationCurve backgroundCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Text Canvas Transition (Alpha Fade)")]
        [SerializeField] private CanvasGroup textCanvasGroup;
        [SerializeField] private float textFadeDuration = 0.15f;
        [SerializeField] private AnimationCurve textFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Hero Panels")]
        [SerializeField] private GameObject[] heroPanels = new GameObject[4];

        [Header("Roots & State")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private bool playOnAwake;
        [SerializeField] private bool startHidden = true;
        [SerializeField] private bool useUnscaledTime = true;

        private Material _instantiatedMaterial;
        private MaterialPropertyBlock _mpb;
        private Coroutine _transitionRoutine;
        private int _activeHeroIndex = -1;
        private float _currentProgress;
        private bool _isBackgroundVisible;

        public bool IsVisible => _isBackgroundVisible;
        public int ActiveHeroIndex => _activeHeroIndex;
        public bool IsTransitioning => _transitionRoutine != null;

        public event Action<bool> OnTransitionStarted;
        public event Action<bool> OnTransitionFinished;

        /// Initializes materials, disables background raycasting, and sets initial visual state on awake.
        private void Awake(){
            if (!rootObject) rootObject = gameObject;
            if (backgroundGraphic){
                backgroundGraphic.raycastTarget = false;
                if (Application.isPlaying){
                    _instantiatedMaterial = new Material(backgroundGraphic.material);
                    backgroundGraphic.material = _instantiatedMaterial;
                }
            }
            if (backgroundRenderer) _mpb = new MaterialPropertyBlock();

            if (startHidden){
                _isBackgroundVisible = false;
                _currentProgress = 0f;
                SetBackgroundProgress(0f);
                if (textCanvasGroup){
                    textCanvasGroup.alpha = 0f;
                    textCanvasGroup.blocksRaycasts = false;
                    textCanvasGroup.interactable = false;
                }
                SelectHeroPanel(-1);
            }
            else if (playOnAwake) ShowHero(0);
        }

        /// Cleans up created material instances.
        private void OnDestroy() => Destroy(_instantiatedMaterial);

        /// Reveals background shader and fades in the parent text canvas for the designated hero slot.
        public void ShowHero(int heroIndex, Action onComplete = null){
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (rootObject && !rootObject.activeSelf) rootObject.SetActive(true);
            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            SelectHeroPanel(heroIndex);
            if (textCanvasGroup){
                textCanvasGroup.blocksRaycasts = true;
                textCanvasGroup.interactable = true;
            }
            _transitionRoutine = StartCoroutine(ShowRoutine(onComplete));
        }

        /// Fades out parent text canvas and closes background transition shader.
        public void Hide(Action onComplete = null){
            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            if (!gameObject.activeInHierarchy) return;
            _transitionRoutine = StartCoroutine(HideRoutine(onComplete));
        }

        /// Immediately sets visibility and active hero slot without animating.
        public void SetInstant(bool visible, int heroIndex = 0){
            if (!gameObject.activeSelf && visible) gameObject.SetActive(true);
            if (_transitionRoutine != null){
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }
            _isBackgroundVisible = visible;
            _currentProgress = visible ? 1f : 0f;
            SetBackgroundProgress(_currentProgress);

            if (textCanvasGroup){
                textCanvasGroup.alpha = visible ? 1f : 0f;
                textCanvasGroup.blocksRaycasts = visible;
                textCanvasGroup.interactable = visible;
            }

            SelectHeroPanel(visible ? heroIndex : -1);
            if (rootObject && rootObject.activeSelf != visible) rootObject.SetActive(visible);
        }

        /// Activates the designated hero panel and disables all others.
        public void SelectHeroPanel(int heroIndex){
            _activeHeroIndex = heroIndex;
            for (int i = 0; i < heroPanels.Length; i++){
                if (!heroPanels[i]) continue;
                bool isTarget = i == heroIndex;
                if (heroPanels[i].activeSelf != isTarget) heroPanels[i].SetActive(isTarget);
            }
        }

        /// Coroutine driving show sequence (background shader in -> text canvas alpha in).
        private IEnumerator ShowRoutine(Action onComplete){
            OnTransitionStarted?.Invoke(true);

            float startProgress = _currentProgress;
            if (startProgress < 1f){
                float duration = Mathf.Max(0.01f, backgroundDuration * (1f - startProgress));
                float elapsed = 0f;
                while (elapsed < duration){
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _currentProgress = Mathf.Lerp(startProgress, 1f, backgroundCurve.Evaluate(t));
                    SetBackgroundProgress(_currentProgress);
                    yield return null;
                }
                _currentProgress = 1f;
                SetBackgroundProgress(1f);
            }
            _isBackgroundVisible = true;

            if (textCanvasGroup && textCanvasGroup.alpha < 1f){
                float startAlpha = textCanvasGroup.alpha;
                float duration = Mathf.Max(0.01f, textFadeDuration * (1f - startAlpha));
                float elapsed = 0f;
                while (elapsed < duration){
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    textCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, textFadeCurve.Evaluate(t));
                    yield return null;
                }
                textCanvasGroup.alpha = 1f;
            }

            if (textCanvasGroup){
                textCanvasGroup.blocksRaycasts = true;
                textCanvasGroup.interactable = true;
            }

            _transitionRoutine = null;
            onComplete?.Invoke();
            OnTransitionFinished?.Invoke(true);
        }

        /// Coroutine driving hide sequence (text canvas alpha out -> background shader out).
        private IEnumerator HideRoutine(Action onComplete){
            OnTransitionStarted?.Invoke(false);

            if (textCanvasGroup && textCanvasGroup.alpha > 0f){
                textCanvasGroup.blocksRaycasts = false;
                textCanvasGroup.interactable = false;
                float startAlpha = textCanvasGroup.alpha;
                float duration = Mathf.Max(0.01f, textFadeDuration * startAlpha);
                float elapsed = 0f;
                while (elapsed < duration){
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    textCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, textFadeCurve.Evaluate(t));
                    yield return null;
                }
                textCanvasGroup.alpha = 0f;
            }

            SelectHeroPanel(-1);

            float startProgress = _currentProgress;
            if (startProgress > 0f){
                float duration = Mathf.Max(0.01f, backgroundDuration * startProgress);
                float elapsed = 0f;
                while (elapsed < duration){
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _currentProgress = Mathf.Lerp(startProgress, 0f, backgroundCurve.Evaluate(t));
                    SetBackgroundProgress(_currentProgress);
                    yield return null;
                }
                _currentProgress = 0f;
                SetBackgroundProgress(0f);
            }
            _isBackgroundVisible = false;

            if (rootObject && rootObject.activeSelf) rootObject.SetActive(false);

            _transitionRoutine = null;
            onComplete?.Invoke();
            OnTransitionFinished?.Invoke(false);
        }

        /// Updates shader transition progress on graphic material or renderer property block.
        private void SetBackgroundProgress(float progress){
            if (_instantiatedMaterial) _instantiatedMaterial.SetFloat(PROP_TRANSITION_PROGRESS, progress);
            else if (backgroundGraphic && backgroundGraphic.material) backgroundGraphic.material.SetFloat(PROP_TRANSITION_PROGRESS, progress);

            if (!backgroundRenderer) return;
            backgroundRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(PROP_TRANSITION_PROGRESS, progress);
            backgroundRenderer.SetPropertyBlock(_mpb);
        }
    }
}
