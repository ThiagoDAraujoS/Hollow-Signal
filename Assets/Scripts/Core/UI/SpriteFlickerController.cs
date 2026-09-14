using System.Collections;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Controls a sprite's brightness flicker using a black-and-white mask.
    /// Drives the Custom/SpriteFlickerMask shader via MaterialPropertyBlocks.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SpriteFlickerController : MonoBehaviour
    {
        public enum FlickerStyle
        {
            BrokenFluorescent, // Erratic, jittery bursts with random micro-dropouts
            OrganicNoise,       // Smooth Perlin noise candle/fire flutter
            SinePulse,          // Clean sinusoidal breathing/strobe
            CustomCurve,        // Driven by a designer-specified AnimationCurve
            ManualOnly          // Controlled exclusively via script triggers
        }

        [Header("Target Renderer")]
        [SerializeField] private Renderer targetRenderer;

        [Header("Flicker Mode")]
        [SerializeField] private FlickerStyle style = FlickerStyle.BrokenFluorescent;
        [SerializeField] private bool playOnAwake = true;

        [Header("Brightness Tuning")]
        [Tooltip("Resting brightness of the masked (white) area. (1 = normal color, 0 = blacked out).")]
        [Range(0f, 2f)]
        [SerializeField] private float baseBrightness = 1.0f;

        [Tooltip("Peak brightness of the masked (white) area when at maximum flicker surge.")]
        [Range(1f, 10f)]
        [SerializeField] private float peakBrightness = 3.5f;

        [Tooltip("Optional glow tint added to the white mask region during peaks.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color flickerGlowColor = Color.white;

        [Header("Broken Fluorescent Settings")]
        [Tooltip("Speed/frequency of the erratic stutter.")]
        [Range(5f, 60f)]
        [SerializeField] private float stutterSpeed = 30f;

        [Tooltip("Probability (0-1) per frame of entering a sudden blackout dip.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float blackoutChance = 0.05f;

        [Header("Sine / Organic Settings")]
        [Tooltip("Frequency of pulse or noise oscillation.")]
        [Range(0.1f, 15f)]
        [SerializeField] private float speed = 3f;

        [Header("Custom Curve")]
        [SerializeField] private AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float curveDuration = 1.5f;

        private MaterialPropertyBlock _propBlock;
        private Coroutine _burstCoroutine;
        private float _currentFlickerAmount;
        private float _temporaryBurstAmount;
        private float _noiseSeed;

        private static readonly int PROP_FLICKER_AMOUNT = Shader.PropertyToID("_FlickerAmount");
        private static readonly int PROP_BASE_BRIGHTNESS = Shader.PropertyToID("_BaseBrightness");
        private static readonly int PROP_MAX_BRIGHTNESS = Shader.PropertyToID("_MaxBrightness");
        private static readonly int PROP_FLICKER_COLOR = Shader.PropertyToID("_FlickerColor");

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            _noiseSeed = Random.Range(0f, 1000f);
        }

        private void Start()
        {
            ApplyPropertiesToMaterial();
        }

        private void Update()
        {
            if (targetRenderer == null) return;

            if (Application.isPlaying && !playOnAwake && _burstCoroutine == null)
            {
                _currentFlickerAmount = 0f;
            }
            else
            {
                _currentFlickerAmount = EvaluateFlicker(Time.time);
            }

            float finalAmount = Mathf.Clamp01(_currentFlickerAmount + _temporaryBurstAmount);
            ApplyFlicker(finalAmount);
        }

        private float EvaluateFlicker(float time)
        {
            switch (style)
            {
                case FlickerStyle.BrokenFluorescent:
                {
                    // Layered erratic noise
                    float t = time * stutterSpeed;
                    float n1 = Mathf.PerlinNoise(_noiseSeed, t * 0.4f);
                    float n2 = Mathf.PerlinNoise(t * 1.3f, _noiseSeed);
                    float combined = Mathf.Pow(n1 * n2, 2.5f) * 2.5f;

                    // Occasional sudden blackout/glitch drop
                    if (Random.value < blackoutChance)
                    {
                        combined *= 0.1f;
                    }

                    return Mathf.Clamp01(combined);
                }

                case FlickerStyle.OrganicNoise:
                {
                    float val = Mathf.PerlinNoise(_noiseSeed + time * speed, _noiseSeed);
                    return Mathf.Clamp01(val);
                }

                case FlickerStyle.SinePulse:
                {
                    float sine = Mathf.Sin(time * speed * Mathf.PI * 2f) * 0.5f + 0.5f;
                    return sine;
                }

                case FlickerStyle.CustomCurve:
                {
                    if (curveDuration <= 0f) return 0f;
                    float progress = Mathf.Repeat(time / curveDuration, 1.0f);
                    return Mathf.Clamp01(customCurve.Evaluate(progress));
                }

                case FlickerStyle.ManualOnly:
                default:
                    return 0f;
            }
        }

        private void ApplyPropertiesToMaterial()
        {
            if (targetRenderer == null) return;

            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }

            targetRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(PROP_BASE_BRIGHTNESS, baseBrightness);
            _propBlock.SetFloat(PROP_MAX_BRIGHTNESS, peakBrightness);
            _propBlock.SetColor(PROP_FLICKER_COLOR, flickerGlowColor);
            targetRenderer.SetPropertyBlock(_propBlock);
        }

        private void ApplyFlicker(float amount)
        {
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }

            targetRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(PROP_FLICKER_AMOUNT, amount);
            _propBlock.SetFloat(PROP_BASE_BRIGHTNESS, baseBrightness);
            _propBlock.SetFloat(PROP_MAX_BRIGHTNESS, peakBrightness);
            _propBlock.SetColor(PROP_FLICKER_COLOR, flickerGlowColor);
            targetRenderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Triggers a sudden temporary flicker burst (e.g. on button hover, click, or scare event).
        /// </summary>
        public void TriggerBurst(float duration = 0.6f, int flickers = 5)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_burstCoroutine != null)
            {
                StopCoroutine(_burstCoroutine);
            }

            _burstCoroutine = StartCoroutine(DoBurstRoutine(duration, flickers));
        }

        private IEnumerator DoBurstRoutine(float duration, int flickers)
        {
            float elapsed = 0f;
            float interval = duration / (flickers * 2f);

            while (elapsed < duration)
            {
                _temporaryBurstAmount = Random.value > 0.35f ? 1.0f : 0.0f;
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            _temporaryBurstAmount = 0f;
            _burstCoroutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            ApplyPropertiesToMaterial();
        }
#endif
    }
}
