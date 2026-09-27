using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;
using World.Tactical;

namespace World.Actors.Brains{
    /// Command pipeline handling turn-based Crisis mode, single-hero focus, and tactical slot routing.
    public class CrisisCommandPipeline : ICommandPipeline{
        private readonly PartySelection _selection;
        private readonly List<Character> _activeParty;

        public CrisisCommandPipeline(PartySelection selection, List<Character> activeParty){
            _selection   = selection;
            _activeParty = activeParty;
        }

        /// Resolves ground click to the nearest tactical slot and navigates the active hero there.
        public void HandleGroundClicked(Vector3 destinationPoint){
            Character activeHero = _selection.Lead;
            if (!activeHero)
                return;

            TacticalSlot slot = TacticalSpatialResolver.ResolveClickedSlot(destinationPoint, activeHero.WorldPosition);
            if (slot != null)
                activeHero.movement.MoveToSlot(slot);
        }

        /// Ignores continuous movement drag updates during Crisis mode.
        public void HandleContinuousMove(Vector3 destinationPoint){}

        /// Navigates active hero directly to clicked slot.
        public void HandleSlotClicked(AreaSlot slot){
            Character activeHero = _selection.Lead;
            if (activeHero && (slot.IsAvailable || slot.Occupant == activeHero || slot.ReservedBy == activeHero))
                activeHero.movement.MoveToSlot(slot);
        }

        /// Enforces single-hero selection during Crisis mode.
        public void HandleCharacterClicked(Character character, bool isAdditive){
            if (_activeParty.Contains(character))
                _selection.SingleUnitSelect(character);
        }

        /// Enforces single-hero selection when box selecting in Crisis mode.
        public void HandleMarqueeSelect(List<Character> enclosed, bool isAdditive){
            if (enclosed.Count > 0)
                _selection.SingleUnitSelect(enclosed[0]);
        }

        /// Ignores GoHere triggers during Crisis mode.
        public void HandleGoHereClicked(GoHere goHere){}

        /// Halts movement on the active hero.
        public void HandleStop(){
            if (_selection.Lead)
                _selection.Lead.movement.Stop();
        }
    }
}
