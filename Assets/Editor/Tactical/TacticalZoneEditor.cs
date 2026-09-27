#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using World.Tactical;

namespace CRPG.Editor.Tactical{
    /// Custom editor and tool menu items for cooking and visualizing tactical zones and slots.
    [CustomEditor(typeof(TacticalZone))]
    [CanEditMultipleObjects]
    public class TacticalZoneEditor : UnityEditor.Editor{
        /// Adds menu command to cook tactical zones and slots across active scene.
        [MenuItem("Tools/CRPG/Cook Tactical Zones & Slots")]
        [MenuItem("Tools/Tactical/Cook Tactical Zones & Slots")]
        public static void CookAllZonesMenu() => TacticalZone.CookAllZones();

        /// Draws default inspector plus action buttons for quick scene setup.
        public override void OnInspectorGUI(){
            DrawDefaultInspector();

            TacticalZone zone = (TacticalZone)target;
            EditorGUILayout.Space(8);

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("Cook Tactical Zones & Slots", GUILayout.Height(30))){
                TacticalZone.CookAllZones();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Toggle Voronoi Gizmos", GUILayout.Height(24))){
                zone.ToggleVoronoiGizmos();
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
