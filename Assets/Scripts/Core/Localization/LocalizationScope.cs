using System.Collections.Generic;
using Core.Managers;
using UnityEngine;

namespace Core.Localization{
    /// RAII component that loads configured localization tables on Awake and unloads them on OnDestroy.
    [DisallowMultipleComponent]
    public class LocalizationScope : MonoBehaviour{
        [SerializeField]
        public List<string> tableNames = new();

        private void Awake() => LoadAll();

        private void OnDestroy() => UnloadAll();

        public void LoadAll(){
            foreach (string table in tableNames)
                LocalizationManager.LoadTable(table);
        }

        public void UnloadAll(){
            foreach (string table in tableNames)
                LocalizationManager.UnloadTable(table);
        }
    }
}
