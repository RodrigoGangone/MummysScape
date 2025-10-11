using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlines : ScriptableRendererFeature
{
    // === Ajustes para la RT de normales ===
    [System.Serializable]
    public class ViewSpaceNormalsTextureSettings
    {
        [Tooltip("Formato de color de la textura de normales.")]
        public RenderTextureFormat colorFormat = RenderTextureFormat.ARGB32;

        [Tooltip("Bits de profundidad para la RT de normales.")]
        public int depthBufferBits = 0;

        [Tooltip("Filter Mode para la muestra en el pass de outline.")]
        public FilterMode filterMode = FilterMode.Point;

        [Tooltip("Color de limpieza (usar alpha).")]
        public Color backgroundColor = new Color(0,0,0,0);
    }

    // === Ajustes para el outline ===
    [System.Serializable]
    public class OutlineSettings
    {
        [Range(0.5f, 5f)] public float thickness = 1.0f;
        [Range(0.0f, 1f)] public float blend = 1.0f;

        // Depth
        [Range(0.0f, 3f)] public float depthThreshold = 1.0f;
        [Range(0.0f, 1f)] public float depthSmoothWidth = 0.05f;
        [Range(0.0f, 2f)] public float robertsCrossMultiplier = 1.0f;
        [Range(0.0f, 2f)] public float depthAttenuation = 0.0f;      // 0 = sin atenuar
        [Range(0.0f, 4f)] public float depthRelativeScale = 0.0f;     // 0 = apagar componente relativa

        // Normals
        [Range(0.0f, 2f)] public float normalsThreshold = 0.2f;
        [Range(0.0f, 4f)] public float normalSensitivity = 1.0f;

        // Toggles
        public bool useDepth = true;
        public bool useNormals = true;
    }

    // ------------------ PASS 1: generar textura de normales (view space) ------------------
    private class ViewSpaceNormalsTexturePass : ScriptableRenderPass
    {
        private readonly List<ShaderTagId> _shaderTagIdList = new()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("LightweightForward"),
        };

        private FilteringSettings _filteringSettings;
        private readonly Material _normalsMaterial;
        private readonly RenderTargetHandle _normalsHandle;
        private readonly ViewSpaceNormalsTextureSettings _settings;

        public ViewSpaceNormalsTexturePass(
            RenderPassEvent evt,
            LayerMask outlinesLayerMask,
            ViewSpaceNormalsTextureSettings settings,
            Material normalsMat)
        {
            renderPassEvent = evt;
            _settings = settings;

            _normalsMaterial = normalsMat;
            _normalsHandle = new RenderTargetHandle();
            _normalsHandle.Init("_SceneViewSpaceNormals");

            _filteringSettings = new FilteringSettings(RenderQueueRange.all, outlinesLayerMask);
            _filteringSettings.renderingLayerMask = uint.MaxValue; 
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.colorFormat     = _settings.colorFormat;
            desc.depthBufferBits = 0; // usamos el depth de la cámara, no uno nuevo

            cmd.GetTemporaryRT(_normalsHandle.id, desc, _settings.filterMode);

            //RT de normales + el depth de la cámara
            #if UNITY_2022_3_OR_NEWER // URP 14+
                        ConfigureTarget(_normalsHandle.Identifier(), renderingData.cameraData.renderer.cameraDepthTargetHandle);
            #else // URP 12/13
                    ConfigureTarget(_normalsHandle.Identifier(), renderingData.cameraData.renderer.cameraDepthTarget);
            #endif

            // Se limpia solo el color (alpha para coverage)
            ConfigureClear(ClearFlag.Color, new Color(0,0,0,0));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // var desc = cameraTextureDescriptor;
            // desc.colorFormat = _settings.colorFormat;
            // desc.depthBufferBits = _settings.depthBufferBits;
            // // Si usás RTHandles en URP más nuevo, podrías migrar. Esto es simple y funciona.
            //
            // cmd.GetTemporaryRT(_normalsHandle.id, desc, _settings.filterMode);
            // ConfigureTarget(_normalsHandle.Identifier());
            // ConfigureClear(ClearFlag.All, _settings.backgroundColor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_normalsMaterial == null)
                return;

            var cmd = CommandBufferPool.Get("SceneViewSpaceNormalsTextureCreation");
            using (new ProfilingScope(cmd, new ProfilingSampler("Normals Pass")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawingSettings = CreateDrawingSettings(
                    _shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial = _normalsMaterial;
                
                //Usamos el FilteringSettings con LayerMask configurado
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
                // Aseguramos que la textura quede accesible como global para el shader de outline
                cmd.SetGlobalTexture("_SceneViewSpaceNormals", _normalsHandle.Identifier());
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null) return;
            cmd.ReleaseTemporaryRT(_normalsHandle.id);
        }
    }

    // ------------------ PASS 2: post-proceso de outlines en pantalla ------------------
    private class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        private readonly Material _outlineMat;
        private RenderTargetIdentifier _cameraColorTarget;
        private RenderTargetIdentifier _tempBuffer;
        private readonly int _tempBufferId = Shader.PropertyToID("_OutlinesTempBuffer");

        private readonly OutlineSettings _settings;

        public ScreenSpaceOutlinePass(RenderPassEvent evt, OutlineSettings settings, Material outlineMat)
        {
            renderPassEvent = evt;
            _settings = settings;
            _outlineMat = outlineMat;
        }

        // Llamado por la Feature antes de encolar el pass
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Tomar el color target DENTRO del scope del pass
            _cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Creamos RT temporal para el blit ping-pong
            cmd.GetTemporaryRT(_tempBufferId, cameraTextureDescriptor, FilterMode.Bilinear);
            _tempBuffer = new RenderTargetIdentifier(_tempBufferId);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_outlineMat == null)
                return;

            // === Seteo de parámetros dinámicos (Inspector) ===
            var s = _settings;

            // Color / grosor / mezcla
            _outlineMat.SetFloat("_DepthThreshold", s.depthThreshold);
            _outlineMat.SetFloat("_DepthSmoothWidth", s.depthSmoothWidth);
            _outlineMat.SetFloat("_RobertsCrossMultiplier", s.robertsCrossMultiplier);
            _outlineMat.SetFloat("_DepthAttenuation", s.depthAttenuation);
            _outlineMat.SetFloat("_DepthRelativeScale", s.depthRelativeScale);

            _outlineMat.SetFloat("_NormalsThreshold", s.normalsThreshold);
            _outlineMat.SetFloat("_NormalSensitivity", s.normalSensitivity);

            _outlineMat.SetFloat("_UseDepth",   s.useDepth   ? 1f : 0f);
            _outlineMat.SetFloat("_UseNormals", s.useNormals ? 1f : 0f);
            
            _outlineMat.SetFloat("_Thickness", s.thickness);
            _outlineMat.SetFloat("_Blend",     s.blend);

            var cmd = CommandBufferPool.Get("ScreenSpaceOutlines");
            using (new ProfilingScope(cmd, new ProfilingSampler("Outline Pass")))
            {
                // 1) Copiamos el color de cámara a un RT temporal
                Blit(cmd, _cameraColorTarget, _tempBuffer);

                // 2) Publicamos el source para el shader de outline
                cmd.SetGlobalTexture("_MainTex", _tempBufferId);     // shaders que leen _MainTex
                cmd.SetGlobalTexture("_BlitTexture", _tempBufferId); // compat URP Blit

                // 3) Composición: Temp -> Cámara con el material de outlines
                Blit(cmd, _tempBuffer, _cameraColorTarget, _outlineMat, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null) return;
            cmd.ReleaseTemporaryRT(_tempBufferId);
        }
    }

    // =================== Campos de la Feature ===================
    [Header("Orden de inyección del pass")]
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

    [Header("Layers a outlinear")]
    [SerializeField] private LayerMask outlinesLayerMask = ~0; // por defecto, todo

    [Header("Ajustes RT de Normales")]
    [SerializeField] private ViewSpaceNormalsTextureSettings viewSpaceNormalsTextureSettings = new();

    [Header("Ajustes Outline")]
    [SerializeField] private OutlineSettings outlineSettings = new();
    
    [SerializeField] private Shader viewSpaceNormalsShader;
    [SerializeField] private Shader outlineShader;

    private ViewSpaceNormalsTexturePass _normalsPass;
    private ScreenSpaceOutlinePass _outlinePass;
    
    private Material _normalsMat;
    private Material _outlineMat;

    public override void Create()
    {
        if (!viewSpaceNormalsShader)
            Debug.LogError("[ScreenSpaceOutlines] Falta asignar 'viewSpaceNormalsShader' en el Inspector.");
        if (!outlineShader)
            Debug.LogError("[ScreenSpaceOutlines] Falta asignar 'outlineShader' en el Inspector.");

        if (viewSpaceNormalsShader) _normalsMat = new Material(viewSpaceNormalsShader);
        if (outlineShader) _outlineMat = new Material(outlineShader);

        _normalsPass = new ViewSpaceNormalsTexturePass(
            renderPassEvent, outlinesLayerMask, viewSpaceNormalsTextureSettings, _normalsMat);

        _outlinePass = new ScreenSpaceOutlinePass(
            renderPassEvent, outlineSettings, _outlineMat);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_outlinePass == null || _normalsPass == null) return;

        //Si no hay capas seleccionadas, no hacemos nada
        if (outlinesLayerMask.value == 0) return;

        renderer.EnqueuePass(_normalsPass);
        renderer.EnqueuePass(_outlinePass);
    }
}