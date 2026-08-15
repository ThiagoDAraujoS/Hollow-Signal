using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace Core.TurnManagement{
    [DefaultExecutionOrder(-100)]
    public class TurnWheel : MonoBehaviour{
        private static TurnWheel _i;

        [SerializeField] private bool isCrisis;
        
        public static bool IsCrisis{
            get => _i.isCrisis;
            set{
                if (_i.isCrisis == value) return;
                _i.isCrisis = value;
                if (_i.isCrisis) _i.onCrisisStartEvent.Invoke();
                else _i.onCrisisEndEvent.Invoke();
            }
        }
        public static event UnityAction OnCrisisStart{
            add => _i.onCrisisStartEvent.AddListener(value);
            remove => _i.onCrisisStartEvent.RemoveListener(value);
        }
        public static event UnityAction OnCrisisEnd{
            add => _i.onCrisisEndEvent.AddListener(value);
            remove => _i.onCrisisEndEvent.RemoveListener(value);
        }
        public static event UnityAction OnTurnStart{
            add => _i.onTurnStartEvent.AddListener(value);
            remove => _i.onTurnStartEvent.RemoveListener(value);
        }
        public static event UnityAction OnTurnEnd{
            add => _i.onTurnEndEvent.AddListener(value);
            remove => _i.onTurnEndEvent.RemoveListener(value);
        }
        public static event UnityAction OnRoundEnd{
            add => _i.onRoundEndEvent.AddListener(value);
            remove => _i.onRoundEndEvent.RemoveListener(value);
        }
        public static event UnityAction OnWheelChanged{
            add => _i.onWheelChangedEvent.AddListener(value);
            remove => _i.onWheelChangedEvent.RemoveListener(value);
        }
        
        public List<TurnUser> users = new(15);
        
        public UnityEvent
            onTurnEndEvent,
            onTurnStartEvent,
            onRoundEndEvent,
            onWheelChangedEvent,
            onCrisisStartEvent,
            onCrisisEndEvent;
        
        public CoroutineComponent[]
            onTurnEndRoutine,
            onTurnStartRoutine,
            onRoundEndRoutine;
        
        public static TurnUser User => _i._user;

        private TurnUser _user;
        private Guid     _id;
        private int      
            _index = -1, 
            _tbw, 
            _init;
        
        private bool _isPassingTurn;

        public static void PassTurn(){
            if(!_i._isPassingTurn)
                _i.StartCoroutine(_i.EndTurnRoutine());
        }
        public static void AddUser(TurnUser user){
            if (_i.users.Contains(user) || user == null) return;
            _i.users.Add(user);
            RecalculateTurnElements();
            user.OnEnterTurnSystem();
            _i.onWheelChangedEvent?.Invoke();
            if(_i.users.Count == 1)
                PassTurn();
        }
        public static void RemoveUser(TurnUser user){
            if (!_i.users.Contains(user) || user == null) return;
            _i.users.Remove(user);
            RecalculateTurnElements();
            user.OnLeaveTurnSystem();
            _i.onWheelChangedEvent?.Invoke();
        }
        private IEnumerator EndTurnRoutine(){
            _isPassingTurn = true;
            User?.OnTurnEnd();
            onTurnEndEvent?.Invoke();
            yield return this.Multicast(onTurnEndRoutine);
            yield return NextTurn();
            onTurnStartEvent?.Invoke();
            yield return this.Multicast(onTurnStartRoutine);
            User!.OnTurnStart();
            _isPassingTurn = false;
        }
        
        private IEnumerator NextTurn(){
            if (users.Count == 0){
                _index  = -1;
                _user = null;
            }
            else if (++_index >= users.Count){
                _index = 0;
                SetCurrentUser(_index);
                onRoundEndEvent?.Invoke();
                yield return this.Multicast(onRoundEndRoutine);
                foreach (TurnUser user in users)
                    user.OnRoundEnd();
            }
            else
                SetCurrentUser(_index);
        }
        public static void RecalculateTurnElements(){
            _i.users.RemoveAll(item => item == null);
            _i.users.Sort();
            _i.RelocateIterator();
        }
        private void RelocateIterator(){
            if (_index == -1 || users.Count == 0){
                _index = -1;
                return;
            }

            for (int i = 0; i < users.Count; i++){
                if (users[i].CompareTo(_init, _tbw, _id) <= 0) continue;
                SetCurrentUser((i - 1 + users.Count) % users.Count);
                return;
            }
            SetCurrentUser(users.Count - 1);
        }
        private void SetCurrentUser(int newIndex){
            TurnUser newUser = users[newIndex];
            _user  = newUser;
            _index = newIndex;
            _init  = newUser.Initiative;
            _tbw   = newUser.Tbw;
            _id    = newUser.Id;
        }

        private void Awake() => _i = this;
    }
}


