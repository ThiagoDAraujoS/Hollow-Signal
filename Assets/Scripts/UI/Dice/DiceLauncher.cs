using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI.Dice{
    /// Launches physical dice from three explicit spawn transform slots with directional impulse and random torque.
    [DisallowMultipleComponent]
    public class DiceLauncher : MonoBehaviour{
        [Header("Dice References")]
        [SerializeField] private List<Rigidbody> dice = new();

        [Header("Spawn Slots (3 Transforms)")]
        [SerializeField] private Transform[] spawnSlots = new Transform[3];
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

        /// Resolves the world spawn position for a die at the given index.
        public Vector3 GetSpawnPosition(int index) =>
            index < spawnSlots.Length && spawnSlots[index] != null
                ? spawnSlots[index].position
                : transform.position;

        /// Measures the die bounding box extent along one axis.
        private float GetDiceCubeSize(){
            if (dice.Count > 0 && dice[0] != null){
                Collider col = dice[0].GetComponentInChildren<Collider>();
                if (col != null)
                    return col.bounds.size.x;
                Renderer rend = dice[0].GetComponentInChildren<Renderer>();
                if (rend != null)
                    return rend.bounds.size.x;
            }
            return 0.2f;
        }

        /// Teleports dice to their designated spawn transform positions and applies launch impulse and torque.
        [ContextMenu("Roll Dice")]
        public void RollDice(){
            Vector3 baseDir = transform.TransformDirection(localLaunchDirection.normalized);
            int count = dice.Count;

            for (int i = 0; i < count; i++){
                Rigidbody rb = dice[i];
                rb.transform.position = GetSpawnPosition(i);

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

        /// Visualizes spawn bounding box cubes and trajectory toss rays in the scene view.
        private void OnDrawGizmosSelected(){
            float cubeSize = GetDiceCubeSize();
            Vector3 cubeDimensions = new(cubeSize, cubeSize, cubeSize);
            Vector3 baseDir = transform.TransformDirection(localLaunchDirection.normalized);
            float rayLength = launchForce * 0.2f;

            for (int i = 0; i < spawnSlots.Length; i++){
                Vector3 spawnPos = GetSpawnPosition(i);

                Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
                Gizmos.DrawCube(spawnPos, cubeDimensions);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(spawnPos, cubeDimensions);

                Gizmos.color = Color.yellow;
                Vector3 endPos = spawnPos + baseDir * rayLength;
                Gizmos.DrawLine(spawnPos, endPos);
                Gizmos.DrawSphere(endPos, cubeSize * 0.15f);
            }
        }
    }
}
