using System;
using System.Collections.Generic;
using Core.State;
using UnityEngine;

namespace Core.Crisis{
    [Serializable]
    public class CrisisTurn : ITracked{
        // ReSharper disable once MemberInitializerValueIgnored
        [SerializeField] private string key = "crisis_turn";
        [SerializeField] private bool hasMoved;
        [SerializeField] private bool hasActed;

        public event Action OnTurnReset;
        public event Action OnMoved;
        public event Action OnActed;
        public event Action OnTurnCompleted;

        public string Key => key;
        public bool HasMoved => hasMoved;
        public bool HasActed => hasActed;
        public bool CanMove => !hasMoved || !hasActed;
        public bool CanAct => !hasActed;
        public bool IsTurnCompleted => hasMoved && hasActed;

        public CrisisTurn(string key = "crisis_turn") => this.key = key;

        /// Resets turn resources for a new combat round.
        public void ResetTurn(){
            hasMoved = false;
            hasActed = false;
            OnTurnReset?.Invoke();
        }

        /// Consumes standard move, or converts action to take a safe second move.
        public void ConsumeMove(){
            if (!hasMoved)
                hasMoved = true;
            else if (!hasActed)
                hasActed = true;
            OnMoved?.Invoke();
            if (IsTurnCompleted)
                OnTurnCompleted?.Invoke();
        }

        /// Consumes the single major action for the turn.
        public void ConsumeAction(){
            hasActed = true;
            OnActed?.Invoke();
            if (IsTurnCompleted)
                OnTurnCompleted?.Invoke();
        }

        // TODO: SPRINT & MULTI-HERO DIALOGUE INTEGRATION
        // When a player attempts to move twice AND act (or act then move twice):
        // 1. Locate the acting Character's dedicated DialogueRunner / PRE_DialogUI instance.
        // 2. Start a dialogue coroutine prompting: "Attempt Sprint to act this turn?"
        //    Options: [1] Sprint (3d6 + Athletics vs DC 11), [2] Cancel.
        // 3. If Sprint is selected:
        //    - Roll 3d6 vs DC 11 (with optional 2 Composure effort push if hero has mobility mastery).
        //    - On Pass: Call ApplySprintSuccess() to retain the Action.
        //    - On Fail: Call ApplySprintFailure() which consumes the Action, applies [Winded] decorator, and ends turn.
        // 4. If Canceled: Character remains in current spot without consuming Action.

        /// Grants a bonus move from a successful Sprint test without burning the Action.
        public void ApplySprintSuccess(){
            hasMoved = true;
            OnMoved?.Invoke();
        }

        /// Fails a sprint attempt, consuming the Action and ending movement capability.
        public void ApplySprintFailure(){
            hasMoved = true;
            hasActed = true;
            OnActed?.Invoke();
            OnTurnCompleted?.Invoke();
        }

        /// Restores an action if an interaction or targeting was canceled before committing.
        public void RefundAction() => hasActed = false;

        /// Restores movement if a move command was canceled.
        public void RefundMove() => hasMoved = false;

        /// Serializes current turn state into the Blackboard partition.
        public void Save(Dictionary<string, object> state){
            state[$"{key}_moved"] = hasMoved;
            state[$"{key}_acted"] = hasActed;
        }

        /// Restores turn state from the Blackboard partition.
        public void Load(Dictionary<string, object> state){
            if (state.TryGetValue($"{key}_moved", out object rawMoved))
                hasMoved = Convert.ToBoolean(rawMoved);
            if (state.TryGetValue($"{key}_acted", out object rawActed))
                hasActed = Convert.ToBoolean(rawActed);
        }
    }
}
