using System.Threading.Tasks;
using UnityEngine;

namespace Core.UI {
    /// Controls full-screen transition fades and raycast blocking during scene loading.
    /// Drives a CanvasGroup over unscaled time so fades remain smooth even during paused time scales.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingScreenCurtain : MonoBehaviour {
        public static LoadingScreenCurtain Instance { get; private set; }

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.35f;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDestroy() {
            if (Instance == this)
                Instance = null;
        }

        /// Fades the curtain alpha up to 1.0 to completely cover the screen.
        public async Task FadeInAsync() {
            canvasGroup.blocksRaycasts = true;
            await AnimateAlphaAsync(1.0f);
        }

        /// Fades the curtain alpha down to 0.0 to reveal the underlying scene.
        public async Task FadeOutAsync() {
            await AnimateAlphaAsync(0.0f);
            canvasGroup.blocksRaycasts = false;
        }

        /// Immediately snaps the curtain to a target alpha without transitioning.
        public void SnapAlpha(float targetAlpha) {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha > 0f;
        }

        private async Task AnimateAlphaAsync(float targetAlpha) {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            if (fadeDuration <= 0f) {
                canvasGroup.alpha = targetAlpha;
                return;
            }

            while (elapsed < fadeDuration) {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}
