using UnityEngine;
using Utilities;

namespace UI.TurnTable{
    public class TurnTableController : MonoBehaviour{
        [Header("Lights (Animate these in the Animation window)")]
        public Light centerLight;
        public Light allyLight;
        public Light enemyLight;
        public float centerBrightness;
        public float allyBrightness;
        public float enemyBrightness;

        [Header("Perpetual Filament Flicker")]
        public float flickerFrequency = 25f;
        public float microShimmerDepth = 0.08f;
        public float minFlickerInterval = 2f;
        public float maxFlickerInterval = 5f;
        public float dropDuration = 0.25f;
        public AnimationCurve dropCurve = new(
            new(0f, 1f),
            new(0.15f, 0.15f),
            new(0.35f, 0.9f),
            new(0.5f, 0.1f),
            new(0.7f, 0.95f),
            new(1f, 1f)
        );

        [Header("Gears (Animate these in the Animation window)")]
        [Tooltip("The primary gear driving all follower gears via GearDriver.")]
        public Transform keyGear;
        public Vector3 gearAxis = new(0f, 0f, 1f);
        [Tooltip("Current speed in degrees/second. Positive = forward, negative = reverse, 0 = stopped.")]
        public float gearSpeed;
        [Tooltip("When true, samples the throttleCurve to produce discrete clock ticks. When false, runs straight at 1f.")]
        public bool isClockThrottled;

        [Header("Clock Throttle Curve")]
        [Tooltip("Pivot speed where the curve evaluates at standard 1x speed. Higher gearSpeed evaluates the curve faster.")]
        public float referenceSpeed = 30f;
        [Tooltip("The velocity profile of one tick cycle. The last keyframe defines the loop duration.")]
        public AnimationCurve throttleCurve = new(
            new(0f, 0f),
            new(0.08f, 5f),
            new(0.16f, 0f),
            new(0.5f, 0f)
        );

        [Header("Sparks / Particles")]
        [Tooltip("Emitters triggered when gears rotate forward (gearSpeed >= 0).")]
        public ParticleSystem[] allySparkEmitters;
        [Tooltip("Emitters triggered when gears rotate in reverse (gearSpeed < 0).")]
        public ParticleSystem[] enemySparkEmitters;
        [Tooltip("Min number of distinct emitters to trigger per cast.")]
        public int minEmittersToCast = 1;
        [Tooltip("Max number of distinct emitters to trigger per cast.")]
        public int maxEmittersToCast = 2;
        [Tooltip("Particle burst count emitted per triggered emitter.")]
        public int particlesPerBurst = 15;

        private Stopwatch _dropWatch;
        private float _dropModifier = 1f;
        private float _nextDropTime;
        private float _throttleTimer;

        /// <summary>Initializes stopwatch routines.</summary>
        private void Awake() => Init();

        /// <summary>Ensures stopwatches are restored after hot reload.</summary>
        private void OnEnable() => Init();

        /// <summary>Instantiates non-serialized stopwatch instances if null.</summary>
        private void Init(){
            _dropWatch ??= new Stopwatch(dropDuration, p => _dropModifier = dropCurve.Evaluate(p));
            ScheduleNextDrop();
        }

        /// <summary>Updates modulated values and rotates gears.</summary>
        private void LateUpdate(){
            if (_dropWatch == null) Init();
            UpdateLights();
            UpdateGears();
        }

        /// <summary>Modulates animator-driven brightness fields with perpetual micro-shimmer and intermittent voltage drops.</summary>
        private void UpdateLights(){
            if (!_dropWatch.Run()){
                _dropModifier = 1f;
                if (Time.time >= _nextDropTime){
                    _dropWatch.Start();
                    ScheduleNextDrop();
                }
            }

            float noise = Mathf.PerlinNoise(Time.time * flickerFrequency, 0f);
            float micro = 1f - (noise * microShimmerDepth);
            float flicker = micro * _dropModifier;

            centerLight.intensity = centerBrightness * flicker;
            allyLight.intensity = allyBrightness * flicker;
            enemyLight.intensity = enemyBrightness * flicker;
        }

        /// <summary>Rotates key gear, multiplying gearSpeed by throttleCurve when throttled or 1f when straight.</summary>
        private void UpdateGears(){
            if (Mathf.Approximately(gearSpeed, 0f)) return;

            float duration = throttleCurve.length > 1 ? throttleCurve.keys[throttleCurve.length - 1].time : 1f;
            if (duration <= 0.0001f) duration = 1f;

            float speedRatio = Mathf.Abs(gearSpeed) / Mathf.Max(0.001f, referenceSpeed);
            _throttleTimer = (_throttleTimer + (Time.deltaTime * speedRatio)) % duration;

            float modifier = isClockThrottled ? throttleCurve.Evaluate(_throttleTimer) : 1f;
            keyGear.Rotate(gearAxis * (gearSpeed * modifier * Time.deltaTime), Space.Self);
        }

        /// <summary>Animation Event: Randomly triggers one or more emitters from the side currently rotating.</summary>
        [ContextMenu("Cast Particles")]
        public void CastParticles(){
            var pool = gearSpeed >= 0f ? allySparkEmitters : enemySparkEmitters;
            if (pool == null || pool.Length == 0) return;

            int count = Random.Range(minEmittersToCast, maxEmittersToCast + 1);
            count = Mathf.Clamp(count, 1, pool.Length);

            int[] indices = new int[pool.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            for (int i = 0; i < indices.Length; i++){
                int r = Random.Range(i, indices.Length);
                (indices[i], indices[r]) = (indices[r], indices[i]);
            }

            for (int i = 0; i < count; i++){
                var ps = pool[indices[i]];
                if (ps == null) continue;
                ps.Play();
                ps.Emit(particlesPerBurst);
            }
        }

        /// <summary>Animation Event alias for CastParticles.</summary>
        public void CastParticle() => CastParticles();

        /// <summary>Schedules next timestamp for intermittent voltage drops.</summary>
        [ContextMenu("Schedule Next Drop")]
        public void ScheduleNextDrop() => _nextDropTime = Time.time + Random.Range(minFlickerInterval, maxFlickerInterval);

        // =========================================================================
        // ANIMATION EVENT STUBS (Prevents missing receiver warnings if called by legacy clips)
        // =========================================================================

        /// <summary>Legacy animation event receiver stub.</summary>
        public void DecelerateGears(){}

        /// <summary>Legacy animation event receiver stub.</summary>
        public void AccelerateBurst(){}

        /// <summary>Legacy animation event receiver stub.</summary>
        public void StartContinuousSpin(){}

        /// <summary>Legacy animation event receiver stub.</summary>
        public void PickSideEnemy(){}

        /// <summary>Legacy animation event receiver stub.</summary>
        public void PickSideAlly(){}

        /// <summary>Legacy animation event receiver stub.</summary>
        public void WakeLowPower(){}

        /// <summary>Legacy animation event receiver stub.</summary>
        public void FadeDownLights(){}
    }
}
