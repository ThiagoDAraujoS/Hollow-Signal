using System;
using Data.Effects;
using UnityEngine;

namespace Core.Crisis{
    /// Coordinates turn phases, round progression, combat state, and the unified world clock.
    [DisallowMultipleComponent]
    public class CrisisManager : MonoBehaviour{
        public static CrisisManager Instance{ get; private set; }

        [Header("Time Economy")]
        [SerializeField] private float roundDurationInSeconds = 60f;

        [Header("Scheduler")]
        [SerializeField] private EffectDatabase effectDatabase;
        [SerializeField] private CrisisScheduler scheduler = new();

        public CrisisPhase CurrentPhase{ get; private set; } = CrisisPhase.Exploration;
        public int         RoundNumber { get; private set; }
        public float       ElapsedWorldTime{ get; private set; }
        public CrisisScheduler Scheduler => scheduler;

        public bool IsCrisis => CurrentPhase != CrisisPhase.Exploration;

        public static event Action<float> OnWorldTimeAdvanced;
        public static event Action        OnCrisisStarted;
        public static event Action<int>   OnPlayerPhaseStarted;
        public static event Action        OnPlayerPhaseEnded;
        public static event Action        OnEnemyPhaseStarted;
        public static event Action        OnEnemyPhaseEnded;
        public static event Action        OnCrisisEnded;

        private void Awake() => Instance = this;

        private void OnDestroy() => Instance = Instance == this ? null : Instance;

        private void Update(){
            if (!IsCrisis)
                AdvanceTime(Time.deltaTime);
        }

        /// Yields execution until combat enters player phase or returns to exploration.
        public static CustomYieldInstruction WaitForPlayerPhase() =>
            new WaitUntil(() => Instance == null || Instance.CurrentPhase == CrisisPhase.PlayerPhase || Instance.CurrentPhase == CrisisPhase.Exploration);

        /// Initiates tactical crisis combat mode and begins the first round.
        [ContextMenu("Start Crisis")]
        public void StartCrisis(){
            CurrentPhase = CrisisPhase.PlayerPhase;
            RoundNumber  = 1;
            AdvanceTime(roundDurationInSeconds);
            OnCrisisStarted?.Invoke();
            OnPlayerPhaseStarted?.Invoke(RoundNumber);
        }

        /// Starts a new player turn phase, increments round counter, and advances world clock by one round.
        public void StartPlayerPhase(){
            CurrentPhase = CrisisPhase.PlayerPhase;
            RoundNumber++;
            AdvanceTime(roundDurationInSeconds);
            OnPlayerPhaseStarted?.Invoke(RoundNumber);
        }

        /// Concludes active player turn phase and transitions into enemy phase.
        [ContextMenu("End Player Phase -> Enemy")]
        public void EndPlayerPhase(){
            OnPlayerPhaseEnded?.Invoke();
            StartEnemyPhase();
        }

        /// Starts enemy turn phase and notifies AI listeners.
        public void StartEnemyPhase(){
            CurrentPhase = CrisisPhase.EnemyPhase;
            OnEnemyPhaseStarted?.Invoke();
        }

        /// Concludes enemy turn phase and advances round to the next player phase.
        [ContextMenu("End Enemy Phase -> Player")]
        public void EndEnemyPhase(){
            OnEnemyPhaseEnded?.Invoke();
            StartPlayerPhase();
        }

        /// Disengages tactical combat mode and restores real-time exploration.
        [ContextMenu("End Crisis")]
        public void EndCrisis(){
            CurrentPhase = CrisisPhase.Exploration;
            RoundNumber  = 0;
            OnCrisisEnded?.Invoke();
        }

        /// Advances world clock by specified delta and notifies all listeners.
        public void AdvanceTime(float deltaSeconds){
            ElapsedWorldTime += deltaSeconds;
            scheduler.AdvanceClock(ElapsedWorldTime, effectDatabase);
            OnWorldTimeAdvanced?.Invoke(ElapsedWorldTime);
        }
    }
}
