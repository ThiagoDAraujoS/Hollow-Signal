using System;
using System.Collections.Generic;
using UnityEngine;
using World.Actors.Player;
using World.Anchors;

namespace World.Actors.Brains{
    /// Encapsulates mouse gesture state for selection, double-click tracking, and context menu triggering.
    public class SelectionGestureHandler{
        private const float DoubleClickWindow = 0.3f;

        private Camera                   _camera;
        private readonly LayerMask       _characterLayer;
        private readonly PartySelection  _selection;
        private readonly List<Character> _activePartyMembers;
        private readonly float           _dragThreshold;
        private readonly float           _holdThreshold;
        private readonly Color           _boxBorderColor;
        private readonly Color           _boxFillColor;

        private Vector2 _pressStartPos;
        private float   _pressStartTime;
        private bool    _isPressed;
        private bool    _contextMenuFired;

        private Character _lastClickedCharacter;
        private float     _lastClickTime;

        public bool IsDragging{ get; private set; }

        public event Action<Vector2> OnContextMenuRequested;

        /// Constructs handler with input thresholds, layer masks, and visual preferences.
        public SelectionGestureHandler(
            Camera camera,
            LayerMask characterLayer,
            PartySelection selection,
            List<Character> activePartyMembers,
            float dragThreshold,
            float holdThreshold,
            Color boxBorderColor,
            Color boxFillColor){
            _camera             = camera;
            _characterLayer     = characterLayer;
            _selection          = selection;
            _activePartyMembers = activePartyMembers;
            _dragThreshold      = dragThreshold;
            _holdThreshold      = holdThreshold;
            _boxBorderColor     = boxBorderColor;
            _boxFillColor       = boxFillColor;
        }

        /// Updates the reference to the active world camera.
        public void SetCamera(Camera camera) => _camera = camera;

        /// Registers gesture start position and timestamp.
        public void OnPressStarted(Vector2 screenPos){
            _isPressed        = true;
            IsDragging        = false;
            _contextMenuFired = false;
            _pressStartPos    = screenPos;
            _pressStartTime   = Time.unscaledTime;
        }

        /// Checks drag distance and hold duration thresholds each frame.
        public void Update(Vector2 currentScreenPos){
            if (!_isPressed || _contextMenuFired) return;

            float dragDist = Vector2.Distance(_pressStartPos, currentScreenPos);
            if (dragDist >= _dragThreshold)
                IsDragging = true;
            else if (Time.unscaledTime - _pressStartTime >= _holdThreshold){
                _contextMenuFired = true;
                OnContextMenuRequested?.Invoke(currentScreenPos);
            }
        }

        /// Resolves tap selection, drag box selection, or deselect on empty ground click.
        public void OnPressCanceled(Vector2 releasePos, bool isShiftPressed){
            if (!_isPressed) return;
            _isPressed = false;

            if (IsDragging){
                IsDragging = false;
                List<Character> enclosed = SelectionScanner.GetCharactersInScreenRect(
                    _camera, _pressStartPos, releasePos, _activePartyMembers);

                if (enclosed.Count <= 0) return;

                if (isShiftPressed)
                    _selection.AdditiveBoxSelect(enclosed);
                else
                    _selection.DragboxSelect(enclosed);
            }
            else if (!_contextMenuFired){
                Character hitCharacter = SelectionScanner.RaycastCharacter(_camera, releasePos, _characterLayer);
                if (hitCharacter == null){
                    bool leadInDialogue = _selection.Lead != null && _selection.Lead.dialogueSession != null && _selection.Lead.dialogueSession.HasActiveDialogue;
                    if (!isShiftPressed && !leadInDialogue)
                        _selection.Clear();
                    return;
                }

                if (isShiftPressed)
                    _selection.AddUnitSelect(hitCharacter);
                else{
                    if (_selection.Contains(hitCharacter) && _selection.Count > 1)
                        _selection.SetLead(hitCharacter);
                    else
                        _selection.SingleUnitSelect(hitCharacter);

                    _lastClickedCharacter = hitCharacter;
                    _lastClickTime        = Time.unscaledTime;
                }
            }
        }

        /// Renders selection marquee box onto the screen GUI.
        public void DrawGUI(Vector2 currentScreenPos){
            if (!IsDragging) return;

            Vector2 guiStart   = new(_pressStartPos.x, Screen.height - _pressStartPos.y);
            Vector2 guiCurrent = new(currentScreenPos.x, Screen.height - currentScreenPos.y);
            Rect    guiRect    = SelectionScanner.GetScreenRect(guiStart, guiCurrent);

            SelectionScanner.DrawScreenRect(guiRect, _boxFillColor);
            SelectionScanner.DrawScreenRectBorder(guiRect, 2f, _boxBorderColor);
        }

        /// Resets active drag and hold state when input is canceled.
        public void Reset(){
            _isPressed        = false;
            IsDragging        = false;
            _contextMenuFired = false;
        }
    }
}
