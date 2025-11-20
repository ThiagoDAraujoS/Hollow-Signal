using System;
using UnityEngine;
using UnityEngine.Events;

namespace Utils{
    [Serializable]
    public class Resource{
        [SerializeField] private int value;
        [SerializeField] private int capacity;
        
        public UnityEvent 
            onPointAdded,
            onPointRemoved,
            onPointZeroed,
            onPointCapped,
            onCapacityIncreased,
            onCapacityDecreased;
        
        public int Value{
            get => value;
            set{
                if (value == this.value)
                    return;

                int oldValue = this.value;
                int newValue = Mathf.Clamp(value, 0, capacity);

                this.value = newValue;
                if (newValue > oldValue)
                    onPointAdded?.Invoke();
                else if (newValue < oldValue)
                    onPointRemoved?.Invoke();
                if (newValue == 0)
                    onPointZeroed?.Invoke();
                else if (newValue == capacity)
                    onPointCapped?.Invoke();
            }
        }
        public int Capacity{
            get => capacity;
            set{
                if (value == capacity)
                    return;

                int oldCap = capacity;
                int newCap = Mathf.Max(0, value);
                
                capacity = newCap;
                if (newCap > oldCap)
                    onCapacityIncreased?.Invoke();
                else if (newCap < oldCap)
                    onCapacityDecreased?.Invoke();
                
                int clamped = Mathf.Clamp(this.value, 0, capacity);
                if (clamped != this.value)
                    Value = clamped;
            }
        }
        public void Restore() => Value = Capacity;
        public static implicit operator int(Resource p) => p.Value;
    }
}