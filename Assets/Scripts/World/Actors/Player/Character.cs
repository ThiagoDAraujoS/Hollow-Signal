﻿using System;
using Core.Managers;
using Core.State;
using Narrative.Dialog;
using UnityEngine;
using UnityEngine.AI;
using World.Actors.Brains;
using World.Tactical;

namespace World.Actors.Player{
    [RequireComponent(typeof(UniqueId))]
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(CharacterDialogueSession))]
    public class Character : MonoBehaviour, ISelectable{
        [NonSerialized] public CharacterMovement        movement;
        [NonSerialized] public CharacterSheet           sheet;
        [NonSerialized] public CharacterDialogueSession dialogueSession;
        [NonSerialized] public NavMeshAgent             nmAgent;
        [NonSerialized] public Animator                 animator;

        [Header("Hero Identity")]
        [SerializeField] public HeroEnum heroType;

        [Header("Selection & Visuals")] public Transform selectionCircle;

        [Tooltip("The parent object holding body, items, colliders, and visuals.")] [SerializeField]
        public GameObject body;

        private UniqueId _uniqueId;

        public AreaSlot CurrentSlot{ get; set; }

        /// Caches required component references across awake and edit-time calls.
        public void EnsureInitialized(){
            _uniqueId       = GetComponent<UniqueId>();
            movement        = GetComponent<CharacterMovement>();
            sheet           = GetComponent<CharacterSheet>();
            dialogueSession = GetComponent<CharacterDialogueSession>();

            // Animator is on the root Character object (or fallback to children)
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            // NavMeshAgent is on the Body child (or fallback to root/children)
            if (body != null)
                nmAgent = body.GetComponent<NavMeshAgent>();

            if (nmAgent == null)
                nmAgent = GetComponentInChildren<NavMeshAgent>(true);

            if (nmAgent == null)
                nmAgent = GetComponent<NavMeshAgent>();
        }

        /// Caches references and disables default standalone movement.
        private void Awake(){
            EnsureInitialized();
            movement.enabled = false;
        }

        /// Re-links component references upon reset.
        private void Reset() => EnsureInitialized();

        public Sheet      Sheet           => sheet;
        public Transform  SelectionCircle => selectionCircle;
        public Transform  BodyTransform   => body != null ? body.transform : transform;
        public Vector3    WorldPosition   => BodyTransform.position;
        public Quaternion WorldRotation   => BodyTransform.rotation;

        /// Activates the ground selection circle visual indicator.
        public void TurnSelectionCircleOn()  => selectionCircle.gameObject.SetActive(true);

        /// Deactivates the ground selection circle visual indicator.
        public void TurnSelectionCircleOff() => selectionCircle.gameObject.SetActive(false);

        /// Vacates and releases any currently occupied area slot.
        public void LeaveSlot(){
            if (CurrentSlot == null) return;
            AreaSlot slot = CurrentSlot;
            CurrentSlot = null;
            slot.Vacate();
        }

        /// Toggles visual body active state and disables movement components if inactive.
        public void SetVisualsActive(bool active){
            if (body != null) body.SetActive(active);

            if (active) return;
            if (movement != null) movement.enabled = false;
            if (nmAgent != null) nmAgent.enabled  = false;
        }

        /// Registers character with the session party, activates visuals, and warps to spawn.
        public void JoinParty(){
            EnsureInitialized();
            GameSessionManager.Instance.SetCharacterActive(_uniqueId.Id, true);
            SetVisualsActive(true);
            PlayerBrain.AddPartyMember(this);

            if (nmAgent != null){
                nmAgent.enabled = true;
                nmAgent.Warp(GameSessionManager.CurrentMapManager.DefaultSpawnPoint.position);
            }
            if (movement != null) movement.enabled = true;
        }
    }
}
