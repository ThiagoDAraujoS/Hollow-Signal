using System.Collections.Generic;
using Narrative.Dialog;
using UnityEngine;
using World.Actors.Player;
using World.Tactical;

namespace World.Actors.Brains{
    /// Handles player movement and interaction command execution, routing, and continuous steering.
    public class PlayerCommandDispatcher{
        private Camera _camera;
        private readonly LayerMask _groundLayer;
        private readonly float _continuousRepathInterval;
        private const float RayDistance = 300f;

        private float _lastContinuousCommandTime;

        public bool IsCommandHeld{ get; private set; }

        public PlayerCommandDispatcher(Camera camera, LayerMask groundLayer, float continuousRepathInterval = 0.08f){
            _camera                   = camera;
            _groundLayer              = groundLayer;
            _continuousRepathInterval = continuousRepathInterval;
        }

        /// Updates the reference to the active world camera.
        public void SetCamera(Camera camera) => _camera = camera;

        /// Starts command hold tracking and resets hold timer.
        public void OnCommandStarted(){
            IsCommandHeld              = true;
            _lastContinuousCommandTime = Time.unscaledTime;
        }

        /// Releases command hold tracking.
        public void OnCommandCanceled() => IsCommandHeld = false;

        /// Executes an instantaneous command at the clicked screen position.
        public void ExecuteDirectCommand(Vector2 screenPos, Character lead, IReadOnlyCollection<Character> selectedUnits){
            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, RayDistance, _groundLayer)) return;

            IUsable usable = hit.collider.GetComponentInParent<IUsable>();
            if (usable != null){
                IsCommandHeld = false;
                if (lead != null){
                    if (usable is DialogueBehaviour db && db.IsInUse && db.CurrentUser != lead) return;
                    if (usable is AreaSlot slot){
                        if (!slot.IsAvailable && slot.Occupant != lead && slot.ReservedBy != lead) return;
                        if (slot.LinkedUsable is DialogueBehaviour slotDb && slotDb.IsInUse && slotDb.CurrentUser != lead) return;
                    }
                    lead.movement.MoveToAndUse(usable, lead.sheet);
                }
                return;
            }

            MoveSelectedTo(hit.point, lead, selectedUnits);
        }

        /// Evaluates continuous movement steering updates while holding command button.
        public void UpdateContinuous(Vector2 screenPos, Character lead, IReadOnlyCollection<Character> selectedUnits){
            if (!IsCommandHeld || Time.unscaledTime - _lastContinuousCommandTime < _continuousRepathInterval) return;

            _lastContinuousCommandTime = Time.unscaledTime;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, RayDistance, _groundLayer))
                MoveSelectedTo(hit.point, lead, selectedUnits);
        }

        /// Arranges selected units in formation around destination and issues movement orders.
        public static void MoveSelectedTo(Vector3 destinationPoint, Character lead, IReadOnlyCollection<Character> selectedUnits){
            if (selectedUnits.Count == 0) return;

            List<Character> movableUnits = new(selectedUnits.Count);
            foreach (Character unit in selectedUnits){
                if (unit == null) continue;
                if (unit.dialogueSession != null && unit.dialogueSession.HasActiveDialogue) continue;
                movableUnits.Add(unit);
            }

            if (movableUnits.Count == 0) return;

            Dictionary<Character, Vector3> destinations =
                FormationCalculator.CalculateFormationPositions(destinationPoint, lead, movableUnits);

            foreach ((Character unit, Vector3 destination) in destinations)
                unit.movement.MoveTo(destination);
        }

        /// Halts navigation and clears paths for all specified units.
        public static void StopUnits(IEnumerable<Character> units){
            foreach (Character unit in units)
                unit.movement.Stop();
        }

        /// Resets command hold state when input is disabled or interrupted.
        public void Reset() => IsCommandHeld = false;
    }
}
