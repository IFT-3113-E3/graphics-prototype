using System;
using Unity.Logging;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class EdgeDetectionPassFeature : ScriptableRendererFeature
{
    class DrawObjectsPass : ScriptableRenderPass
    {
        private Material _materialToUse;
        private int _textureId;

        public void Setup(Material material)
        {
            _materialToUse = material;
            _textureId = Shader.PropertyToID("_EdgeDetectionFilterTexture");
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }
       
        private class PassData
        {
            // Create a field to store the list of objects to draw
            public RendererListHandle RendererListHandle;
            internal TextureHandle TargetTexture;
        }
        
        public class IntermediateTextureData : ContextItem
        {
            public TextureHandle IntermediateTexture;
            public override void Reset()
            {
                IntermediateTexture = TextureHandle.nullHandle;
            }
        }
 
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Edge Detection Filter Pass", out var passData);
            // Get the data needed to create the list of objects to draw
            UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalLightData lightData = frameContext.Get<UniversalLightData>();
            SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
            RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
            FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, 1 << 9);

            // Redraw only objects that have their LightMode tag set to UniversalForward 
            ShaderTagId shadersToOverride = new ShaderTagId("UniversalForward");

            // Create drawing settings
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

            // Add the override material to the drawing settings
            drawSettings.overrideMaterial = _materialToUse;

            // Create the list of objects to draw
            var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

            // Convert the list to a list handle that the render graph system can use
            passData.RendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
            
            // Create the render texture to draw the objects to
            RenderTextureDescriptor textureProperties = cameraData.cameraTargetDescriptor;//new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
            textureProperties.depthStencilFormat = GraphicsFormat.None;
            textureProperties.graphicsFormat = GraphicsFormat.R8_UNorm;
            TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "_EdgeDetectionFilterTexture", false);
            
            IntermediateTextureData intermediateTextureData = frameContext.Create<IntermediateTextureData>();
            intermediateTextureData.IntermediateTexture = texture;
            
            // Set the render target as the color and depth textures of the active camera texture
            builder.UseRendererList(passData.RendererListHandle);
            builder.SetRenderAttachment(texture, 0, AccessFlags.Write);
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(texture, _textureId);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Clear the render target to black
            context.cmd.ClearRenderTarget(true, true, Color.black);

            // Draw the objects in the list
            context.cmd.DrawRendererList(data.RendererListHandle);
        }

    }
    
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
        }

        private class PassData
        {
        };
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var intermediateTextureData = frameData.Get<DrawObjectsPass.IntermediateTextureData>();
            
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection", out _);

            builder.SetRenderAttachment(resourceData.cameraColor, 0);
            builder.UseTexture(intermediateTextureData.IntermediateTexture, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData _, RasterGraphContext context) => { Blitter.BlitTexture(context.cmd, Vector2.one, _material, 0); });
        }
    }

    
    [Serializable]
    public class EdgeDetectionSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    [SerializeField] private EdgeDetectionSettings settings;
    private Material _edgeDetectionMaterial;
    private Material _drawObjectsMaterial;
    private EdgeDetectionPass _edgeDetectionPass;
    private DrawObjectsPass _drawObjectsPass;

    public override void Create()
    {
        _edgeDetectionPass ??= new EdgeDetectionPass();
        _drawObjectsPass ??= new DrawObjectsPass();
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
        
        if (_drawObjectsMaterial == null)
        {
            _drawObjectsMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/EdgeDetectionFilter"));
            if (_drawObjectsMaterial == null)
            {
                Debug.LogWarning("Not all required materials could be created. Edge Detection will not render.");
                return;
            }
        }
        
        _edgeDetectionPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
        _edgeDetectionPass.requiresIntermediateTexture = true;
        _edgeDetectionPass.Setup(ref settings, ref _edgeDetectionMaterial);
        _drawObjectsPass.Setup(_drawObjectsMaterial);

        renderer.EnqueuePass(_drawObjectsPass);
        renderer.EnqueuePass(_edgeDetectionPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        _edgeDetectionPass = null;
        CoreUtils.Destroy(_edgeDetectionMaterial);
    }
}