using UnityEngine;
using World.Actors.Player;

namespace World{
    /// Contract for interactive objects in the world that can be operated by a character.
    public interface IUsable{
        /// World position where the character stands to interact.
        Vector3 UsePosition{ get; }

        /// World rotation the character aligns with upon arrival.
        Quaternion UseRotation{ get; }

        /// Executes the primary interaction on this object.
        void Use(CharacterSheet whosUsing);
    }
}
