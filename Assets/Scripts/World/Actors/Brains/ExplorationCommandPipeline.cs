using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;
using World.Tactical;

namespace World.Actors.Brains{
    /// Command pipeline handling real-time exploration, multi-unit formations, and continuous steering.
    public class ExplorationCommandPipeline : ICommandPipeline{
        private readonly PartySelection _selection;
        private readonly List<Character> _activeParty;

        public ExplorationCommandPipeline(PartySelection selection, List<Character> activeParty){
            _selection   = selection;
            _activeParty = activeParty;
        }

        /// Moves selected units in formation around clicked destination.
        public void HandleGroundClicked(Vector3 destinationPoint) => MoveFormation(destinationPoint);

        /// Updates formation movement destination continuously while holding mouse button.
        public void HandleContinuousMove(Vector3 destinationPoint) => MoveFormation(destinationPoint);

        /// Directs lead character to navigate to and use the clicked slot.
        public void HandleSlotClicked(AreaSlot slot){
            Character lead = _selection.Lead;
            if (!lead || (!slot.IsAvailable && slot.Occupant != lead && slot.ReservedBy != lead))
                return;

            lead.movement.MoveToSlot(slot, () => slot.Use(lead.sheet));
        }

        /// Handles unit selection or lead promotion.
        public void HandleCharacterClicked(Character character, bool isAdditive){
            if (!_activeParty.Contains(character))
                return;

            if (isAdditive)
                _selection.AddUnitSelect(character);
            else if (_selection.Contains(character) && _selection.Count > 1)
                _selection.SetLead(character);
            else
                _selection.SingleUnitSelect(character);
        }

        /// Handles multi-unit marquee box selection.
        public void HandleMarqueeSelect(List<Character> enclosed, bool isAdditive){
            if (enclosed.Count == 0)
                return;

            if (isAdditive)
                _selection.AdditiveBoxSelect(enclosed);
            else
                _selection.DragboxSelect(enclosed);
        }

        /// Dispatches party formation movement on GoHere click.
        public void HandleGoHereClicked(GoHere goHere) => goHere.Send();

        /// Halts movement on all currently selected units.
        public void HandleStop(){
            foreach (Character unit in _selection.Selected)
                unit.movement.Stop();
        }

        /// Calculates formation positions and issues movement orders to movable units.
        private void MoveFormation(Vector3 destinationPoint){
            Character lead = _selection.Lead;
            if (lead && lead.dialogueSession && lead.dialogueSession.HasActiveDialogue)
                return;

            List<Character> movable = new(_selection.Count);
            foreach (Character unit in _selection.Selected)
                if (unit && !(unit.dialogueSession && unit.dialogueSession.HasActiveDialogue))
                    movable.Add(unit);

            if (movable.Count == 0)
                return;

            Dictionary<Character, Vector3> destinations =
                FormationCalculator.CalculateFormationPositions(destinationPoint, lead, movable);

            foreach ((Character unit, Vector3 destination) in destinations)
                unit.movement.MoveTo(destination);
        }
    }
}
