using System;
using Unity.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class EdgeDetectionPassFeature : ScriptableRendererFeature
{
    private class EdgeDetectionPass : ScriptableRenderPass
    {
        private Material _material;
        
        private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
        
        public EdgeDetectionPass()
        {
            profilingSampler = new ProfilingSampler(nameof(EdgeDetectionPass));
        }
        
        public void Setup(ref EdgeDetectionSettings settings, ref Material edgeDetectionMaterial)
        {
            _material = edgeDetectionMaterial;
            renderPassEvent = settings.renderPassEvent;

            _material.SetFloat(OutlineThicknessProperty, settings.outlineThickness);
            _material.SetColor(OutlineColorProperty, settings.outlineColor);
        }

        private class PassData
        {
        };
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection", out _);

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData _, RasterGraphContext context) => { Blitter.BlitTexture(context.cmd, Vector2.one, _material, 0); });
        }
    }
    
    
    [Serializable]
    public class EdgeDetectionSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        [Range(0, 15)] public int outlineThickness = 3;
        public Color outlineColor = Color.black;
    }

    [SerializeField] private EdgeDetectionSettings settings;
    private Material _edgeDetectionMaterial;
    private EdgeDetectionPass _edgeDetectionPass;

    public override void Create()
    {
        _edgeDetectionPass ??= new EdgeDetectionPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || renderingData.cameraData.cameraType == CameraType.Reflection
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;
        
        
        if (_edgeDetectionMaterial == null)
        {
            _edgeDetectionMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/EdgeDetection3D"));
            if (_edgeDetectionMaterial == null)
            {
                Debug.LogWarning("Not all required materials could be created. Edge Detection will not render.");
                return;
            }
        }
        
        _edgeDetectionPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
        _edgeDetectionPass.requiresIntermediateTexture = true;
        _edgeDetectionPass.Setup(ref settings, ref _edgeDetectionMaterial);

        renderer.EnqueuePass(_edgeDetectionPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        _edgeDetectionPass = null;
        CoreUtils.Destroy(_edgeDetectionMaterial);
    }
}