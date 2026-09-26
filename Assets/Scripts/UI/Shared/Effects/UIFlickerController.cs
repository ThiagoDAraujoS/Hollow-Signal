using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared.Effects{
    /// Drives the UI/FlickerMask shader using an AnimationCurve to add HDR shine.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIFlickerController : MonoBehaviour{
        [Header("Target Graphic")] [SerializeField]
        private Graphic targetGraphic;

        [Header("Curve Control")] [Tooltip("Curve defining the flicker shine over normalized time (0 to 1).")] [SerializeField]
        private AnimationCurve flickerCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.0f),
            new Keyframe(0.10f, 0.8f),
            new Keyframe(0.15f, 0.1f),
            new Keyframe(0.25f, 1.0f),
            new Keyframe(0.35f, 0.2f),
            new Keyframe(0.50f, 0.9f),
            new Keyframe(0.60f, 0.0f),
            new Keyframe(0.85f, 0.4f),
            new Keyframe(1.00f, 0.0f)
        );

        [Tooltip("Duration in seconds for one full cycle of the curve.")] [Range(0.05f, 10f)] [SerializeField]
        private float cycleDuration = 1.0f;

        [Tooltip("Multiplier applied to the curve value. Higher numbers push into HDR/Bloom values.")] [Range(0f, 15f)] [SerializeField]
        private float intensityMultiplier = 3.5f;

        [Header("Playback")] [SerializeField] private bool loop            = true;
        [SerializeField]                      private bool playOnAwake     = true;
        [SerializeField]                      private bool useUnscaledTime = true;

        [Tooltip("Offset the start time randomly so multiple buttons don't flicker identically.")] [SerializeField]
        private bool randomStartOffset = true;

        [Header("Editor Preview")] [SerializeField]
        private bool previewInEditor;

        private Material _materialInstance;
        private float    _playbackTime;
        private bool     _isPlaying;
        private bool     _playOnce;

        private static readonly int PROP_SHINE = Shader.PropertyToID("_ShineIntensity");

        /// Initializes target graphic reference and sets up initial playback state.
        private void Awake(){
            if (!targetGraphic)
                targetGraphic = GetComponent<Graphic>();

            if (Application.isPlaying){
                if (randomStartOffset)
                    _playbackTime = Random.Range(0f, cycleDuration);

                _isPlaying = playOnAwake;
                SetupMaterialInstance();
            }
        }

        /// Ensures the material instance is created when enabling in play mode.
        private void OnEnable(){
            if (Application.isPlaying && !_materialInstance)
                SetupMaterialInstance();
        }

        /// Resets the shine intensity when disabled in play mode.
        private void OnDisable(){
            if (Application.isPlaying)
                SetShineIntensity(0f);
        }

        /// Cleans up the instantiated material.
        private void OnDestroy(){
            if (Application.isPlaying)
                Destroy(_materialInstance);
            else
                DestroyImmediate(_materialInstance);
        }

        /// Clones the target graphic material for independent flicker control.
        private void SetupMaterialInstance(){
            _materialInstance      = new Material(targetGraphic.material);
            targetGraphic.material = _materialInstance;
        }

        /// Advances playback progress and applies curve intensity to the material.
        private void Update(){
            if (!Application.isPlaying){
#if UNITY_EDITOR
                if (previewInEditor){
                    float editorProgress = Mathf.Repeat((float)UnityEditor.EditorApplication.timeSinceStartup / cycleDuration, 1.0f);
                    float editorValue    = flickerCurve.Evaluate(editorProgress) * intensityMultiplier;
                    targetGraphic.material.SetFloat(PROP_SHINE, editorValue);
                }
#endif
                return;
            }

            if (!_isPlaying)
                return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _playbackTime += dt;

            float progress = _playbackTime / cycleDuration;

            if (progress >= 1.0f){
                if (_playOnce || !loop){
                    _isPlaying = false;
                    _playOnce  = false;
                    SetShineIntensity(0f);
                    return;
                }

                _playbackTime = Mathf.Repeat(_playbackTime, cycleDuration);
                progress      = _playbackTime / cycleDuration;
            }

            SetShineIntensity(flickerCurve.Evaluate(progress) * intensityMultiplier);
        }

        /// Sets the shine intensity shader property on the material instance.
        private void SetShineIntensity(float value) => _materialInstance.SetFloat(PROP_SHINE, value);

        /// Starts or resumes continuous looping playback.
        public void Play(){
            _playOnce  = false;
            _isPlaying = true;
        }

        /// Pauses playback and resets shine intensity to zero.
        public void Stop(){
            _isPlaying = false;
            _playOnce  = false;
            SetShineIntensity(0f);
        }

        /// Plays the flicker animation curve once from start to finish.
        public void TriggerOnce(){
            _playbackTime = 0f;
            _playOnce     = true;
            _isPlaying    = true;
        }

#if UNITY_EDITOR
        /// Editor callback to keep target graphic referenced and reset preview intensity.
        private void OnValidate(){
            if (!targetGraphic)
                targetGraphic = GetComponent<Graphic>();

            if (!previewInEditor && !Application.isPlaying)
                targetGraphic.material.SetFloat(PROP_SHINE, 0f);
        }
#endif
    }
}
