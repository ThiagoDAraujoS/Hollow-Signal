using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Dice{
    public class DiceRollController : MonoBehaviour{
        public static DiceRollController Instance{ get; private set; }
        public static event Action<int[]> OnRollCompleted;

        [Header("Components")]
        [SerializeField] private DiceLauncher launcher;
        [SerializeField] private List<Die> dice = new();

        [Header("Showcase Targets")]
        [SerializeField] private Transform[] showcaseSlots = new Transform[3];
        [SerializeField] private Camera showcaseCamera;

        [Header("Timing")]
        [SerializeField] private float minRollDuration = 0.35f;
        [SerializeField] private float settleCheckDuration = 0.12f;
        [SerializeField] private float settleTimeout = 2.0f;
        [SerializeField] private float showcaseMoveSpeed = 8f;
        [SerializeField] private float showcaseDisplayDuration = 2.0f;

        [Header("Events")]
        public UnityEvent<int[]> onRollCompleted;
        public UnityEvent onRollReset;

        private Action<int[]> _pendingCallback;

        /// Assigns singleton instance.
        private void Awake() => Instance = this;

        /// Cleans up singleton instance on destroy.
        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }

        /// Static helper to trigger a roll sequence from anywhere with a callback.
        public static void Roll(Action<int[]> callback = null) => Instance.ReleaseDice(callback);

        /// Evaluates whether all dice have dropped below motion thresholds.
        private bool AreAllDiceSettled(){
            foreach (Die die in dice)
                if (!die.IsSettled())
                    return false;
            return true;
        }

        /// Resets physics and visibility, then triggers launch and settling sequence.
        [ContextMenu("Release Dice")]
        public void ReleaseDice() => ReleaseDice(null);

        /// Launches dice with an optional completion callback.
        public void ReleaseDice(Action<int[]> callback){
            _pendingCallback = callback;
            StopAllCoroutines();
            foreach (Die die in dice){
                die.gameObject.SetActive(true);
                die.SetKinematic(false);
            }

            launcher.RollDice();
            StartCoroutine(RollSequence());
        }

        /// Waits for dice settling via WaitUntil, tallies results, showcases, and resets.
        private IEnumerator RollSequence(){
            Transform camTransform = showcaseCamera.transform;

            yield return new WaitForSeconds(minRollDuration);

            float timeout = Time.time + settleTimeout;
            yield return new WaitUntil(() => {
                if (AreAllDiceSettled())
                    return true;
                return Time.time > timeout;
            });

            yield return new WaitForSeconds(settleCheckDuration);

            int[] results = new int[dice.Count];

            for (int i = 0; i < dice.Count; i++){
                Die die = dice[i];
                die.SetKinematic(true);

                Die.Face winningFace = die.GetWinningFace();
                results[i] = winningFace.value;

                Quaternion targetRot = die.GetShowcaseRotation(winningFace, camTransform.forward, camTransform.up);
                StartCoroutine(MoveToShowcase(die.transform, showcaseSlots[i].position, targetRot));
            }

            onRollCompleted?.Invoke(results);
            OnRollCompleted?.Invoke(results);

            Action<int[]> callback = _pendingCallback;
            _pendingCallback = null;
            callback?.Invoke(results);

            yield return new WaitForSeconds(showcaseDisplayDuration);
            ResetDice();
        }

        /// Smoothly moves and rotates a die to its designated showcase slot.
        private IEnumerator MoveToShowcase(Transform target, Vector3 destPos, Quaternion destRot){
            while (Vector3.Distance(target.position, destPos) > 0.005f || Quaternion.Angle(target.rotation, destRot) > 0.5f){
                target.position = Vector3.MoveTowards(target.position, destPos, showcaseMoveSpeed * Time.deltaTime);
                target.rotation = Quaternion.RotateTowards(target.rotation, destRot, showcaseMoveSpeed * 100f * Time.deltaTime);
                yield return null;
            }

            target.position = destPos;
            target.rotation = destRot;
        }

        /// Hides dice and resets the roll system.
        [ContextMenu("Reset Dice")]
        public void ResetDice(){
            foreach (Die die in dice)
                die.gameObject.SetActive(false);

            onRollReset?.Invoke();
        }
    }
}
