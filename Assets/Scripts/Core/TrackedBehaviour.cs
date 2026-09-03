using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Core{
    /// Base class for pure-data components that automatically registers
    /// and synchronizes all wrapped Tracked properties with the save system.
    [RequireComponent(typeof(BlackboardClient))]
    public abstract class TrackedBehaviour : MonoBehaviour, IBoundState{
        /// Caches all tracked properties initialized in subclasses.
        /// Initialized inline so no Awake() lifecycle coupling is needed.
        private readonly List<ITracked> _trackedProperties = new();

        /// Serializes all registered Tracked variables into the active blackboard partition.
        /// Overriding methods must call base.OnSaveState(state).
        public virtual void OnSaveState(Dictionary<string, object> state){
            if (state == null) return;
            foreach (ITracked tracked in _trackedProperties)
                tracked.Save(state);
        }

        /// Deserializes all registered Tracked variables from the active blackboard partition.
        /// Overriding methods must call base.OnLoadState(state).
        public virtual void OnLoadState(Dictionary<string, object> state){
            if (state == null) return;
            foreach (ITracked tracked in _trackedProperties)
                tracked.Load(state);
        }

        private void DiscoverTrackedProperties(){
            _trackedProperties.Clear();
            Type currentType = GetType();

            while (currentType != null && currentType != typeof(TrackedBehaviour)){
                FieldInfo[] fields = currentType.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
                );

                foreach (FieldInfo field in fields){
                    if (!typeof(ITracked).IsAssignableFrom(field.FieldType)) continue;
                    if (field.GetValue(this) is ITracked trackedInstance)
                        _trackedProperties.Add(trackedInstance);
                }

                currentType = currentType.BaseType;
            }
        }
        protected virtual void OnAwake(){}
        private void Awake() {
            DiscoverTrackedProperties();
            OnAwake();
        }
    }

    /// A non-generic contract allowing a collection of mixed Tracked variables 
    /// (ints, floats, strings, booleans, etc.) to be saved and loaded in bulk.
    public interface ITracked {
        string Key { get; }
        void   Save(Dictionary<string, object> state);
        void   Load(Dictionary<string, object> state);
    }

    /// A true RAII decorator. It holds a high-performance local memory backing value 
    /// during active gameplay and only synchronizes with the Blackboard partition 
    /// during explicit Load/Save hooks (OnLoadState / OnSaveState).
    [Serializable]
    public class Tracked<T> : ITracked{
        [SerializeField] private string key;

        [SerializeField] private T value;

        private readonly T _defaultValue;

        public string Key => key;

        /// Gets or sets the local gameplay value. Reads and writes are direct CPU memory 
        /// operations with zero dictionary lookup or boxing overhead during gameplay.
        public T Value{
            get => value;
            set => this.value = value;
        }

        /// Constructor that registers this property into a component's local list for automatic saving and loading.
        /// <param name="key">The case-insensitive Blackboard partition key.</param>
        /// <param name="defaultValue">The fallback value if the key does not exist in the save file.</param>
        public Tracked(string key, T defaultValue){
            this.key      = key;
            _defaultValue = defaultValue;
            value         = defaultValue;
        }

        /// Flushes the local gameplay value into the Blackboard partition (RAII Save/Flush phase).
        public void Save(Dictionary<string, object> state)=> state[key] = value;

        /// Restores the local gameplay value from the Blackboard partition (RAII Load/Acquire phase).
        public void Load(Dictionary<string, object> state){
            if (state == null){
                value = _defaultValue;
                return;
            }

            if (state.TryGetValue(key, out object rawVal)){
                if (rawVal is T directValue)
                    value = directValue;
                else{
                    try{
                        // Safely convert numerical types (e.g. double to float or long to int)
                        value = (T)Convert.ChangeType(rawVal, typeof(T));
                    }
                    catch{
                        Debug.LogWarning($"[Tracked] Type conversion failed for key '{key}'. Expected {typeof(T).Name}, got {rawVal?.GetType().Name}. Defaulting.");
                        value = _defaultValue;
                    }
                }
            }
            else
                value = _defaultValue;
        }

        /// Direct implicit cast to T, allowing clean reads without appending .Value
        /// (e.g., float currentHP = character.currentHP;)
        public static implicit operator T(Tracked<T> tracked) => tracked.Value ?? default(T);
        public override string ToString() => value?.ToString() ?? "null";
    }
}