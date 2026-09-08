using System;
using UnityEditor;

namespace Editor.Dialog{
    /// AssetPostprocessor that automatically watches for changes to .dialog files
    /// and invokes the DialogBaker upon asset import or save.
    public class DialogAssetPostprocessor : AssetPostprocessor{
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths){
            bool requiresRefresh = false;

            foreach (string assetPath in importedAssets){
                if (assetPath.EndsWith(".dialog", StringComparison.OrdinalIgnoreCase)){
                    if (DialogBaker.BakeFile(assetPath))
                        requiresRefresh = true;
                }
            }

            if (requiresRefresh)
                AssetDatabase.Refresh();
        }
    }
}
