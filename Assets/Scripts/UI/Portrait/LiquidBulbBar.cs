using UnityEngine;

namespace UI.Portrait
{
    public class LiquidBulbBar : MonoBehaviour
    {
        [SerializeField] private MeshRenderer barRenderer;

        private MaterialPropertyBlock _propertyBlock;
        private static readonly int FillPropertyId = Shader.PropertyToID("_FillAmount");
        private static readonly int LiquidColorPropertyId = Shader.PropertyToID("_LiquidColor");
        private static readonly int SurfaceColorPropertyId = Shader.PropertyToID("_SurfaceColor");

        /// Initializes the property block cache.
        private void Awake() => _propertyBlock = new MaterialPropertyBlock();

        /// Sets the normalized liquid fill level between 0 and 1.
        public void SetFill(float normalizedFill)
        {
            barRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(FillPropertyId, Mathf.Clamp01(normalizedFill));
            barRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// Updates the HDR colors of the liquid and its surface meniscus.
        public void SetColors(Color liquidColor, Color meniscusColor)
        {
            barRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(LiquidColorPropertyId, liquidColor);
            _propertyBlock.SetColor(SurfaceColorPropertyId, meniscusColor);
            barRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
