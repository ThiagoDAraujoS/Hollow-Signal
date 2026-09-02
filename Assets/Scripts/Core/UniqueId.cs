using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Core{
    /// <summary>
    /// Provides a persistent, immutable, and globally unique identifier (UUID) for GameObjects.
    /// This identifier is generated strictly within the Unity Editor and serialized permanently, 
    /// acting as the database lookup key to bind local entities to the global <see cref="Blackboard"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class UniqueId : MonoBehaviour{
        [SerializeField]
        [HideInInspector] 
        private string uniqueId;

        /// <summary>
        /// Gets the immutable, unique string identifier assigned to this entity.
        /// </summary>
        public string Id => uniqueId;

#if UNITY_EDITOR
        /// <summary>
        /// Unity editor callback invoked when the script is loaded, added, or values are changed.
        /// Ensures that a new, valid GUID is instantly generated and serialized if the current field is empty.
        /// </summary>
        private void OnValidate(){
            if (!string.IsNullOrEmpty(uniqueId)) return;
            uniqueId = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}