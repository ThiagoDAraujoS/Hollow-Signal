using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;
using World.Tactical;

namespace World.Actors.Brains{
    /// Command pipeline handling turn-based Crisis mode, single-hero focus, and ghost turn planning.
    public class CrisisCommandPipeline : ICommandPipeline{
        private readonly PartySelection  _selection;
        private readonly List<Character> _activeParty;

        public CrisisCommandPipeline(PartySelection selection, List<Character> activeParty){
            _selection   = selection;
            _activeParty = activeParty;
        }

        /// Resolves ground click to the nearest tactical slot and commands the Ghost planner.
        public void HandleGroundClicked(Vector3 destinationPoint){
            Character activeHero = _selection.Lead;
            if (!activeHero)
                return;

            Vector3 fromPos = GhostActor.Instance && GhostActor.Instance.ChannelingHero == activeHero
                ? GhostActor.Instance.Movement.transform.position
                : activeHero.WorldPosition;

            TacticalSlot slot = TacticalSpatialResolver.ResolveClickedSlot(destinationPoint, fromPos);
            if (slot != null)
                PlanGhostMoveToSlot(slot, activeHero);
        }

        /// Ignores continuous movement drag updates during Crisis mode.
        public void HandleContinuousMove(Vector3 destinationPoint){}

        /// Navigates the Ghost directly to the clicked slot.
        public void HandleSlotClicked(AreaSlot slot){
            Character activeHero = _selection.Lead;
            if (activeHero && slot is TacticalSlot tacticalSlot && (tacticalSlot.IsAvailable || tacticalSlot.Occupant == activeHero || tacticalSlot.ReservedBy == activeHero))
                PlanGhostMoveToSlot(tacticalSlot, activeHero);
        }

        /// Enforces single-hero selection and channels the Ghost for the newly selected hero.
        public void HandleCharacterClicked(Character character, bool isAdditive){
            if (!_activeParty.Contains(character))
                return;

            _selection.SingleUnitSelect(character);
            if (GhostActor.Instance)
                GhostActor.Instance.Channel(character);
        }

        /// Enforces single-hero selection when box selecting in Crisis mode.
        public void HandleMarqueeSelect(List<Character> enclosed, bool isAdditive){
            if (enclosed.Count == 0)
                return;

            Character target = enclosed[0];
            _selection.SingleUnitSelect(target);
            if (GhostActor.Instance)
                GhostActor.Instance.Channel(target);
        }

        /// Ignores GoHere triggers during Crisis mode.
        public void HandleGoHereClicked(GoHere goHere){}

        /// Halts movement on the Ghost.
        public void HandleStop(){
            if (GhostActor.Instance)
                GhostActor.Instance.Movement.Stop();
        }

        /// Directs the Ghost to navigate to the target slot and records path & zone transition milestones.
        private void PlanGhostMoveToSlot(TacticalSlot slot, Character hero){
            GhostActor ghost = GhostActor.Instance;
            if (!ghost)
                return;

            if (ghost.ChannelingHero != hero)
                ghost.Channel(hero);

            TacticalZone fromZone = TacticalZone.GetZoneAt(ghost.Movement.transform.position);
            TacticalZone toZone   = slot.ParentZone;

            ghost.Movement.MoveToSlot(slot, () => {
                ghost.PlanTrack.TargetSlot = slot;

                if (ghost.GhostCharacter.nmAgent.hasPath)
                    foreach (Vector3 corner in ghost.GhostCharacter.nmAgent.path.corners)
                        ghost.PlanTrack.Waypoints.Add(corner);

                if (fromZone != null && toZone != null && fromZone != toZone){
                    Vector3 transitionPoint = (fromZone.Center + toZone.Center) * 0.5f;
                    fromZone.AddExitEffectsToPlan(ghost.PlanTrack, transitionPoint, hero.sheet);
                    toZone.AddEnterEffectsToPlan(ghost.PlanTrack, transitionPoint, hero.sheet);
                }
            });
        }
    }
}
