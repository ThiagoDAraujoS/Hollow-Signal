#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Utilities;

namespace CRPG.Editor.Tools
{
    [CustomEditor(typeof(ParallaxFrustumFitter))]
    public class ParallaxFrustumFitterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ParallaxFrustumFitter fitter = (ParallaxFrustumFitter)target;

            EditorGUILayout.Space(12);

            GUI.backgroundColor = new Color(0.3f, 1.0f, 0.4f);
            if (GUILayout.Button("Fit All Parallax Planes to Frustum", GUILayout.Height(36)))
            {
                fitter.FitAllPlanes();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox(
                "How to use:\n" +
                "1. Move your quads along Z (e.g. Z = 5, 10, 25, 50, 100).\n" +
                "2. Click 'Fit All Parallax Planes to Frustum'.\n" +
                "Every plane will automatically scale to match the 1920x1080 camera view with pixel-perfect registration!",
                MessageType.Info);
        }
    }
}
#endif
