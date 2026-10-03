using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace World.Interactables{
    /// Preset semantic highlight color categories.
    public enum HighlightCategory{
        Custom,
        NonHostile,
        Hostile,
        Usable,
        Door,
        Pickup,
        Neutral
    }
}

/// Draws objects on highlight layers into an offscreen mask and composites a screen-space outline.
[DisallowMultipleRendererFeature("InteractableOutlineFeature")]
public class InteractableOutlineFeature : ScriptableRendererFeature{
    [System.Serializable]
    public class PaletteSettings{
        public Color nonHostileColor = new(1f, 0.9f, 0.2f, 1f);
        public Color hostileColor    = new(1f, 0.2f, 0.2f, 1f);
        public Color usableColor     = new(0.2f, 0.9f, 0.4f, 1f);
        public Color doorColor       = new(0.75f, 0.35f, 1f, 1f);
        public Color pickupColor     = new(0f, 0.85f, 1f, 1f);
        public Color neutralColor    = new(0.85f, 0.85f, 0.85f, 1f);
    }

    [System.Serializable]
    public class Settings{
        [Tooltip("Layers to include in outline mask. Defaults to Highlight (14).")]
        public LayerMask targetLayer = 1 << 14;
        public RenderPassEvent compositePassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader maskShader;
        public Shader outlineShader;
        [Range(1f, 6f)] public float outlineThickness = 2f;
        [Range(0f, 1f)] public float outlineIntensity = 1f;
        [Range(0f, 1f)] public float innerFillOpacity;
        [Tooltip("If true, occluded parts behind walls and geometry will not be outlined.")]
        public bool occludeBehindWalls = true;
        public PaletteSettings palette = new();
    }

    [SerializeField] private Settings settings = new();
    private static InteractableOutlineFeature _instance;
    private HighlightOutlinePass _outlinePass;
    private Material _maskMaterial;
    private Material _compositeMaterial;

    /// Resolves color corresponding to semantic category from active renderer feature settings.
    public static Color GetCategoryColor(World.Interactables.HighlightCategory cat) => (_instance != null ? _instance.settings.palette : null) switch{
        null => cat switch{
            World.Interactables.HighlightCategory.Hostile    => new Color(1f, 0.2f, 0.2f, 1f),
            World.Interactables.HighlightCategory.Usable     => new Color(0.2f, 0.9f, 0.4f, 1f),
            World.Interactables.HighlightCategory.Door       => new Color(0.75f, 0.35f, 1f, 1f),
            World.Interactables.HighlightCategory.Pickup     => new Color(0f, 0.85f, 1f, 1f),
            World.Interactables.HighlightCategory.Neutral    => new Color(0.85f, 0.85f, 0.85f, 1f),
            _                                                => new Color(1f, 0.9f, 0.2f, 1f)
        },
        var p => cat switch{
            World.Interactables.HighlightCategory.Hostile    => p.hostileColor,
            World.Interactables.HighlightCategory.Usable     => p.usableColor,
            World.Interactables.HighlightCategory.Door       => p.doorColor,
            World.Interactables.HighlightCategory.Pickup     => p.pickupColor,
            World.Interactables.HighlightCategory.Neutral    => p.neutralColor,
            _                                                => p.nonHostileColor
        }
    };

    /// Initializes shaders, materials, and the outline render pass.
    public override void Create(){
        _instance = this;
        EnsureMaterials();
        _outlinePass = new HighlightOutlinePass(settings, _maskMaterial, _compositeMaterial);
    }

