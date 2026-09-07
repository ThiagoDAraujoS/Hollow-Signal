using Actors.Player;
using UnityEngine;

namespace World {
    /// <summary>
    /// Contract for interactive objects in the world that can be operated by a character.
    /// </summary>
    public interface IUsable {
        /// <summary>
        /// Dedicated transform (position and facing orientation) where the character stands to interact.
        /// </summary>
        Transform UseSpot { get; }
        Quaternion UseRotation { get; }

        /// <summary>
        /// Executes the primary interaction on this object.
        /// </summary>
        /// <param name="whosUsing">The CharacterSheet of the character performing the action.</param>
        void Use(CharacterSheet whosUsing);
    }
}
