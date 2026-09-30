using UnityEngine;

namespace Core.Managers{
    /// Manages application target frame rate throttling.
    public class PerformanceManager : MonoBehaviour{
        [Header("Frame Rate")]
        [Tooltip("Target frame rate (e.g. 24, 30, 60). Set to -1 for uncapped.")]
        [SerializeField] private int targetFrameRate = 24;

        [Tooltip("VSync count. Must be 0 for targetFrameRate to take effect.")]
        [SerializeField] private int vSyncCount = 0;

        /// Applies frame rate throttling on startup.
        private void Awake() => ApplySettings();

        /// Updates application VSync count and target frame rate.
        public void ApplySettings(){
            QualitySettings.vSyncCount = vSyncCount;
            Application.targetFrameRate = targetFrameRate;
        }

        /// Live-updates target frame rate during editor play testing.
        private void OnValidate(){
            if (Application.isPlaying){
                ApplySettings();
            }
        }
    }
}
