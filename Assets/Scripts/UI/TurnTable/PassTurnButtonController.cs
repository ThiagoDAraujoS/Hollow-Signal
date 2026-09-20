using Core.Crisis;
using UnityEngine;
using UnityEngine.Events;

namespace UI.TurnTable{
    [RequireComponent(typeof(MeshRenderer))]
    [DisallowMultipleComponent]
    public class PassTurnButtonController : MonoBehaviour{
        public enum ButtonState{
            Up,
            Highlighted,
            Down,
            TurnedOff
        }

        public enum Quadrant{
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [Header("State Mapping (2x2 Grid: Row x Column)")]
        [Tooltip("Row 1, Col 1 (Top-Left)")]
        public Quadrant upQuadrant = Quadrant.TopLeft;

        [Tooltip("Row 1, Col 2 (Top-Right)")]
        public Quadrant downQuadrant = Quadrant.TopRight;

        [Tooltip("Row 2, Col 1 (Bottom-Left)")]
        public Quadrant turnedOffQuadrant = Quadrant.BottomLeft;

        [Tooltip("Row 2, Col 2 (Bottom-Right)")]
        public Quadrant highlightedQuadrant = Quadrant.BottomRight;

        [Header("State")]
        [SerializeField] private ButtonState currentState = ButtonState.TurnedOff;

        [Header("Audio & Events")]
        public AudioSource audioSource;
        public AudioClip hoverClip;
        public AudioClip clickClip;
        public UnityEvent onTurnPassed;

        public MeshRenderer meshRenderer;
        private MaterialPropertyBlock _propBlock;
        private bool _isHovered;
        private bool _isPressed;

        private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

        public ButtonState CurrentState => currentState;
        public bool IsInteractable => CrisisManager.Instance.CurrentPhase == CrisisPhase.PlayerPhase;

        /// Captures renderer and allocates property block.
        private void Awake(){
            meshRenderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        /// Subscribes to CrisisManager lifecycle events.
        private void OnEnable(){
            CrisisManager.OnCrisisStarted += HandleCrisisStarted;
            CrisisManager.OnPlayerPhaseStarted += HandlePlayerPhaseStarted;
            CrisisManager.OnEnemyPhaseStarted += HandleEnemyPhaseStarted;
            CrisisManager.OnCrisisEnded += HandleCrisisEnded;
        }

        /// Unsubscribes from CrisisManager lifecycle events.
        private void OnDisable(){
            CrisisManager.OnCrisisStarted -= HandleCrisisStarted;
            CrisisManager.OnPlayerPhaseStarted -= HandlePlayerPhaseStarted;
            CrisisManager.OnEnemyPhaseStarted -= HandleEnemyPhaseStarted;
            CrisisManager.OnCrisisEnded -= HandleCrisisEnded;
        }

        /// Synchronizes initial button state with current crisis phase.
        private void Start() => SyncWithCrisisState();

        /// Synchronizes button state with active crisis phase.
        public void SyncWithCrisisState() =>
            SetState(CrisisManager.Instance.CurrentPhase == CrisisPhase.PlayerPhase ? ButtonState.Up : ButtonState.TurnedOff);

        /// Updates the quad's UV offset in the 2x2 atlas without instantiating materials.
        public void SetState(ButtonState state){
            currentState = state;

            Quadrant targetQuad = state switch{
                ButtonState.Up          => upQuadrant,
                ButtonState.Highlighted => highlightedQuadrant,
                ButtonState.Down        => downQuadrant,
                ButtonState.TurnedOff   => turnedOffQuadrant,
                _                       => upQuadrant
            };

            Vector2 offset = GetQuadrantOffset(targetQuad);
            Vector4 st = new(0.5f, 0.5f, offset.x, offset.y);

            meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetVector(BaseMapST, st);
            _propBlock.SetVector(MainTexST, st);
            meshRenderer.SetPropertyBlock(_propBlock);
        }

        /// Executes turn pass transition and advances combat phase to enemy.
        [ContextMenu("Pass Turn")]
        public void ExecutePassTurn(){
            if (!IsInteractable) return;
            if (audioSource && clickClip) audioSource.PlayOneShot(clickClip);
            onTurnPassed?.Invoke();
            CrisisManager.Instance.EndPlayerPhase();
            SetState(ButtonState.TurnedOff);
        }

        /// Handles cursor entering button collider.
        private void OnMouseEnter(){
            if (!IsInteractable) return;
            _isHovered = true;
            if (!_isPressed) SetState(ButtonState.Highlighted);
            if (audioSource && hoverClip) audioSource.PlayOneShot(hoverClip);
        }

        /// Handles cursor exiting button collider.
        private void OnMouseExit(){
            if (!IsInteractable) return;
            _isHovered = false;
            if (!_isPressed) SetState(ButtonState.Up);
        }

        /// Handles cursor pressing down on button collider.
        private void OnMouseDown(){
            if (!IsInteractable) return;
            _isPressed = true;
            SetState(ButtonState.Down);
        }

        /// Handles cursor release after click.
        private void OnMouseUp(){
            if (!_isPressed) return;
            _isPressed = false;
            if (_isHovered && IsInteractable) ExecutePassTurn();
            else SetState(IsInteractable ? ButtonState.Up : ButtonState.TurnedOff);
        }

        /// Restores button to up state when combat starts.
        private void HandleCrisisStarted() => SetState(ButtonState.Up);

        /// Restores button to up state when player phase starts.
        private void HandlePlayerPhaseStarted(int round) => SetState(ButtonState.Up);

        /// Turns off button when enemy phase starts.
        private void HandleEnemyPhaseStarted() => SetState(ButtonState.TurnedOff);

        /// Turns off button when combat ends.
        private void HandleCrisisEnded() => SetState(ButtonState.TurnedOff);

        /// Converts a quadrant enum to UV offset coordinates in the 2x2 grid.
        private static Vector2 GetQuadrantOffset(Quadrant quadrant) => quadrant switch{
            Quadrant.TopLeft     => new(0.0f, 0.5f),
            Quadrant.TopRight    => new(0.5f, 0.5f),
            Quadrant.BottomLeft  => new(0.0f, 0.0f),
            Quadrant.BottomRight => new(0.5f, 0.0f),
            _                    => Vector2.zero
        };
    }
}
