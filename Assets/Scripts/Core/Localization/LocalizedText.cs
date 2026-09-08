using Core.Managers;
using TMPro;
using UnityEngine;

namespace Core.Localization{
    /// Lightweight UI component that automatically updates a TextMeshProUGUI element with localized text.
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour{
        [Tooltip("The localization table (e.g. 'system', 'items', 'masteries') containing the key.")]
        [SerializeField] private string tableName = "system";

        [Tooltip("The localization key used to retrieve translated text from the database.")]
        [SerializeField] private string localizationKey;

        private TextMeshProUGUI _textComponent;

        private void Awake() => _textComponent = GetComponent<TextMeshProUGUI>();

        private void OnEnable(){
            LocalizationManager.OnLanguageChanged += RefreshText;
            RefreshText();
        }

        private void OnDisable() => LocalizationManager.OnLanguageChanged -= RefreshText;

        /// Updates the attached text component with the current translation of the localization key.
        public void RefreshText(){
            if (_textComponent == null || string.IsNullOrEmpty(localizationKey)) return;
            _textComponent.text = LocalizationManager.Get(tableName, localizationKey);
        }

        /// Optionally updates the key dynamically at runtime and refreshes.
        public void SetKey(string newKey){
            localizationKey = newKey;
            RefreshText();
        }

        /// Optionally updates both table and key dynamically at runtime and refreshes.
        public void SetTableAndKey(string newTable, string newKey){
            tableName = newTable;
            localizationKey = newKey;
            RefreshText();
        }
    }
}
