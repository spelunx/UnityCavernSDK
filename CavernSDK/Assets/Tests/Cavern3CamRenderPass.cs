using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Spelunx
{
    public class Cavern3CamRenderPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        private RTHandle source;
        const string name = "Cavern3CamRenderPass";

        class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        public Cavern3CamRenderPass(Material blitMaterial, RTHandle source)
        {
            this.blitMaterial = blitMaterial;
            this.source = source;
            this.requiresIntermediateTexture = false;
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
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

            passData.material = blitMaterial;
            // TextureDesc importDesc = new(source)
            // {
            //     depthBufferBits = DepthBits.None
            // };
            // RenderTargetIdentifier colorOnlyIdentifier = new(source);
            // // passData.source = renderGraph.ImportTexture(colorOnlyIdentifier, importDesc);
            // RenderTargetInfo info = new()
            // {
            //     width = source.rt.width,
            //     height = source.rt.height,
            //     volumeDepth = 1,
            //     format = source.rt.graphicsFormat,
            // };
            // passData.source = renderGraph.ImportTexture(source, info);
            // builder.UseTexture(passData.source, AccessFlags.Read);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
            });

            // resourceData.cameraColor = resourceData.activeColorTexture;
        }
    }
}