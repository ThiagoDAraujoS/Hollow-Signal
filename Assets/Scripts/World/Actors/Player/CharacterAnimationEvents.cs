using UnityEngine;

namespace World.Actors.Player{
    /// Relays Unity animation events from Animator to CharacterMovement.
    [RequireComponent(typeof(CharacterMovement))]
    public class CharacterAnimationEvents : MonoBehaviour{
        private CharacterMovement _movement;

        /// Caches movement reference on awake.
        private void Awake() => _movement = GetComponent<CharacterMovement>();

        /// Animation event callback when the Use animation reaches its activation point.
        public void Use() => _movement.TriggerAnimationUse();
    }
}
