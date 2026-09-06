using System;
using UnityEngine.InputSystem;

namespace Core.Input{
    /// <summary>
    /// Encapsulates an InputActionReference and its delegate callbacks.
    /// Simplifies bulk enabling, disabling, and event unbinding in OnEnable/OnDisable.
    /// </summary>
    public class BoundAction{
        private readonly InputActionReference                _actionRef;
        private readonly Action<InputAction.CallbackContext> _onPerformed;
        private readonly Action<InputAction.CallbackContext> _onStarted;
        private readonly Action<InputAction.CallbackContext> _onCanceled;

        public BoundAction(InputActionReference actionRef, Action<InputAction.CallbackContext> onPerformed){
            _actionRef   = actionRef;
            _onPerformed = onPerformed;
        }

        public BoundAction(InputActionReference                actionRef,
                           Action<InputAction.CallbackContext> onStarted,
                           Action<InputAction.CallbackContext> onCanceled){
            _actionRef  = actionRef;
            _onStarted  = onStarted;
            _onCanceled = onCanceled;
        }

        public void Enable(){
            if (_onPerformed != null) _actionRef.action.performed += _onPerformed;
            if (_onStarted != null) _actionRef.action.started     += _onStarted;
            if (_onCanceled != null) _actionRef.action.canceled   += _onCanceled;
            _actionRef.action.Enable();
        }

        public void Disable(){
            if (_onPerformed != null) _actionRef.action.performed -= _onPerformed;
            if (_onStarted != null) _actionRef.action.started     -= _onStarted;
            if (_onCanceled != null) _actionRef.action.canceled   -= _onCanceled;
            _actionRef.action.Disable();
        }
    }
}
