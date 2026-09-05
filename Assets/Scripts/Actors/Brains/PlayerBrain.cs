using Actors.Player;
using UnityEngine;
using World;

namespace Actors.Brains {
    /// Translates mouse input into raycasts, selecting characters or issuing movement commands.
    public class PlayerBrain : MonoBehaviour {
        /// The character currently responding to player commands.
        public CharacterMovement activeCharacter;
        
        /// An optional reference to the camera controller to update the follow target when a character is selected.
        public CameraController cameraController;
        
        private Camera _mainCamera;
        
        private void Awake() {
            _mainCamera = Camera.main;
        }

        private void Update() {
            ProcessSelection();
            ProcessMovementOrders();
        }

        /// Raycasts to select a character when the left mouse button is pressed.
        private void ProcessSelection() {
            if (!Input.GetMouseButtonDown(0)) return;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                CharacterMovement clickedCharacter = hit.collider.GetComponent<CharacterMovement>();
                if (clickedCharacter != null) {
                    activeCharacter = clickedCharacter;
                    if (cameraController != null) {
                        //    cameraController.target = activeCharacter.transform;
                    }
                }
            }
        }

        /// Raycasts to issue a movement command when the right mouse button is pressed.
        private void ProcessMovementOrders() {
            if (activeCharacter == null || !Input.GetMouseButtonDown(1)) return;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                activeCharacter.MoveTo(hit.point);
            }
        }
    }
}