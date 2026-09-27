using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;
using World.Tactical;

namespace World.Actors.Brains{
    /// Strategy interface for routing player gestures and movement orders based on gameplay mode.
    public interface ICommandPipeline{
        void HandleGroundClicked(Vector3 destinationPoint);
        void HandleContinuousMove(Vector3 destinationPoint);
        void HandleSlotClicked(AreaSlot slot);
        void HandleCharacterClicked(Character character, bool isAdditive);
        void HandleMarqueeSelect(List<Character> enclosed, bool isAdditive);
        void HandleGoHereClicked(GoHere goHere);
        void HandleStop();
    }
}
