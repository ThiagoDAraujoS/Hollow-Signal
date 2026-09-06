using System;
using Core.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace Actors.Player{
    public class Character : MonoBehaviour, ISelectable{
        public  CharacterMovement movement;
        public  CharacterSheet    sheet;
        public  NavMeshAgent      nmAgent;
        public  Animator          animator;

        private void Start(){
            GameSessionManager.OnMapLoaded += OnMapLoaded;
        }

        private void OnDestroy(){
            GameSessionManager.OnMapLoaded -= OnMapLoaded;
        }

        private void OnMapLoaded(MapManager manager){
            nmAgent.enabled = true;
        }

        public Sheet Sheet => sheet;
    }
}