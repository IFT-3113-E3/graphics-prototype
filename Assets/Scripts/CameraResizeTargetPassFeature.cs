using Unity.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class CameraResizeTargetPassFeature : ScriptableRendererFeature
{
    
    private class CameraResizeTargetPass : ScriptableRenderPass
    {
        private const string PassName = "CameraResizeTargetPass";
        private int _resolutionFactor = 1;
        private Material _material;

        enum TargetType
        {
            Color,
            Depth,
            Normal
        }

        private class PassData
        {
            public Material material;
        }
        
        public void Setup(Material material, int resolutionFactor)
        {
            _resolutionFactor = Mathf.Clamp(resolutionFactor, 1, 4);
            _material = material;
        }
        
        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, Color.black);
            var mesh = new Mesh();
            const float width = 1f;
            const float height = 1f;
            var vertices = new Vector3[]
            {
                new(0, 0, 0),
                new(width, 0, 0),
                new(0, height, 0),
                new(width, height, 0)
            };
            mesh.vertices = vertices;

            // Draw quad on screen
            context.cmd.DrawMesh(mesh, Matrix4x4.identity, data.material);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData);
            passData.material = _material;
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            builder.AllowPassCulling(false);
        }
            
        private void ConfigureTextureDesc(ref TextureDesc desc, TargetType targetType)
        {
            desc.name = $"CameraResizeTargetPass-{targetType}";
            desc.width /= _resolutionFactor;
            desc.height /= _resolutionFactor;
        }
    }

    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPrePasses;
    public int resolutionFactor = 1;
    public Material material;

    private CameraResizeTargetPass _scriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        _scriptablePass = new CameraResizeTargetPass
        {
            // Configures where the render pass should be injected.
            renderPassEvent = injectionPoint
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _scriptablePass.Setup(material, resolutionFactor);
        renderer.EnqueuePass(_scriptablePass);
    }
}
