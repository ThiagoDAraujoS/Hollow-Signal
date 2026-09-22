using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI.Dice{
    /// Launches physical dice from a specific origin with directional impulse and random torque.
    [DisallowMultipleComponent]
    public class DiceLauncher : MonoBehaviour{
        [Header("Dice References")]
        [SerializeField] private List<Rigidbody> dice = new();

        [Header("Spawn & Launch Position")]
        [SerializeField] private Transform launchOrigin;
        [SerializeField] private float diceSpacing = 0.25f;
        [SerializeField] private bool randomizeInitialOrientation = true;

        [Header("Launch Force & Trajectory")]
        [SerializeField] private Vector3 localLaunchDirection = new(0f, -0.5f, 1f);
        [SerializeField] private float launchForce = 8f;
        [SerializeField] private float forceVariance = 1.5f;
        [Range(0f, 45f)]
        [SerializeField] private float spreadAngle = 10f;

        [Header("Tumble & Torque")]
        [SerializeField] private float minTorque = 4f;
        [SerializeField] private float maxTorque = 12f;

        [Header("Input Trigger")]
        [SerializeField] private bool enableKeyTrigger = true;
        [SerializeField] private Key rollKey = Key.Space;

        [Header("Events")]
        public UnityEvent onRoll;

        /// Discovers child dice Rigidbodies if unassigned.
        private void Awake(){
            if (dice.Count == 0)
                dice.AddRange(GetComponentsInChildren<Rigidbody>());
        }

        /// Polls keyboard input trigger.
        private void Update(){
            if (enableKeyTrigger && Keyboard.current != null && Keyboard.current[rollKey].wasPressedThisFrame)
                RollDice();
        }

        /// Resets dice positions to origin and applies launching impulse and tumble torque.
        [ContextMenu("Roll Dice")]
        public void RollDice(){
            Transform origin = launchOrigin != null ? launchOrigin : transform;
            Vector3 baseDir = origin.TransformDirection(localLaunchDirection.normalized);

            int count = dice.Count;
            float totalWidth = (count - 1) * diceSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++){
                Rigidbody rb = dice[i];

                Vector3 localOffset = new(startX + (i * diceSpacing), 0f, 0f);
                rb.transform.position = origin.TransformPoint(localOffset);

                if (randomizeInitialOrientation)
                    rb.transform.rotation = Random.rotation;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Quaternion spreadRot = Quaternion.Euler(
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle),
                    0f);

                Vector3 dir = spreadRot * baseDir;
                float forceMag = launchForce + Random.Range(-forceVariance, forceVariance);
                rb.AddForce(dir.normalized * forceMag, ForceMode.Impulse);

                Vector3 randomTorqueAxis = Random.insideUnitSphere.normalized;
                float torqueMag = Random.Range(minTorque, maxTorque);
                rb.AddTorque(randomTorqueAxis * torqueMag, ForceMode.Impulse);
            }

            onRoll?.Invoke();
        }

        /// Visualizes launch trajectory and origin gizmos in the scene view.
        private void OnDrawGizmosSelected(){
            Transform origin = launchOrigin != null ? launchOrigin : transform;
            Gizmos.color = Color.cyan;

            Vector3 baseDir = origin.TransformDirection(localLaunchDirection.normalized);
            Gizmos.DrawRay(origin.position, baseDir * (launchForce * 0.2f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin.position, 0.05f);
        }
    }
}
