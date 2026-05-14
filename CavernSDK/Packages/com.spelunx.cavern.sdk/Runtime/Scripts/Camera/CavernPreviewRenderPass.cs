using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace Spelunx
{
    // Todo: Execute to game view in edit mode?
    public class CavernPreviewRenderPass : ScriptableRenderPass
    {
        const string name = "CavernPreviewRenderPass";
        private RTHandle destination;
        private Material blitMaterial;

        public CavernPreviewRenderPass(RTHandle destination)
        {
            this.destination = destination;
            this.requiresIntermediateTexture = true;
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

            // This is the standard URP blit shader
            Shader shader = Shader.Find("Hidden/Universal Render Pipeline/Blit");
            blitMaterial = new Material(shader);

        }

        class PassData
        {
            public TextureHandle source;
            public TextureHandle destination;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {

            // Add blit pass.
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) { return; }
            var source = resourceData.activeColorTexture;

            // Get destination.
            // var destinationDesc = renderGraph.GetTextureDesc(source);
            // destinationDesc.name = name;
            // destinationDesc.clearBuffer = false;
            // TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            TextureHandle destinationHandle = renderGraph.ImportTexture(destination);
            /*
            RenderGraphUtils.BlitMaterialParameters para = new(source, destinationHandle, blitMaterial, 0);
            renderGraph.AddBlitPass(para, passName: name);
            */
            // Use this instead of AddBlitPass for external RTHandles
            using var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData);
            passData.source = source;
            passData.destination = destinationHandle;

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }
    }
}