using UnityEditor;
using UnityEngine;

namespace Core.Board.Editor{
    [CustomEditor(typeof(ZoneShape))]
    public sealed class ZoneShapeEditor : UnityEditor.Editor {
    
        // Allows editing the handle in the Scene View
        private void OnSceneGUI() {
            ZoneShape script = (ZoneShape)target;

            EditorGUI.BeginChangeCheck();
        
            // Convert local offset to world position for the handle
            Vector3 worldPos = script.transform.position + script.centerOffset;
        
            // Draw the Position Handle
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);

            if (!EditorGUI.EndChangeCheck()) return;
            // Record Undo so Ctrl+Z works
            Undo.RecordObject(script, "Move Zone Center");
            
            // Convert back to local offset
            script.centerOffset = newWorldPos - script.transform.position;
            
            // Mark as dirty to save changes
            EditorUtility.SetDirty(script);
        }
    
        // Optional: Keeps your button
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
        
            ZoneShape script = (ZoneShape)target;
            if (!GUILayout.Button("Bake Geometry")) return;
            script.BakeGeometry();
            EditorUtility.SetDirty(script);
        }
    }
}