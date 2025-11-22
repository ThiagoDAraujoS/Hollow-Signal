using System;
using UnityEngine;
using UnityEngine.Events;

namespace Utils{
    [Serializable]
    public class Resource{
        [SerializeField] private int value;
        [SerializeField] private int limit;

        public UnityEvent
            onValueAdded,
            onValueRemoved,
            onValueZeroed,
            onValueCapped,
            onLimitIncreased,
            onLimitDecreased;

        public int Value{
            get => value;
            set{
                if (value == this.value)
                    return;

                int oldValue = this.value;
                int newValue = Mathf.Clamp(value, 0, limit);

                this.value = newValue;
                if (newValue > oldValue)
                    onValueAdded?.Invoke();
                else if (newValue < oldValue)
                    onValueRemoved?.Invoke();
                
                if (newValue == 0)
                    onValueZeroed?.Invoke();
                else if (newValue == limit)
                    onValueCapped?.Invoke();
            }
        }

        public int Limit{
            get => limit;
            set{
                if (value == limit)
                    return;

                int oldCap = limit;
                int newCap = Mathf.Max(0, value);

                limit = newCap;
                if (newCap > oldCap)
                    onLimitIncreased?.Invoke();
                else if (newCap < oldCap)
                    onLimitDecreased?.Invoke();

                int clamped = Mathf.Clamp(this.value, 0, limit);
                if (clamped != this.value)
                    Value = clamped;
            }
        }

        public void Restore() => Value = Limit;
        
        public static implicit operator int(Resource p) => p.Value;
    }
}