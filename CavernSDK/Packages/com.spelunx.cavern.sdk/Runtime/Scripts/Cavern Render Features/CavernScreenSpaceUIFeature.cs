using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Spelunx
{
    [ExecuteInEditMode]
    [AddComponentMenu("Cavern/Render Features/Screenspace UI Feature")]
    public class CavernScreenSpaceUIFeature : CavernFeature, ICavernRenderFeature
    {
        public Camera uiCamera;
        // private RTHandle uiRTHandle;
        public Shader screenSpaceUIShader;
        [SerializeField, Range(-0.5f, 0.5f)] private float offsetFor3d = 0;
        // [SerializeField, Tooltip("Change offset for 3d to look like it's at the screen")] private bool setOffsetAtScreenDistance;
        private CavernScreenSpaceUIRenderPass _renderPass;
        private Material renderMat;
        private RenderTexture uiRenderTexture;

        /*
        Proper offset to make something look like it's floating at the cavern wall
                d/0.5 = d' / (3/2 pi r)
        d/0.5 = (sep/2) / (3/2 pi r)
        d = (0.065/2) / (3/2 pi 3.048)
        d = 0.002263
        */

        // private float distanceInMeters = 0;
        // private float counter = 0;

        // // void Update()
        // // {
        // //     distanceInMeters = Mathf.Sin(Time.time) * (5 / 2.0f) + (5 / 2.0f);
        // //     float sep = (2 * cavernSetup.IPD) / (3 * Mathf.PI) * ((1 / cavernSetup.CavernRadius) - (1 / distanceInMeters));
        // //     Debug.Log(sep);
        // //     renderMat?.SetFloat("_3d_offset", sep);
        // //     _renderPass?.SetMaterial(renderMat);
        // // }

        void OnEnable()
        {
            _renderPass = new CavernScreenSpaceUIRenderPass();
            CreateMaterial();
        }

        public void CreateMaterial()
        {
            if (screenSpaceUIShader == null) return;
            uiRenderTexture = new RenderTexture(width: cavernSetup.ScreenDimensions.x, height: cavernSetup.ScreenDimensions.y, depth: 24, RenderTextureFormat.ARGB32)
            {
                dimension = TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Clamp
            };
            uiRenderTexture.Create();
            renderMat = new Material(screenSpaceUIShader);
            _renderPass?.SetMaterial(renderMat);
            SetProperties();
        }

        private void SetProperties()
        {
            if (uiCamera != null)
            {
                uiCamera.targetTexture = uiRenderTexture;

            }
            if (renderMat != null)
            {
                renderMat.SetTexture("_MainTex", uiRenderTexture);
                renderMat.SetFloat("_3d_offset", offsetFor3d);
            }
        }

        public void EnqueuePass(ScriptableRenderContext context, Camera camera)
        {
            if (camera == cavernSetup.RenderCamera)
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
            SetProperties();
        }
#endif
    }


    public class CavernScreenSpaceUIRenderPass : ScriptableRenderPass
    {
        private Material blitMaterial;
        const string name = "CavernScreenSpaceUIRenderPass";

        class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        public CavernScreenSpaceUIRenderPass()
        {
            this.requiresIntermediateTexture = true;
            this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void SetMaterial(Material mat)
        {
            blitMaterial = mat;
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
            // var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, source, blitMaterial, 0);
            // renderGraph.AddBlitPass(blitParams, passName: name);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            passData.material = blitMaterial;
            // builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
            });

            // resourceData.cameraColor = source;
        }
    }
}