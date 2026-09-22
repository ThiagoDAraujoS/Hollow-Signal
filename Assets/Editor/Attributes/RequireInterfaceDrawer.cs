#if UNITY_EDITOR
using Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace CRPG.Editor.Attributes{
    /// Custom property drawer for RequireInterfaceAttribute that enforces interface implementation on assigned Object.
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public sealed class RequireInterfaceDrawer : PropertyDrawer{
        /// Draws the property field with interface type validation and component extraction.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            RequireInterfaceAttribute requireInterface = (RequireInterfaceAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            Object currentObject = property.objectReferenceValue;
            Object newObject = EditorGUI.ObjectField(position, label, currentObject, typeof(MonoBehaviour), true);

            if (EditorGUI.EndChangeCheck()){
                if (newObject == null)
                    property.objectReferenceValue = null;
                else if (requireInterface.InterfaceType.IsAssignableFrom(newObject.GetType()))
                    property.objectReferenceValue = newObject;
                else if (newObject is Component comp){
                    Component targetComp = comp.GetComponent(requireInterface.InterfaceType);
                    if (targetComp != null)
                        property.objectReferenceValue = targetComp;
                    else
                        Debug.LogError($"[RequireInterface] '{newObject.name}' does not implement {requireInterface.InterfaceType.Name}!");
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
