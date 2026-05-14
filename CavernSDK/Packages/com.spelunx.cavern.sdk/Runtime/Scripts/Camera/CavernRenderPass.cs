using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Spelunx
{
    // Todo: Execute to game view in edit mode?
    public class CavernRenderPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        const string name = "CavernRenderPass";

        class PassData
        {
            public Material material;
            public TextureHandle destination;
        }

        public CavernRenderPass(Material blitMaterial)
        {
            this.blitMaterial = blitMaterial;
            this.requiresIntermediateTexture = false;
            this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void SetMaterial(Material material)
        {
            blitMaterial = material;
        }


        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData);
            // passData.destination = 
            // Get source.
            var resourceData = frameData.Get<UniversalResourceData>();
            // Debug.Log(resourceData.isActiveTargetBackBuffer);
            // if (resourceData.isActiveTargetBackBuffer) { return; }
            // var source = resourceData.activeColorTexture;
            // var s = TextureHandle.nullHandle;

            // Get destination.
            // var destinationDesc = renderGraph.GetTextureDesc(source);
            // destinationDesc.name = name;
            // destinationDesc.clearBuffer = false;
            // TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            // Add blit pass.
            // RenderGraphUtils.BlitMaterialParameters para = new(source, destination, blitMaterial, 0);
            // renderGraph.AddBlitPass(para, passName: name);

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            passData.material = blitMaterial;
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
            });

            // resourceData.cameraColor = resourceData.activeColorTexture;
        }
    }
}