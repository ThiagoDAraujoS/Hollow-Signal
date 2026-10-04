using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Renderers
{
    /// <summary>
    /// VolumeComponent allowing per-volume / per-scene control of Tilt-Shift depth-of-field parameters.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Tilt Shift")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class TiltShift : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Master blend intensity of the tilt-shift effect.")]
        public ClampedFloatParameter intensity = new(0f, 0f, 1f);

        [Tooltip("Normalized screen Y position of the sharp focus center (0 = bottom, 0.5 = middle, 1 = top).")]
        public ClampedFloatParameter focusCenter = new(0.5f, 0f, 1f);

        [Tooltip("Normalized height of the 100% sharp in-focus region.")]
        public ClampedFloatParameter focusRange = new(0.2f, 0f, 1f);

        [Tooltip("Smoothness of transition from sharp to blurred regions.")]
        public ClampedFloatParameter feather = new(0.3f, 0.01f, 1f);

        [Tooltip("Tilt angle of the focus line in degrees (-90 to 90).")]
        public ClampedFloatParameter angle = new(0f, -90f, 90f);

        [Tooltip("Maximum pixel radius of the out-of-focus bokeh blur.")]
        public ClampedFloatParameter maxBlurRadius = new(8f, 0f, 30f);

        [Tooltip("Non-linear optical falloff exponent for defocus.")]
        public ClampedFloatParameter blurCurve = new(2.0f, 0.5f, 4f);

        [Tooltip("Subtle optical chromatic aberration in out-of-focus areas.")]
        public ClampedFloatParameter chromaticAberration = new(0.4f, 0f, 2f);

        [Tooltip("Miniature/toy saturation enhancement in out-of-focus areas.")]
        public ClampedFloatParameter saturationBoost = new(0.2f, -0.5f, 1f);

        [Tooltip("Luminance threshold above which highlights flare into circular bokeh.")]
        public ClampedFloatParameter bokehThreshold = new(0.7f, 0f, 2f);

        [Tooltip("Intensity multiplier for bokeh highlight flare.")]
        public ClampedFloatParameter bokehIntensity = new(0.8f, 0f, 3f);

        [Tooltip("Debug visualizer: overlays green on sharp area and red on blurred areas.")]
        public BoolParameter previewFocus = new(false);

        public bool IsActive() => intensity.value > 0f || previewFocus.value;
        public bool IsTileCompatible() => false;
    }

    /// <summary>
    /// Universal Render Pipeline ScriptableRendererFeature providing miniature tilt-shift post-processing.
    /// Can be driven either directly via feature Inspector settings or dynamically via Unity Volume profiles.
    /// </summary>
    [DisallowMultipleRendererFeature("TiltShiftFeature")]
    public class TiltShiftFeature : ScriptableRendererFeature
    {
        public enum SettingsMode
        {
            [Tooltip("Uses Volume stack if present & active; otherwise falls back to Feature settings.")]
            AutoBlend,
            [Tooltip("Strictly uses Volume component from active Volume Profile.")]
            VolumeOnly,
            [Tooltip("Strictly uses Feature settings defined in this Renderer inspector.")]
            FeatureOverrideOnly
        }

        [Serializable]
        public class Settings
        {
            public SettingsMode mode = SettingsMode.AutoBlend;
            public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public Shader tiltShiftShader;

            [Header("Default / Fallback Parameters")]
            [Range(0f, 1f)] public float intensity = 1f;
            [Range(0f, 1f)] public float focusCenter = 0.5f;
            [Range(0f, 1f)] public float focusRange = 0.2f;
            [Range(0.01f, 1f)] public float feather = 0.3f;
            [Range(-90f, 90f)] public float angle;
            [Range(0f, 30f)] public float maxBlurRadius = 8f;
            [Range(0.5f, 4f)] public float blurCurve = 2f;
            [Range(0f, 2f)] public float chromaticAberration = 0.4f;
            [Range(-0.5f, 1f)] public float saturationBoost = 0.2f;
            [Range(0f, 2f)] public float bokehThreshold = 0.7f;
            [Range(0f, 3f)] public float bokehIntensity = 0.8f;
            public bool previewFocus;
        }

        [SerializeField] private Settings settings = new();
        private Material _material;
        private TiltShiftPass _tiltShiftPass;

        public override void Create()
        {
            EnsureMaterial();
            _tiltShiftPass = new TiltShiftPass(settings, _material);
        }

        private void EnsureMaterial()
        {
            if (!settings.tiltShiftShader)
                settings.tiltShiftShader = Shader.Find("Hidden/PostProcess/TiltShift");

            if (!_material && settings.tiltShiftShader)
                _material = CoreUtils.CreateEngineMaterial(settings.tiltShiftShader);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType is CameraType.Preview or CameraType.Reflection)
                return;

            EnsureMaterial();
            if (_material == null) return;

            _tiltShiftPass ??= new TiltShiftPass(settings, _material);
            renderer.EnqueuePass(_tiltShiftPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private class TiltShiftPass : ScriptableRenderPass
        {
            private static readonly int PropIntensity        = Shader.PropertyToID("_Intensity");
            private static readonly int PropFocusCenter     = Shader.PropertyToID("_FocusCenter");
            private static readonly int PropFocusRange      = Shader.PropertyToID("_FocusRange");
            private static readonly int PropFeather          = Shader.PropertyToID("_Feather");
            private static readonly int PropAngle            = Shader.PropertyToID("_Angle");
            private static readonly int PropMaxBlurRadius  = Shader.PropertyToID("_MaxBlurRadius");
            private static readonly int PropBlurCurve       = Shader.PropertyToID("_BlurCurve");
            private static readonly int PropChromaticAberr  = Shader.PropertyToID("_ChromaticAberration");
            private static readonly int PropSaturationBoost = Shader.PropertyToID("_SaturationBoost");
            private static readonly int PropBokehThreshold  = Shader.PropertyToID("_BokehThreshold");
            private static readonly int PropBokehIntensity  = Shader.PropertyToID("_BokehIntensity");
            private static readonly int PropPreviewFocus    = Shader.PropertyToID("_PreviewFocus");

            private readonly Settings _settings;
            private readonly Material _material;

            private class PassData
            {
                public Material material;
                public TextureHandle source;
            }

            public TiltShiftPass(Settings settings, Material material)
            {
                _settings = settings;
                _material = material;
                renderPassEvent = settings.passEvent;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                if (!resourcesData.cameraColor.IsValid() || resourcesData.isActiveTargetBackBuffer)
                    return;

                // Resolve parameters from Volume stack or Feature defaults
                float intensity = _settings.intensity;
                float focusCenter = _settings.focusCenter;
                float focusRange = _settings.focusRange;
                float feather = _settings.feather;
                float angle = _settings.angle;
                float maxBlurRadius = _settings.maxBlurRadius;
                float blurCurve = _settings.blurCurve;
                float chromaticAberration = _settings.chromaticAberration;
                float saturationBoost = _settings.saturationBoost;
                float bokehThreshold = _settings.bokehThreshold;
                float bokehIntensity = _settings.bokehIntensity;
                bool previewFocus = _settings.previewFocus;

                bool isEffectActive = intensity > 0.001f || previewFocus;

                if (_settings.mode != SettingsMode.FeatureOverrideOnly)
                {
                    VolumeStack stack = VolumeManager.instance.stack;
                    TiltShift volume = stack?.GetComponent<TiltShift>();

                    if (volume != null && volume.IsActive())
                    {
                        intensity = volume.intensity.value;
                        focusCenter = volume.focusCenter.value;
                        focusRange = volume.focusRange.value;
                        feather = volume.feather.value;
                        angle = volume.angle.value;
                        maxBlurRadius = volume.maxBlurRadius.value;
                        blurCurve = volume.blurCurve.value;
                        chromaticAberration = volume.chromaticAberration.value;
                        saturationBoost = volume.saturationBoost.value;
                        bokehThreshold = volume.bokehThreshold.value;
                        bokehIntensity = volume.bokehIntensity.value;
                        previewFocus = volume.previewFocus.value;
                        isEffectActive = true;
                    }
                    else if (_settings.mode == SettingsMode.VolumeOnly)
                    {
                        isEffectActive = false;
                    }
                }

                if (!isEffectActive)
                    return;

                // Upload resolved properties to material
                _material.SetFloat(PropIntensity, intensity);
                _material.SetFloat(PropFocusCenter, focusCenter);
                _material.SetFloat(PropFocusRange, focusRange);
                _material.SetFloat(PropFeather, feather);
                _material.SetFloat(PropAngle, angle);
                _material.SetFloat(PropMaxBlurRadius, maxBlurRadius);
                _material.SetFloat(PropBlurCurve, blurCurve);
                _material.SetFloat(PropChromaticAberr, chromaticAberration);
                _material.SetFloat(PropSaturationBoost, saturationBoost);
                _material.SetFloat(PropBokehThreshold, bokehThreshold);
                _material.SetFloat(PropBokehIntensity, bokehIntensity);
                _material.SetFloat(PropPreviewFocus, previewFocus ? 1f : 0f);

                TextureHandle source = resourcesData.activeColorTexture;
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "_CameraColor-TiltShift";
                destinationDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("TiltShiftPass", out var passData, profilingSampler))
                {
                    passData.material = _material;
                    passData.source = source;

                    builder.UseTexture(source);
                    builder.SetRenderAttachment(destination, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                    });
                }

                resourcesData.cameraColor = destination;
            }
        }
    }
}
