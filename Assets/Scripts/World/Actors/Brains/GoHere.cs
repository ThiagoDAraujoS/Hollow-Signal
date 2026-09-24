using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using World.Actors.Player;
using World.Anchors;

namespace World.Actors.Brains{
    public class GoHere : MonoBehaviour{
        [SerializeField] private HeroEnum   targetHeroes    = HeroEnum.All;
        [SerializeField] private bool       trackTarget     = true;
        [SerializeField] private UnityEvent onArrived;

        [Header("Editor Visuals")]
        [SerializeField] private bool showGizmo = true;

        private Vector3 _destination;

        /// Samples and caches valid NavMesh point on start.
        private void Start(){
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                _destination = hit.position;
            else
                _destination = transform.position;
        }

        /// Updates the target hero mask dynamically.
        public void SetHeroes(HeroEnum heroesMask) => targetHeroes = heroesMask;

        /// Dispatches targeted heroes in formation towards cached NavMesh destination and locks controls.
        public void Send(){
            List<Character> heroes = PlayerBrain.GetHeroes(targetHeroes);
            PlayerBrain.TurnControlsOff();
            Character leadHero = heroes.Contains(PlayerBrain.Lead) ? PlayerBrain.Lead : heroes[0];

            if (trackTarget)
                CameraAnchor.FollowHeroes(heroes);

            Dictionary<Character, Vector3> destinations =
                FormationCalculator.CalculateFormationPositions(_destination, leadHero, heroes);

            foreach ((Character unit, Vector3 destination) in destinations)
                unit.movement.MoveTo(destination);

            StartCoroutine(TrackArrivalRoutine(heroes, destinations));
        }

        /// Coroutine monitoring hero navigation completion before restoring controls and firing event.
        private IEnumerator TrackArrivalRoutine(List<Character> heroes, Dictionary<Character, Vector3> destinations){
            yield return null;
            bool arrived = false;

            while (!arrived){
                yield return null;
                arrived = true;

                foreach (Character hero in heroes){
                    if (Vector3.Distance(hero.WorldPosition, destinations[hero]) > 1.2f){
                        arrived = false;
                        break;
                    }
                }
            }

            CameraAnchor.StopFollow();
            PlayerBrain.TurnControlsOn();
            onArrived.Invoke();
        }

        /// Draws the flag and pointing hand icon gizmo in the editor scene view when enabled.
        private void OnDrawGizmos(){
            if (showGizmo)
                Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "GoHereIcon.png", true);
        }
    }
}
