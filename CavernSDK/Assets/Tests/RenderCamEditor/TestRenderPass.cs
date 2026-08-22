using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class TestRenderPass : ScriptableRenderPass
{
    private Material blitMaterial;
    const string name = "TestRenderPass";

    class PassData
    {
        public Material material;
    }

    public TestRenderPass(Material blitMaterial)
    {
        this.blitMaterial = blitMaterial;
        this.requiresIntermediateTexture = true;
        this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public void SetMaterial(Material material)
    {
        blitMaterial = material;
    }


    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {

        // using var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData);
        var resourceData = frameData.Get<UniversalResourceData>();

        var source = resourceData.activeColorTexture;
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{name}";
        destinationDesc.clearBuffer = false;
        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, blitMaterial, 0);
        renderGraph.AddBlitPass(para, passName: name);

        // FrameData allows to get and set internal pipeline buffers. Here we update the CameraColorBuffer to the texture that we just wrote to in this pass. 
        // Because RenderGraph manages the pipeline resources and dependencies, following up passes will correctly use the right color buffer.
        // This optimization has some caveats. You have to be careful when the color buffer is persistent across frames and between different cameras, such as in camera stacking.
        //  In those cases you need to make sure your texture is an RTHandle and that you properly manage the lifecycle of it.
        resourceData.cameraColor = destination;
    }
}