using System.Collections;
using Core.TurnManagement;
using UnityEngine;

namespace Test{
    public class TurnSystemTest : MonoBehaviour{
        [HideInInspector]
        public TurnUser turnClock;

        private void Awake(){
            turnClock = GetComponent<TurnUser>();
        }

        public void LogOnEnterTurnSystem() => Debug.Log($"{name} Enter Turn Wheel");
        public void LogOnLeaveTurnSystem() => Debug.Log($"{name} Leave Turn Wheel");
        public void LogOnRoundEnd()        => Debug.Log($"{name} Round End");
        public void LogOnTurnEnd()         => Debug.Log($"{name} Turn End");
        public void LogOnTurnStart()       => Debug.Log($"{name} Turn Start");
        
        public void PassTurn(){
            StartCoroutine(PassTurnRoutine());
            return;

            IEnumerator PassTurnRoutine(){
                yield return new WaitForSeconds(2.0f);
                TurnWheel.PassTurn();
            }
        }
    }
}
