using UnityEditor;
using UnityEngine;
using UI.TurnTable;

namespace Editor.Utilities{
    public static class TurnTableLightCurveRetargeter{
        [MenuItem("Tools/Retarget TurnTable Light Curves")]
        public static void Retarget(){
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/UI/TurnTable/Animations" });
            int retargetedCount = 0;

            foreach (string guid in guids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                bool modified = false;

                foreach (var binding in bindings){
                    if (binding.type != typeof(Light) || binding.propertyName != "m_Intensity") continue;

                    string targetProp = null;
                    if (binding.path.EndsWith("CenterLight")) targetProp = "centerBrightness";
                    else if (binding.path.EndsWith("AllyLight")) targetProp = "allyBrightness";
                    else if (binding.path.EndsWith("EnemyLight")) targetProp = "enemyBrightness";

                    if (targetProp == null) continue;

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    var newBinding = EditorCurveBinding.FloatCurve("", typeof(TurnTableController), targetProp);

                    AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    modified = true;
                    retargetedCount++;
                    Debug.Log($"[Retargeter] {clip.name}: Moved '{binding.path}.{binding.propertyName}' -> 'TurnTableController.{targetProp}' ({curve.length} keys)");
                }

                if (modified)
                    EditorUtility.SetDirty(clip);
            }

            if (retargetedCount > 0){
                AssetDatabase.SaveAssets();
                Debug.Log($"[Retargeter] Successfully retargeted {retargetedCount} light curves across TurnTable animations!");
            }
        }
    }
}
