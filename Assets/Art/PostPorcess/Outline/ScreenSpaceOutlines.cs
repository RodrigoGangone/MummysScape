using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    [System.Serializable]
    public class ViewSpaceNormalsTextureSettings
    {
        public RenderTextureFormat colorFormat = RenderTextureFormat.ARGB32;
        public FilterMode filterMode = FilterMode.Point;
    }

    [System.Serializable]
    public class OutlineSettings
    {
        [Range(0.5f, 5f)] public float thickness = 1.0f;
        [Range(0.0f, 1f)] public float blend = 1.0f;
        [Range(0.0f, 3f)] public float depthThreshold = 1.0f;
        [Range(0.0f, 1f)] public float depthSmoothWidth = 0.05f;
        [Range(0.0f, 2f)] public float robertsCrossMultiplier = 1.0f;
        [Range(0.0f, 2f)] public float depthAttenuation = 0.0f;
        [Range(0.0f, 4f)] public float depthRelativeScale = 0.0f;
        [Range(0.0f, 2f)] public float normalsThreshold = 0.2f;
        [Range(0.0f, 4f)] public float normalSensitivity = 1.0f;
        public bool useDepth = true;
        public bool useNormals = true;
    }

    private class ViewSpaceNormalsTexturePass : ScriptableRenderPass
    {
        private static readonly List<ShaderTagId> ShaderTagIds = new()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        private readonly FilteringSettings _filteringSettings;
        private readonly Material _normalsMaterial;
        private readonly ViewSpaceNormalsTextureSettings _settings;
        private readonly int _normalsTextureId = Shader.PropertyToID("_SceneViewSpaceNormals");

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        public ViewSpaceNormalsTexturePass(
            RenderPassEvent evt,
            LayerMask mask,
            ViewSpaceNormalsTextureSettings settings,
            Material mat)
        {
            renderPassEvent = evt;
            _settings = settings;
            _normalsMaterial = mat;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, mask);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_normalsMaterial == null)
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            var desc = cameraData.cameraTargetDescriptor;
            desc.colorFormat = _settings.colorFormat;
            desc.depthBufferBits = 0;

            TextureHandle normalsTex = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                desc,
                "_SceneViewSpaceNormals",
                clear: false,
                _settings.filterMode
            );

            SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                ShaderTagIds[0],
                renderingData,
                cameraData,
                lightData,
                sortFlags
            );

            for (int i = 1; i < ShaderTagIds.Count; i++)
                drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);

            drawingSettings.overrideMaterial = _normalsMaterial;

            var rendererListParams = new RendererListParams(
                renderingData.cullResults,
                drawingSettings,
                _filteringSettings
            );

            using var builder = renderGraph.AddRasterRenderPass<PassData>(
                "View Space Normals Pass",
                out var passData
            );

            passData.rendererList = renderGraph.CreateRendererList(rendererListParams);

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(normalsTex, 0);
            builder.SetGlobalTextureAfterPass(normalsTex, _normalsTextureId);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                // Limpiar solo color. No tocar depth.
                context.cmd.ClearRenderTarget(false, true, Color.clear);
                context.cmd.DrawRendererList(data.rendererList);
            });
        }
    }

    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        private readonly Material _outlineMat;
        private readonly OutlineSettings _settings;
        private readonly int _normalsTextureId = Shader.PropertyToID("_SceneViewSpaceNormals");

        private class PassData
        {
            internal Material material;
            internal TextureHandle source;
        }

        public ScreenSpaceOutlinePass(RenderPassEvent evt, OutlineSettings settings, Material mat)
        {
            renderPassEvent = evt;
            _settings = settings;
            _outlineMat = mat;

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_outlineMat == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle cameraTarget = resourceData.activeColorTexture;

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            TextureHandle tempTex = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                desc,
                "_OutlinesTemp",
                clear: false
            );

            using var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Screen Space Outline Pass",
                out var passData
            );

            _outlineMat.SetFloat("_Thickness", _settings.thickness);
            _outlineMat.SetFloat("_Blend", _settings.blend);
            _outlineMat.SetFloat("_DepthThreshold", _settings.depthThreshold);
            _outlineMat.SetFloat("_DepthSmoothWidth", _settings.depthSmoothWidth);
            _outlineMat.SetFloat("_RobertsCrossMultiplier", _settings.robertsCrossMultiplier);
            _outlineMat.SetFloat("_DepthAttenuation", _settings.depthAttenuation);
            _outlineMat.SetFloat("_DepthRelativeScale", _settings.depthRelativeScale);
            _outlineMat.SetFloat("_NormalsThreshold", _settings.normalsThreshold);
            _outlineMat.SetFloat("_NormalSensitivity", _settings.normalSensitivity);
            _outlineMat.SetFloat("_UseDepth", _settings.useDepth ? 1f : 0f);
            _outlineMat.SetFloat("_UseNormals", _settings.useNormals ? 1f : 0f);

            passData.material = _outlineMat;
            passData.source = cameraTarget;

            builder.UseTexture(cameraTarget, AccessFlags.Read);
            builder.UseGlobalTexture(_normalsTextureId, AccessFlags.Read);
            builder.SetRenderAttachment(tempTex, 0);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
            });

            resourceData.cameraColor = tempTex;
        }
    }

    [Header("Configuración")]
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] private LayerMask outlinesLayerMask = ~0;
    [SerializeField] private ViewSpaceNormalsTextureSettings viewSpaceNormalsTextureSettings = new();
    [SerializeField] private OutlineSettings outlineSettings = new();
    [SerializeField] private Shader viewSpaceNormalsShader;
    [SerializeField] private Shader outlineShader;

    private ViewSpaceNormalsTexturePass _normalsPass;
    private ScreenSpaceOutlinePass _outlinePass;
    private Material _normalsMat;
    private Material _outlineMat;

    public override void Create()
    {
        if (viewSpaceNormalsShader != null)
            _normalsMat = CoreUtils.CreateEngineMaterial(viewSpaceNormalsShader);

        if (outlineShader != null)
            _outlineMat = CoreUtils.CreateEngineMaterial(outlineShader);

        _normalsPass = new ViewSpaceNormalsTexturePass(
            renderPassEvent,
            outlinesLayerMask,
            viewSpaceNormalsTextureSettings,
            _normalsMat
        );

        _outlinePass = new ScreenSpaceOutlinePass(
            renderPassEvent,
            outlineSettings,
            _outlineMat
        );
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_normalsPass == null || _outlinePass == null)
            return;

        if (outlinesLayerMask.value == 0)
            return;

        if (_normalsMat == null || _outlineMat == null)
            return;

        renderer.EnqueuePass(_normalsPass);
        renderer.EnqueuePass(_outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_normalsMat);
        CoreUtils.Destroy(_outlineMat);
    }
}