using UnityEngine;

namespace Utilities{
    [RequireComponent(typeof(Light))]
    public class LampFlicker : MonoBehaviour{
        public float baseIntensity = 1f;
        public float minInterval = 2f;
        public float maxInterval = 6f;
        public float duration = 0.35f;
        public AnimationCurve curve = new(
            new(0f, 1f),
            new(0.15f, 0.1f),
            new(0.3f, 0.85f),
            new(0.45f, 0.05f),
            new(0.6f, 0.9f),
            new(0.75f, 0.2f),
            new(1f, 1f)
        );

        private Light _light;
        private float _timer;
        private float _nextFlickerTime;
        private bool _isFlickering;

        /// <summary>Caches the light component, records base intensity, and schedules the first flicker.</summary>
        private void Awake(){
            _light = GetComponent<Light>();
            baseIntensity = _light.intensity;
            ScheduleNext();
        }

        /// <summary>Updates flicker timing and modulates light intensity.</summary>
        private void Update(){
            if (!_isFlickering){
                if (Time.time >= _nextFlickerTime)
                    StartFlicker();
                return;
            }

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / duration);
            _light.intensity = baseIntensity * curve.Evaluate(t);

            if (t >= 1f){
                _isFlickering = false;
                _light.intensity = baseIntensity;
                ScheduleNext();
            }
        }

        /// <summary>Schedules the next random flicker timestamp.</summary>
        private void ScheduleNext() => _nextFlickerTime = Time.time + Random.Range(minInterval, maxInterval);

        /// <summary>Immediately triggers a flicker burst.</summary>
        [ContextMenu("Trigger Flicker")]
        public void StartFlicker(){
            _isFlickering = true;
            _timer = 0f;
        }
    }
}
