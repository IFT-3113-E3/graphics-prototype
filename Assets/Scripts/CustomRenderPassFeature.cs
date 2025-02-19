using Unity.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    private class CustomRenderPass : ScriptableRenderPass
    {
        private const string PassName = "CustomRenderPass";
        private Material _material;
        private int _resolutionFactor = 1;

        public void Setup(Material material, int resolutionFactor)
        {
            _material = material;
            _resolutionFactor = Mathf.Clamp(resolutionFactor, 1, 4);
            requiresIntermediateTexture = true;
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var customEffect = stack.GetComponent<SphereVolumeComponent>();
            
            if (!customEffect.IsActive()) return;
            
            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Log.Error($"Skipping render pass. {PassName} requires an intermediate buffer to be active.");
                return;
            }

            var src = resourceData.activeColorTexture;
            var srcDesc = src.GetDescriptor(renderGraph);

            var dstDesc = renderGraph.GetTextureDesc(src);
            dstDesc.name = $"CameraColor-{PassName}";
            dstDesc.clearBuffer = false;
            dstDesc.msaaSamples = MSAASamples.None;
            dstDesc.filterMode = FilterMode.Point;
            dstDesc.wrapMode = TextureWrapMode.Clamp;
            dstDesc.width = srcDesc.width / _resolutionFactor;
            dstDesc.height = srcDesc.height / _resolutionFactor;
            
            var dst = renderGraph.CreateTexture(dstDesc);
            
            RenderGraphUtils.BlitMaterialParameters blitParams = new(src, dst, _material, 0);
            renderGraph.AddBlitPass(blitParams, passName: PassName);
            
            resourceData.cameraColor = dst;
        }
    }

    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    public Material material;
    public int resolutionFactor = 1;

    private CustomRenderPass _scriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        _scriptablePass = new CustomRenderPass
        {
            // Configures where the render pass should be injected.
            renderPassEvent = injectionPoint
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!material)
        {
            Log.Warning("CustomRenderPass requires a material.");
            return;
        }
        
        _scriptablePass.Setup(material, resolutionFactor);
        renderer.EnqueuePass(_scriptablePass);
    }
}
