using System;
using UnityEngine;
using UnityEngine.Events;

namespace Core.TurnManagement{
    public class TurnUser : MonoBehaviour, IComparable<TurnUser>{
        [SerializeField] private UnityEvent onInitiativeChanged;
        
        [SerializeField] private int initiative        = 5;
        [SerializeField] private int tieBreakingWeight = 5;

        public Guid Id{ get; private set; }
        public int Tbw{ get; private set; }
        public int Initiative{
            get => initiative;
            set{
                if (initiative == value) return;
                initiative = value;
                TurnWheel.RecalculateTurnElements();
            }
        }
        private void Awake() => Id = Guid.NewGuid();
        public int CompareTo(int init, int tbw, Guid id){
            // 1. Compare Initiative Descending. We use other.CompareTo(this) to sort from high-to-low.
            int compare = init.CompareTo(Initiative);
            if (compare != 0)
                return compare;

            // 2. Compare TieBreakingWeight Descending. Initiatives are tied, so we check the tie-breaker, also high-to-low.
            compare = tbw.CompareTo(tieBreakingWeight);
            if (compare != 0)
                return compare;

            // 3. Compare GUID Ascending. Both are tied. Use the GUID for a final, stable sort order.
            return Id.CompareTo(id);
        }
        
        public int CompareTo(TurnUser other) => other == null ? 1 : CompareTo(other.Initiative, other.tieBreakingWeight, other.Id);
    }
}
