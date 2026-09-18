#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.Utilities{
    public static class DieselPunkMaterialSetup{
        private const string MaterialsPath = "Assets/Art/Enviroment/Kitbash/DieselPunk/Materials";
        private const string TexturesPath  = "Assets/Art/Enviroment/Kitbash/DieselPunk/Textures";
        private const string ShaderName    = "HollowSignal/Environment/SimpleLitPackedAO";

        /// Configures DieselPunk kitbash materials to use the custom packed ambient occlusion shader and textures.
        [MenuItem("Tools/Hollow Signal/Re-link DieselPunk Materials")]
        public static void SetupMaterials(){
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
                throw new InvalidOperationException($"Shader '{ShaderName}' not found. Ensure the shader file compiles without errors.");

            string[] matGuids     = AssetDatabase.FindAssets("t:Material", new[]{ MaterialsPath });
            int      updatedCount = 0;

            foreach (string guid in matGuids){
                string   path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string    matName         = Path.GetFileNameWithoutExtension(path);
                string    expectedTexPath = $"{TexturesPath}/{matName}_basecolor.png";
                Texture2D tex             = AssetDatabase.LoadAssetAtPath<Texture2D>(expectedTexPath);

                mat.shader = shader;

                if (tex != null)
                    mat.SetTexture("_BaseMap", tex);

                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Saturation", 1.0f);
                mat.SetFloat("_OcclusionStrength", 1.0f);
                mat.SetFloat("_OcclusionPower", 1.0f);
                mat.SetFloat("_OcclusionContrast", 1.0f);
                mat.SetColor("_OcclusionTint", new Color(0.2f, 0.22f, 0.25f, 1.0f));
                mat.SetFloat("_DirectOcclusion", 0.35f);

                mat.SetFloat("_EnableHeightGrad", 0.0f);
                mat.SetFloat("_HeightGradMinY", 0.0f);
                mat.SetFloat("_HeightGradMaxY", 15.0f);
                mat.SetColor("_HeightGradColor", new Color(0.4f, 0.4f, 0.45f, 1.0f));
                mat.SetFloat("_HeightGradStrength", 0.5f);

                mat.SetFloat("_EnableShadowSharpness", 0.0f);
                mat.SetFloat("_ShadowThreshold", 0.25f);
                mat.SetFloat("_ShadowSoftness", 0.15f);

                mat.SetFloat("_AmbientBoost", 1.0f);

                EditorUtility.SetDirty(mat);
                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DieselPunkMaterialSetup] Successfully configured {updatedCount} materials with '{ShaderName}'.");
        }
    }
}
#endif
