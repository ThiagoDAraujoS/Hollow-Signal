using Core.Board;
using UnityEngine.Events;

namespace Characters.Player{
    public class ActionController : Component{
        public Area currentArea;

        public UnityEvent onOutOfTurnResources;

        public void OnOutOfTurnResources(){
            if (Sheet.HasAction || Sheet.HasMove) return;
            onOutOfTurnResources?.Invoke();
            TurnUser.PassTurn();
        }

        public void RefreshTurnResources(){
            Sheet.ap.Restore();
            Sheet.mp.Restore();
        }
        public void Awake(){
            Sheet.ap.onPointZeroed.AddListener(OnOutOfTurnResources);
            Sheet.mp.onPointZeroed.AddListener(OnOutOfTurnResources);
            TurnUser.onTurnStart.AddListener(RefreshTurnResources);
            TurnUser.onEnterTurnSystem.AddListener(RefreshTurnResources);
        }

        public void OnDestroy(){
            Sheet.ap.onPointZeroed.RemoveListener(OnOutOfTurnResources);
            Sheet.mp.onPointZeroed.RemoveListener(OnOutOfTurnResources);
            TurnUser.onTurnStart.RemoveListener(RefreshTurnResources);
            TurnUser.onEnterTurnSystem.RemoveListener(RefreshTurnResources);
        }
    }
}
