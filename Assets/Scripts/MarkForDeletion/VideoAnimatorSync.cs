using UnityEngine;
using UnityEngine.Video;

namespace UI.BootMenu{
    /// Slaves an Animator directly to a VideoPlayer's playback clock.
    /// Prevents startup desynchronization caused by video buffering and loading frame drops.
    [DisallowMultipleComponent]
    public class VideoAnimatorSync : MonoBehaviour{
        [Header("Components")]
        [Tooltip("The VideoPlayer playing the background video.")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("The Animator driving the rig/bones.")]
        [SerializeField] private Animator animator;

        [Header("Animation State")]
        [Tooltip("Name of the state in the Animator to keep in sync.")]
        [SerializeField] private string stateName = "Swing";

        [Tooltip("Animator layer index.")]
        [SerializeField] private int layerIndex;

        [Header("Sync Settings")]
        [Tooltip("Offset added to the video's normalized time (0.0 to 1.0) to compensate for loop cuts.")]
        [Range(0f, 1f)]
        [SerializeField] private float cycleOffset = 0.5f;

        [Tooltip("If true, the Animator is hard-locked to the VideoPlayer's exact frame every update. Highly recommended.")]
        [SerializeField] private bool hardLock = true;

        private int  _stateHash;
        private bool _isInitialized;

        private void Awake(){
            if (!animator)
                animator = GetComponent<Animator>();

            if (!videoPlayer)
                videoPlayer = FindAnyObjectByType<VideoPlayer>();

            _stateHash     = Animator.StringToHash(stateName);
            animator.speed = 0f;
        }

        private void Start(){
            if (!videoPlayer.isPrepared){
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare();
            }
            else
                OnVideoPrepared(videoPlayer);
        }

        private void OnDestroy() => videoPlayer.prepareCompleted -= OnVideoPrepared;

        private void OnVideoPrepared(VideoPlayer source){
            _isInitialized = true;

            if (!source.isPlaying && source.playOnAwake)
                source.Play();

            SyncNow();
        }

        private void Update(){
            if (!_isInitialized || !videoPlayer.isPlaying) return;

            if (hardLock){
                SyncNow();
                return;
            }

            // Soft sync: keep animator speed matching playbackSpeed, but resync if drifted > 50ms
            animator.speed = videoPlayer.playbackSpeed;

            double videoDuration = videoPlayer.length;
            if (videoDuration <= 0.0) return;

            float targetNormalized  = Mathf.Repeat((float)(videoPlayer.time / videoDuration) + cycleOffset, 1.0f);
            AnimatorStateInfo   stateInfo         = animator.GetCurrentAnimatorStateInfo(layerIndex);
            float currentNormalized = Mathf.Repeat(stateInfo.normalizedTime, 1.0f);

            float diff = Mathf.Abs(targetNormalized - currentNormalized);
            if (diff > 0.5f)
                diff = 1.0f - diff;

            if (diff > 0.05f)
                animator.Play(_stateHash, layerIndex, targetNormalized);
        }

        /// Instantly forces the Animator to evaluate the exact frame matching the VideoPlayer.
        public void SyncNow(){
            double duration = videoPlayer.length;
            if (duration <= 0.0) return;

            float normalizedVideoTime = (float)(videoPlayer.time / duration);
            float targetTime          = Mathf.Repeat(normalizedVideoTime + cycleOffset, 1.0f);

            animator.Play(_stateHash, layerIndex, targetTime);
            animator.Update(0f);
        }

#if UNITY_EDITOR
        private void OnValidate(){
            _stateHash = Animator.StringToHash(stateName);
            if (Application.isPlaying && _isInitialized)
                SyncNow();
        }
#endif
    }
}
