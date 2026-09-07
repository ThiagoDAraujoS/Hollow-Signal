using System.Collections.Generic;
using Actors.Player;
using UnityEngine;
using World;

namespace Actors.Brains {
    /// <summary>
    /// Handles the execution of player movement and interaction commands (Left Click).
    /// Responsible for ray-casting the world, routing interactions to interactive objects (IUsable),
    /// projecting squad formation destinations onto the NavMesh, and streaming continuous steering updates.
    /// </summary>
    public class PlayerCommandDispatcher {
        private readonly Camera _camera;
        private readonly LayerMask _groundLayer;
        private readonly float _continuousRepathInterval;
        private const float RayDistance = 300f;

        private float _lastContinuousCommandTime;

        /// <summary>
        /// True while the command button (Left Mouse Button) is held down.
        /// </summary>
        public bool IsCommandHeld { get; private set; }

        public PlayerCommandDispatcher(Camera camera, LayerMask groundLayer, float continuousRepathInterval = 0.08f) {
            _camera = camera;
            _groundLayer = groundLayer;
            _continuousRepathInterval = continuousRepathInterval;
        }

        /// <summary>
        /// Signals that the player has pressed down the command button, starting the hold timer.
        /// </summary>
        public void OnCommandStarted() {
            IsCommandHeld = true;
            _lastContinuousCommandTime = Time.unscaledTime;
        }

        /// <summary>
        /// Signals that the player has released the command button.
        /// </summary>
        public void OnCommandCanceled() => IsCommandHeld = false;

        /// <summary>
        /// Executes an instantaneous command at the clicked screen position.
        /// If an IUsable object is clicked, orders the squad lead to interact with it.
        /// Otherwise, commands all selected units to move in formation to the ground destination.
        /// </summary>
        /// <param name="screenPos">Cursor coordinates on screen in pixels.</param>
        /// <param name="lead">The currently designated squad leader.</param>
        /// <param name="selectedUnits">Collection of all characters currently selected.</param>
        public void ExecuteDirectCommand(Vector2 screenPos, Character lead, IReadOnlyCollection<Character> selectedUnits) {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, RayDistance, _groundLayer)) return;

            IUsable usable = hit.collider.GetComponentInParent<IUsable>();
            if (usable != null) {
                if (lead != null)
                    lead.movement.MoveToAndUse(usable, lead.sheet);
                return;
            }

            MoveSelectedTo(hit.point, lead, selectedUnits);
        }

        /// <summary>
        /// Evaluates continuous steering while the command button is held down (Diablo / CRPG style).
        /// Throttles path recalculation to avoid flooding NavMesh queries every frame.
        /// Only updates ground movement; ignores interactive objects while dragging.
        /// </summary>
        /// <param name="screenPos">Current cursor coordinates on screen.</param>
        /// <param name="lead">The squad leader.</param>
        /// <param name="selectedUnits">All selected characters to steer.</param>
        public void UpdateContinuous(Vector2 screenPos, Character lead, IReadOnlyCollection<Character> selectedUnits) {
            if (!IsCommandHeld || Time.unscaledTime - _lastContinuousCommandTime < _continuousRepathInterval) return;

            _lastContinuousCommandTime = Time.unscaledTime;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, RayDistance, _groundLayer))
                MoveSelectedTo(hit.point, lead, selectedUnits);
        }

        /// <summary>
        /// Calculates tactical formation positions around the destination point and orders
        /// each selected unit's CharacterMovement component to navigate to their assigned slot.
        /// </summary>
        /// <param name="destinationPoint">World coordinates of the clicked destination.</param>
        /// <param name="lead">The squad leader placed at slot 0.</param>
        /// <param name="selectedUnits">The units to arrange in formation.</param>
        public static void MoveSelectedTo(Vector3 destinationPoint, Character lead, IReadOnlyCollection<Character> selectedUnits) {
            if (selectedUnits.Count == 0) return;

            Dictionary<Character, Vector3> destinations =
                FormationCalculator.CalculateFormationPositions(destinationPoint, lead, selectedUnits);

            foreach ((Character unit, Vector3 destination) in destinations)
                unit.movement.MoveTo(destination);
        }

        /// <summary>
        /// Immediately halts navigation and clears paths for the specified units.
        /// </summary>
        /// <param name="units">Characters to stop.</param>
        public static void StopUnits(IEnumerable<Character> units) {
            foreach (Character unit in units)
                unit.movement.Stop();
        }

        /// <summary>
        /// Resets internal hold state when input is disabled or interrupted.
        /// </summary>
        public void Reset() => IsCommandHeld = false;
    }
}
