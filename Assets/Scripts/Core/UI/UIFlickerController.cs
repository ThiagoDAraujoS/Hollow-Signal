using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    /// <summary>
    /// Drives the UI/FlickerMask shader using an AnimationCurve.
    /// Adds HDR shine exclusively to the white (R channel) areas of the mask.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIFlickerController : MonoBehaviour
    {
        [Header("Target Graphic")]
        [SerializeField] private Graphic targetGraphic;

        [Header("Curve Control")]
        [Tooltip("Curve defining the flicker shine over normalized time (0 to 1).")]
        [SerializeField]
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

        [Tooltip("Duration in seconds for one full cycle of the curve.")]
        [Range(0.05f, 10f)]
        [SerializeField] private float cycleDuration = 1.0f;

        [Tooltip("Multiplier applied to the curve value. Higher numbers push into HDR/Bloom values.")]
        [Range(0f, 15f)]
        [SerializeField] private float intensityMultiplier = 3.5f;

        [Header("Playback")]
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool useUnscaledTime = true; // Ensures UI flickers even if Time.timeScale is 0 (paused)
        [Tooltip("Offset the start time randomly so multiple buttons don't flicker identically.")]
        [SerializeField] private bool randomStartOffset = true;

        [Header("Editor Preview")]
        [SerializeField] private bool previewInEditor = false;

        private Material _materialInstance;
        private float _playbackTime;
        private bool _isPlaying;
        private bool _playOnce;

        private static readonly int PROP_SHINE = Shader.PropertyToID("_ShineIntensity");

        private void Awake()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (Application.isPlaying)
            {
                if (randomStartOffset)
                {
                    _playbackTime = Random.Range(0f, cycleDuration);
                }

                _isPlaying = playOnAwake;
                SetupMaterialInstance();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SetupMaterialInstance();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                SetShineIntensity(0f);
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_materialInstance);
                }
                else
                {
                    DestroyImmediate(_materialInstance);
                }
                _materialInstance = null;
            }
        }

        private void SetupMaterialInstance()
        {
            if (targetGraphic == null || targetGraphic.material == null) return;

            // Clone material so this UI element can have its own independent flicker value
            if (_materialInstance == null)
            {
                _materialInstance = new Material(targetGraphic.material);
                targetGraphic.material = _materialInstance;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (previewInEditor && targetGraphic != null && targetGraphic.material != null)
                {
                    float editorProgress = Mathf.Repeat((float)UnityEditor.EditorApplication.timeSinceStartup / Mathf.Max(cycleDuration, 0.001f), 1.0f);
                    float editorValue = flickerCurve.Evaluate(editorProgress) * intensityMultiplier;
                    targetGraphic.material.SetFloat(PROP_SHINE, editorValue);
                }
#endif
                return;
            }

            if (!_isPlaying || cycleDuration <= 0.0001f) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _playbackTime += dt;

            float progress = _playbackTime / cycleDuration;

            if (progress >= 1.0f)
            {
                if (_playOnce || !loop)
                {
                    _isPlaying = false;
                    _playOnce = false;
                    SetShineIntensity(0f);
                    return;
                }

                _playbackTime = Mathf.Repeat(_playbackTime, cycleDuration);
                progress = _playbackTime / cycleDuration;
            }

            float curveValue = flickerCurve.Evaluate(progress);
            float finalIntensity = curveValue * intensityMultiplier;

            SetShineIntensity(finalIntensity);
        }

        private void SetShineIntensity(float value)
        {
            if (_materialInstance != null)
            {
                _materialInstance.SetFloat(PROP_SHINE, value);
            }
            else if (targetGraphic != null && targetGraphic.material != null)
            {
                targetGraphic.material.SetFloat(PROP_SHINE, value);
            }
        }

        /// <summary>
        /// Starts or resumes continuous looping playback.
        /// </summary>
        public void Play()
        {
            _playOnce = false;
            _isPlaying = true;
        }

        /// <summary>
        /// Pauses the flicker and resets shine to 0.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _playOnce = false;
            SetShineIntensity(0f);
        }

        /// <summary>
        /// Plays the curve once from start to finish (e.g. on button click or hover event).
        /// </summary>
        public void TriggerOnce()
        {
            _playbackTime = 0f;
            _playOnce = true;
            _isPlaying = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (!previewInEditor && targetGraphic != null && targetGraphic.material != null && !Application.isPlaying)
            {
                targetGraphic.material.SetFloat(PROP_SHINE, 0f);
            }
        }
#endif
    }
}
