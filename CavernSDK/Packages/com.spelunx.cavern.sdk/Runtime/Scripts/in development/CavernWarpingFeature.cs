using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Spelunx
{
    public class CavernWarpingFeature : CavernFeature, ICavernRenderFeature
    {

        [SerializeField] private Camera renderCamera;
        [SerializeField] private Shader warpingShader;
        [SerializeField] private Texture warpTexture;
        [SerializeField] private Texture blendTexture;

        private CavernWarpPass _renderPass;
        private Material renderMat;

        private void Awake()
        {
            _renderPass = new CavernWarpPass();
            CreateMaterial();
        }

        private void CreateMaterial()
        {
            renderMat = new Material(warpingShader);
            renderMat.SetTexture("_UVWarpTexture", warpTexture);
            renderMat.SetTexture("_BlendTexture", blendTexture);
            _renderPass?.SetMaterial(renderMat);
        }

        public void EnqueuePass(ScriptableRenderContext context, Camera camera)
        {
            if (camera == renderCamera)
            {
                _renderPass?.SetMaterial(renderMat);
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_renderPass);
            }
        }


#if UNITY_EDITOR

        public override void Reset()
        {
            base.Reset();
            CreateMaterial();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            CreateMaterial();
        }
#endif
    }


    public class CavernWarpPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        const string name = "CavernWarpRenderPass";

        class PassData
        {
            public Material material;
        }

        public CavernWarpPass()
        {
            this.requiresIntermediateTexture = true;
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public void SetMaterial(Material mat)
        {
            blitMaterial = mat;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Get source.
            var resourceData = frameData.Get<UniversalResourceData>();
            // if (resourceData.isActiveTargetBackBuffer) { return; }
            var source = resourceData.activeColorTexture;

            // Get destination.
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = name;
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            // We blit from the source to the source, just so we can overlay with transparent stuff
            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, blitMaterial, 0);
            // Add blit pass.
            renderGraph.AddBlitPass(para, passName: name);
            resourceData.cameraColor = destination;
        }
    }
}