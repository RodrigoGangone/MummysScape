using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{

    public class OutlineRenderPass : ScriptableRenderPass
    {

        private Settings settings;
        private FilteringSettings filteringSettings;
        private ProfilingSampler _profilingSampler;
        private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();
        private RTHandle rtCustomColor, rtTempColor;

        public OutlineRenderPass(Settings settings, string name)
        {
            this.settings = settings;
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.layerMask);

            // Use default tags
            shaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagsList.Add(new ShaderTagId("UniversalForward"));
            shaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));

            _profilingSampler = new ProfilingSampler(name);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
            colorDesc.depthBufferBits = 0;

            // Set up temporary color buffer (for blit)
            RenderingUtils.ReAllocateIfNeeded(ref rtTempColor, colorDesc, name: "_TemporaryColorTexture");

            // Set up custom color target buffer (to render objects into)
            if (settings.colorTargetDestinationID != "")
            {
                RenderingUtils.ReAllocateIfNeeded(ref rtCustomColor, colorDesc, name: settings.colorTargetDestinationID);
            }
            else
            {
                // colorDestinationID is blank, use camera target instead
                rtCustomColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            // Using camera's depth target (that way we can ZTest with scene objects still)
            RTHandle rtCameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            ConfigureTarget(rtCustomColor, rtCameraDepth);
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            // Set up profiling scope for Profiler & Frame Debugger
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // Command buffer shouldn't contain anything, but apparently need to
                // execute so DrawRenderers call is put under profiling scope title correctly
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Draw Renderers to Render Target (set up in OnCameraSetup)
                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, sortingCriteria);
                if (settings.overrideMaterial != null)
                {
                    drawingSettings.overrideMaterialPassIndex = settings.overrideMaterialPass;
                    drawingSettings.overrideMaterial = settings.overrideMaterial;
                }
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                // Pass our custom target to shaders as a Global Texture reference
                // In a Shader Graph, you'd obtain this as a Texture2D property with "Exposed" unticked
                if (settings.colorTargetDestinationID != "")
                    cmd.SetGlobalTexture(settings.colorTargetDestinationID, rtCustomColor);

                // Apply material (e.g. Fullscreen Graph) to camera
                if (settings.blitMaterial != null)
                {
                    RTHandle camTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    if (camTarget != null && rtTempColor != null)
                    {
                        Blitter.BlitCameraTexture(cmd, camTarget, rtTempColor, settings.blitMaterial, 0);
                        Blitter.BlitCameraTexture(cmd, rtTempColor, camTarget);
                    }
                }
            }
            // Execute Command Buffer one last time and release it
            // (otherwise we get weird recursive list in Frame Debugger)
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        // Cleanup Called by feature below
        public void Dispose()
        {
            if (settings.colorTargetDestinationID != "")
                rtCustomColor?.Release();
            rtTempColor?.Release();
        }
    }

    // Exposed Settings

    [System.Serializable]
    public class Settings
    {
        public bool showInSceneView = true;
        public RenderPassEvent _event = RenderPassEvent.AfterRenderingOpaques;

        [Header("Draw Renderers Settings")]
        public LayerMask layerMask = 1;
        public Material overrideMaterial;
        public int overrideMaterialPass;
        public string colorTargetDestinationID = "";

        [Header("Blit Settings")]
        public Material blitMaterial;
    }

    public Settings settings = new Settings();

    // Feature Methods

    private OutlineRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new OutlineRenderPass(settings, name);
        m_ScriptablePass.renderPassEvent = settings._event;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview) return; // Ignore feature for editor/inspector previews & asset thumbnails
        if (!settings.showInSceneView && cameraType == CameraType.SceneView) return;
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass.Dispose();
    }
}