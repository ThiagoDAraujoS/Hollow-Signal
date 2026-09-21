using System;
using System.Collections.Generic;
using Core.State;
using UnityEngine;

namespace World.Actors{
    [Serializable]
    public class VitalityPool : ITracked{
        [SerializeField] private string key;
        [SerializeField] private int current;
        [SerializeField] private int max;

        public event Action OnFull;
        public event Action OnDead;
        public event Action<int> OnHealed;
        public event Action<int> OnDamaged;
        public event Action<int, int> OnChanged;

        public string Key => key;
        public int Current => current;
        public int Max => max;
        public bool IsFull => current >= max;
        public bool IsDead => current <= 0;
        public float Normalized => max > 0 ? (float)current / max : 0f;

        public VitalityPool(string key, int max) => Initialize(key, max, max);

        public VitalityPool(string key, int current, int max) => Initialize(key, current, max);

        /// Sets the maximum capacity and clamps current value if exceeding new max.
        public void SetMax(int newMax){
            max = Mathf.Max(1, newMax);
            if (current > max)
                current = max;
            OnChanged?.Invoke(current, max);
            if (IsFull)
                OnFull?.Invoke();
        }

        /// Fully restores the pool to its maximum capacity.
        public void HealAll() => Heal(max - current);

        /// Restores an amount up to max capacity and fires appropriate events.
        public void Heal(int amount){
            if (amount <= 0 || current >= max) return;
            int applied = Mathf.Min(amount, max - current);
            current += applied;
            OnHealed?.Invoke(applied);
            OnChanged?.Invoke(current, max);
            if (IsFull)
                OnFull?.Invoke();
        }

        /// Removes an amount down to zero and fires appropriate events.
        public void Damage(int amount){
            if (amount <= 0 || current <= 0) return;
            int applied = Mathf.Min(amount, current);
            current -= applied;
            OnDamaged?.Invoke(applied);
            OnChanged?.Invoke(current, max);
            if (IsDead)
                OnDead?.Invoke();
        }

        /// Convenient alias to heal an amount.
        public void Add(int amount) => Heal(amount);

        /// Convenient alias to damage an amount.
        public void Remove(int amount) => Damage(amount);

        /// Directly overrides the current value clamped between zero and max.
        public void SetCurrent(int value){
            current = Mathf.Clamp(value, 0, max);
            OnChanged?.Invoke(current, max);
            if (IsDead)
                OnDead?.Invoke();
            if (IsFull)
                OnFull?.Invoke();
        }

        /// Serializes current and max values into the Blackboard partition.
        public void Save(Dictionary<string, object> state){
            state[key] = current;
            state[$"{key}_max"] = max;
        }

        /// Deserializes current and max values from the Blackboard partition.
        public void Load(Dictionary<string, object> state){
            if (state.TryGetValue(key, out object rawCurrent))
                current = Convert.ToInt32(rawCurrent);
            if (state.TryGetValue($"{key}_max", out object rawMax))
                max = Convert.ToInt32(rawMax);
        }

        /// Initializes key, current, and max values without firing mutation events.
        private void Initialize(string poolKey, int startingCurrent, int startingMax){
            key = poolKey;
            max = Mathf.Max(1, startingMax);
            current = Mathf.Clamp(startingCurrent, 0, max);
        }
    }
}
