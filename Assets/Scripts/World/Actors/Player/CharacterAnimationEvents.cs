using System;
using UnityEngine;

namespace World.Actors.Player{
    /// Relays Unity animation events from Animator to gameplay listeners.
    public class CharacterAnimationEvents : MonoBehaviour{
        public event Action OnUse;

        /// Animation event callback when the Use animation reaches its activation point.
        public void Use() => OnUse?.Invoke();
    }
}
