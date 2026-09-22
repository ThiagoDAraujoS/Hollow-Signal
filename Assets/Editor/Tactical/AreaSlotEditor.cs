#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using World.Tactical;

namespace CRPG.Editor.Tactical{
    /// Custom editor providing in-scene interactive handles for AreaSlot position and facing direction.
    [CustomEditor(typeof(AreaSlot), true)]
    [CanEditMultipleObjects]
    public class AreaSlotEditor : UnityEditor.Editor{
        private static readonly Color ExplorationColor = new(0.25f, 0.95f, 0.45f);
        private static readonly Color TacticalColor    = new(0.2f, 0.8f, 1.0f);

        /// Hides default move gizmos when selecting slot.
        private void OnEnable() => UnityEditor.Tools.hidden = true;

        /// Restores default transform tools when deselecting slot.
        private void OnDisable() => UnityEditor.Tools.hidden = false;

        /// Renders interactive handles at the slot standing spot in the Scene view based on active tool.
        public virtual void OnSceneGUI(){
            AreaSlot slot = (AreaSlot)target;
            Vector3 worldPos = slot.Position;
            Quaternion worldRot = slot.Rotation;

            Color slotColor = slot is TacticalSlot ? TacticalColor : ExplorationColor;
            Handles.color = slotColor;

            Handles.DrawWireDisc(worldPos, Vector3.up, 0.35f);
            Handles.DrawLine(worldPos, worldPos + (worldRot * Vector3.forward) * 0.75f);
            Handles.ConeHandleCap(0, worldPos + (worldRot * Vector3.forward) * 0.75f, worldRot, 0.15f, EventType.Repaint);

            EditorGUI.BeginChangeCheck();

            Vector3 newWorldPos = worldPos;
            Quaternion newWorldRot = worldRot;

            if (UnityEditor.Tools.current == Tool.Rotate){
                newWorldRot = Handles.RotationHandle(worldRot, worldPos);
                Vector3 fwd = newWorldRot * Vector3.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f)
                    newWorldRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            }
            else{
                Quaternion handleRot = UnityEditor.Tools.pivotRotation == PivotRotation.Local ? worldRot : Quaternion.identity;
                newWorldPos = Handles.PositionHandle(worldPos, handleRot);
                if (NavMesh.SamplePosition(newWorldPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    newWorldPos = hit.position;
            }

            if (!EditorGUI.EndChangeCheck()) return;

            Undo.RecordObject(slot, "Move Slot Pose");
            slot.SetAnchorPoseFromWorld(newWorldPos, newWorldRot);
            EditorUtility.SetDirty(slot);
        }

        /// Extends the default inspector with quick setup buttons.
        public override void OnInspectorGUI(){
            DrawDefaultInspector();

            AreaSlot slot = (AreaSlot)target;
            EditorGUILayout.Space(6);
            if (GUILayout.Button("Reset Spot In Front of Object", GUILayout.Height(24))){
                Undo.RecordObject(slot, "Reset Slot Pose");
                Vector3 defaultPos = slot.transform.TransformPoint(SpatialPose.Default.localPosition);
                Quaternion defaultRot = slot.transform.rotation * Quaternion.Euler(SpatialPose.Default.localEulerAngles);
                if (NavMesh.SamplePosition(defaultPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    defaultPos = hit.position;
                slot.SetAnchorPoseFromWorld(defaultPos, defaultRot);
                EditorUtility.SetDirty(slot);
            }

            if (GUILayout.Button("Snap Spot To NavMesh", GUILayout.Height(24))){
                Undo.RecordObject(slot, "Snap Slot To NavMesh");
                slot.SnapToNavMesh();
                EditorUtility.SetDirty(slot);
            }
        }
    }

    [CustomEditor(typeof(TacticalSlot))]
    [CanEditMultipleObjects]
    public class TacticalSlotEditor : AreaSlotEditor{}

    [CustomEditor(typeof(ExplorationSlot))]
    [CanEditMultipleObjects]
    public class ExplorationSlotEditor : AreaSlotEditor{}
}
#endif
