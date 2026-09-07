using System;
using System.Collections.Generic;
using Actors.Player;
using UnityEngine;

namespace Actors.Brains{
    /// <summary>
    /// Processes right-click mouse gestures: distinguishes between short tap (single unit select),
    /// stationary hold (context menu request), and click-and-drag (marquee box selection).
    /// </summary>
    public class SelectionGestureHandler{
        private readonly Camera          _camera;
        private readonly LayerMask       _characterLayer;
        private readonly PartySelection  _selection;
        private readonly List<Character> _activePartyMembers;
        private readonly float           _dragThreshold;
        private readonly float           _holdThreshold;
        private readonly Color           _boxBorderColor;
        private readonly Color           _boxFillColor;

        private bool    _isPressed;
        private bool    _contextMenuFired;
        private Vector2 _pressStartPos;
        private float   _pressStartTime;

        public event Action<Vector2> OnContextMenuRequested;

        public bool IsDragging{ get; private set; }

        public SelectionGestureHandler(
            Camera          camera,
            LayerMask       characterLayer,
            PartySelection  selection,
            List<Character> activePartyMembers,
            float           dragThreshold  = 10f,
            float           holdThreshold  = 0.35f,
            Color?          boxBorderColor = null,
            Color?          boxFillColor   = null){

            _camera             = camera;
            _characterLayer     = characterLayer;
            _selection          = selection;
            _activePartyMembers = activePartyMembers;
            _dragThreshold      = dragThreshold;
            _holdThreshold      = holdThreshold;
            _boxBorderColor     = boxBorderColor ?? new Color(0.2f, 0.8f, 0.2f, 0.9f);
            _boxFillColor       = boxFillColor ?? new Color(0.2f,   0.8f, 0.2f, 0.2f);
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
                    _selection.ToggleAddSelection(hitCharacter);
                else
                    _selection.SingleUnitSelect(hitCharacter);
            }
        }

        public void DrawGUI(Vector2 currentScreenPos){
            if (!IsDragging) return;

            float distance = Vector2.Distance(_pressStartPos, currentScreenPos);
            if (distance < _dragThreshold) return;

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
