using System;
using UnityEngine;

namespace Core{
    [DisallowMultipleComponent]
    public class UniqueId : MonoBehaviour{
        [SerializeField]
        [HideInInspector] 
        private string uniqueId;
        public string Id => uniqueId;


#if UNITY_EDITOR
        /// OnValidate is called in the editor when the script is loaded or values change
        private void OnValidate(){
            if (!string.IsNullOrEmpty(uniqueId)) return;
            uniqueId = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}