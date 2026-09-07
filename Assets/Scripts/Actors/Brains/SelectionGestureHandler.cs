using System;
using System.Collections.Generic;
using Actors.Player;
using UnityEngine;
using World;

namespace Actors.Brains{
    /// <summary>
    /// Encapsulates mouse gesture state for selection:
    /// - Right-Click tap (< threshold distance & duration) -> Raycasts character for single unit selection.
    /// - Right-Click double tap -> Snaps camera to character body and follows until WASD.
    /// - Shift + Right-Click -> Adds unit to selection.
    /// - Right-Click drag (> threshold distance) -> Marquee drag box selection.
    /// - Shift + Right-Click drag -> Additive box selection.
    /// - Right-Click hold (> threshold duration without dragging) -> Fires context menu request.
    /// </summary>
    public class SelectionGestureHandler{
        private const float DoubleClickWindow = 0.3f;

        private readonly Camera          _camera;
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

        public void OnPressStarted(Vector2 screenPos){
            _isPressed        = true;
            IsDragging        = false;
            _contextMenuFired = false;
            _pressStartPos    = screenPos;
            _pressStartTime   = Time.unscaledTime;
        }

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
                if (hitCharacter == null) return;

                if (isShiftPressed)
                    _selection.AddUnitSelect(hitCharacter);
                else {
                    _selection.SingleUnitSelect(hitCharacter);

                    if (_lastClickedCharacter == hitCharacter && Time.unscaledTime - _lastClickTime <= DoubleClickWindow) {
                        CameraAnchor.Track(hitCharacter.BodyTransform);
                        _lastClickedCharacter = null;
                        _lastClickTime = 0f;
                    }
                    else {
                        _lastClickedCharacter = hitCharacter;
                        _lastClickTime = Time.unscaledTime;
                    }
                }
            }
        }

        public void DrawGUI(Vector2 currentScreenPos){
            if (!IsDragging) return;

            Vector2 guiStart   = new(_pressStartPos.x, Screen.height - _pressStartPos.y);
            Vector2 guiCurrent = new(currentScreenPos.x, Screen.height - currentScreenPos.y);
            Rect    guiRect    = SelectionScanner.GetScreenRect(guiStart, guiCurrent);

            SelectionScanner.DrawScreenRect(guiRect, _boxFillColor);
            SelectionScanner.DrawScreenRectBorder(guiRect, 2f, _boxBorderColor);
        }

        public void Reset(){
            _isPressed        = false;
            IsDragging        = false;
            _contextMenuFired = false;
        }
    }
}
