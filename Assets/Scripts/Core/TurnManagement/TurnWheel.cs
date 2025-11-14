using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Core.TurnManagement{
    [DefaultExecutionOrder(-100)]
    public class TurnWheel : MonoBehaviour{
        private static TurnWheel _i;
        
        public List<TurnUser> users = new(15);
        
        public UnityEvent onRoundEnd;
        public UnityEvent onTurnEnd;
        public UnityEvent onTurnStart;
        public UnityEvent onWheelChanged;
        public TurnUser   CurrentUser { get; private set; }
        
        private int  _prevIndex = -1;
        private int  _prevTbw;
        private int  _prevInit;
        private Guid _prevID;
        
        public static void AddUser(TurnUser user){
            if (_i.users.Contains(user)) return;
            _i.users.Add(user);
            RecalculateTurnElements();
            _i.onWheelChanged?.Invoke();
        }
        public static void RemoveUser(TurnUser user){
            if (!_i.users.Contains(user)) return;
            _i.users.Remove(user);
            RecalculateTurnElements();
            _i.onWheelChanged?.Invoke();
        }
        public static TurnUser NextTurn(){
            if (_i.users.Count == 0){
                _i._prevIndex  = -1;
                _i.CurrentUser = null;
                return null;
            }
            if (++_i._prevIndex >= _i.users.Count){
                _i._prevIndex = 0;
                _i.SetPreviousUser(_i._prevIndex);
                _i.onRoundEnd?.Invoke();
            }
            _i.SetPreviousUser(_i._prevIndex);
            return _i.CurrentUser;
        }
        public static void RecalculateTurnElements(){
            _i.users.RemoveAll(item => item == null);
            _i.users.Sort();
            _i.RelocateIterator();
        }
        private void RelocateIterator(){
            if (_prevIndex == -1 || users.Count == 0){
                _prevIndex = -1;
                return;
            }

            for (int i = 0; i < users.Count; i++){
                if (users[i].CompareTo(_prevInit, _prevTbw, _prevID) <= 0) continue;
                SetPreviousUser((i - 1 + users.Count) % users.Count);
                return;
            }
            SetPreviousUser(users.Count - 1);
        }
        private void SetPreviousUser(int previousIndex){
            TurnUser prev = users[previousIndex];
            _prevIndex = previousIndex;
            CurrentUser  = prev;
            _prevInit  = prev.Initiative;
            _prevTbw   = prev.Tbw;
            _prevID    = prev.Id;
        }

        private void Awake() => _i = this;
    }
}


