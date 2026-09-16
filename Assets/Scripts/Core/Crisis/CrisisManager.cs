using System;
using UnityEngine;

namespace Core.Crisis{
    /// Coordinates turn phases, round progression, and combat state for tactical crisis encounters.
    [DisallowMultipleComponent]
    public class CrisisManager : MonoBehaviour{
        public static CrisisManager Instance{ get; private set; }

        public CrisisPhase CurrentPhase{ get; private set; } = CrisisPhase.Exploration;
        public int         RoundNumber { get; private set; }

        public bool IsCrisis => CurrentPhase != CrisisPhase.Exploration;

        public static event Action      OnCrisisStarted;
        public static event Action<int> OnPlayerPhaseStarted;
        public static event Action      OnPlayerPhaseEnded;
        public static event Action      OnEnemyPhaseStarted;
        public static event Action      OnEnemyPhaseEnded;
        public static event Action      OnCrisisEnded;

        /// Registers the active singleton instance.
        private void Awake() => Instance = this;

        /// Clears the singleton reference when destroyed.
        private void OnDestroy() => Instance = Instance == this ? null : Instance;

        /// Yields execution until combat enters player phase or returns to exploration.
        public static CustomYieldInstruction WaitForPlayerPhase() =>
            new WaitUntil(() => Instance == null || Instance.CurrentPhase == CrisisPhase.PlayerPhase || Instance.CurrentPhase == CrisisPhase.Exploration);

        /// Initiates tactical crisis combat mode and begins the first round.
        public void StartCrisis(){
            CurrentPhase = CrisisPhase.PlayerPhase;
            RoundNumber  = 1;
            OnCrisisStarted?.Invoke();
            OnPlayerPhaseStarted?.Invoke(RoundNumber);
        }

        /// Starts a new player turn phase and increments round counter.
        public void StartPlayerPhase(){
            CurrentPhase = CrisisPhase.PlayerPhase;
            RoundNumber++;
            OnPlayerPhaseStarted?.Invoke(RoundNumber);
        }

        /// Concludes active player turn phase and transitions into enemy phase.
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
        public void EndEnemyPhase(){
            OnEnemyPhaseEnded?.Invoke();
            StartPlayerPhase();
        }

        /// Disengages tactical combat mode and restores real-time exploration.
        public void EndCrisis(){
            CurrentPhase = CrisisPhase.Exploration;
            RoundNumber  = 0;
            OnCrisisEnded?.Invoke();
        }
    }
}