    /// Resolves required shaders and engine material instances.
    private void EnsureMaterials(){
        if (!settings.maskShader) settings.maskShader = Shader.Find("Hidden/Retro/HighlightMask");
        if (!settings.outlineShader) settings.outlineShader = Shader.Find("Hidden/Retro/InteractableOutline");
        if (!_maskMaterial) _maskMaterial = CoreUtils.CreateEngineMaterial(settings.maskShader);
        if (!_compositeMaterial) _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.outlineShader);
    }

    /// Enqueues the outline pass for game and scene cameras.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData){
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection) return;
        _instance = this;
        EnsureMaterials();
        if (_outlinePass == null) _outlinePass = new HighlightOutlinePass(settings, _maskMaterial, _compositeMaterial);
        renderer.EnqueuePass(_outlinePass);
    }

    /// Releases engine material instances upon renderer feature disposal.
    protected override void Dispose(bool disposing){
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_compositeMaterial);
        _maskMaterial = null;
        _compositeMaterial = null;
        if (_instance == this) _instance = null;
    }

    private class HighlightOutlinePass : ScriptableRenderPass{
        private static readonly int MASK_TEXTURE_ID = Shader.PropertyToID("_HighlightMaskTexture");
        private static readonly int THICKNESS_ID    = Shader.PropertyToID("_OutlineThickness");
        private static readonly int INTENSITY_ID    = Shader.PropertyToID("_OutlineIntensity");
        private static readonly int FILL_ID         = Shader.PropertyToID("_InnerFillOpacity");

        private readonly Settings _settings;
        private readonly Material _maskMaterial;
        private readonly Material _compositeMaterial;
        private readonly List<ShaderTagId> _shaderTagIds = new();

        private class MaskPassData{
            public RendererListHandle rendererListHandle;
        }

        private class CompositePassData{
            public Material compositeMaterial;
            public TextureHandle source;
            public TextureHandle mask;
        }

        /// Configures render pass event, intermediate texture requirement, and forward & unlit shader tags.
        public HighlightOutlinePass(Settings settings, Material maskMaterial, Material compositeMaterial){
            _settings          = settings;
            _maskMaterial      = maskMaterial;
            _compositeMaterial = compositeMaterial;
            renderPassEvent    = settings.compositePassEvent;
            requiresIntermediateTexture = true;

            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
        }

        /// Records mask generation and composite raster passes into the URP RenderGraph.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData){
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData       = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData         = frameData.Get<UniversalLightData>();
            UniversalResourceData resourcesData  = frameData.Get<UniversalResourceData>();

            if (!resourcesData.cameraColor.IsValid() || resourcesData.isActiveTargetBackBuffer) return;

            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var filterSettings = new FilteringSettings(RenderQueueRange.all, _settings.targetLayer);
            var drawSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, renderingData, cameraData, lightData, sortFlags);

            drawSettings.overrideMaterial = _maskMaterial;
            drawSettings.overrideMaterialPassIndex = 0;

            var listParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
            RendererListHandle rendererList = renderGraph.CreateRendererList(listParams);

            TextureDesc maskDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
            maskDesc.name            = "_HighlightMaskTexture";
            maskDesc.colorFormat     = GraphicsFormat.R8G8B8A8_UNorm;
            maskDesc.depthBufferBits = 0;
            maskDesc.clearBuffer     = true;
            maskDesc.clearColor      = Color.clear;

            bool canOcclude = _settings.occludeBehindWalls && resourcesData.cameraDepthTexture.IsValid();
            if (canOcclude)
                maskDesc.msaaSamples = renderGraph.GetTextureDesc(resourcesData.cameraDepthTexture).msaaSamples;

            TextureHandle maskTexture = renderGraph.CreateTexture(maskDesc);

            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("HighlightSilhouetteMask", out var passData, profilingSampler)){
                passData.rendererListHandle = rendererList;
                builder.UseRendererList(rendererList);
                builder.SetRenderAttachment(maskTexture, 0, AccessFlags.Write);

                if (canOcclude)
                    builder.SetRenderAttachmentDepth(resourcesData.cameraDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) => context.cmd.DrawRendererList(data.rendererListHandle));
            }

            _compositeMaterial.SetFloat(THICKNESS_ID, _settings.outlineThickness);
            _compositeMaterial.SetFloat(INTENSITY_ID, _settings.outlineIntensity);
            _compositeMaterial.SetFloat(FILL_ID, _settings.innerFillOpacity);

            TextureHandle source = resourcesData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name        = "_CameraColor-InteractableOutline";
            destinationDesc.clearBuffer = false;
            TextureHandle destination   = renderGraph.CreateTexture(destinationDesc);

            using (var blitBuilder = renderGraph.AddRasterRenderPass<CompositePassData>("InteractableOutlineComposite", out var passData, profilingSampler)){
                passData.compositeMaterial = _compositeMaterial;
                passData.source            = source;
                passData.mask              = maskTexture;

                blitBuilder.UseTexture(source, AccessFlags.Read);
                blitBuilder.UseTexture(maskTexture, AccessFlags.Read);
                blitBuilder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                blitBuilder.SetRenderFunc((CompositePassData data, RasterGraphContext context) => {
                    data.compositeMaterial.SetTexture(MASK_TEXTURE_ID, data.mask);
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.compositeMaterial, 0);
                });
            }

            resourcesData.cameraColor = destination;
        }
    }
}
