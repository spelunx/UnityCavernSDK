using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Spelunx
{
    [ExecuteInEditMode]
    public class CavernPreviewFeature : CavernFeature, ICavernRenderFeature
    {
        public enum PreviewEye { Left, Right }
        [SerializeField] private PreviewEye previewEye = PreviewEye.Left;
        // [SerializeField] private RenderTexture testPreviewTexture;

        [SerializeField] private Material previewMaterial;

        private Mesh previewMesh = null;
        private RenderTexture previewTexture = null;
        private RenderTexture editModeRenderOutputTexture = null;
        // private RTHandle previewTexture = null;
        private CavernPreviewRenderPass cavernPreviewRenderPass;
        private bool didRenderLastFrame = false;

#if !UNITY_EDITOR
    // Empty method because we don't need the preview outside of editor
    public void EnqueuePass(ScriptableRenderContext context, Camera camera){}
#endif

#if UNITY_EDITOR
        private void OnEnable()
        {
            didRenderLastFrame = false;
            CreatePreviewMesh();
            CreatePreviewTexture();
            cavernPreviewRenderPass = new CavernPreviewRenderPass(previewTexture);
            cavernPreviewRenderPass.SetPreviewEye(previewEye);
            cavernSetup.settingsChanged.AddListener(CreatePreviewMesh);
        }

        private void OnDisable()
        {
            cavernSetup.settingsChanged.RemoveListener(CreatePreviewMesh);
        }

        void LateUpdate()
        {
            ManuallyRenderCamera();
        }

        private void OnDestroy()
        {
            // previewTexture?.Release();
            // previewTextureTest?.Release();
        }

        /// \*brief
        /// Generate a curved screen mesh.
        /// \*warning Ensure that the mesh's material disables back-face culling!
        private void CreatePreviewMesh()
        {
            previewMesh = cavernSetup.GenerateMesh();
            previewMesh.name = "Cavern Preview Mesh";

        }

        private void CreatePreviewTexture()
        {
            // previewTextureTest?.Release();
            // TODO: change preview texture resolution to just be the base resolution
            // previewTextureTest = RTHandles.Alloc(
            //     width: cavernSetup.ScreenDimensions.x,
            //     height: cavernSetup.ScreenDimensions.y,
            //     depthBufferBits: 0,
            //     // depthBufferBits: DepthBits.Depth32,
            //     // colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
            //     colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.ARGB32, true),
            //     dimension: TextureDimension.Tex2D,
            //     wrapMode: TextureWrapMode.Clamp
            // );
            previewTexture = new RenderTexture(cavernSetup.ScreenDimensions.x, cavernSetup.ScreenDimensions.y, 0, RenderTextureFormat.ARGB32)
            {
                dimension = TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Clamp,
            };
            // previewTexture.Create();
            // previewTexture.hideFlags = HideFlags.HideAndDontSave;
            editModeRenderOutputTexture = new RenderTexture(cavernSetup.ScreenDimensions.x, cavernSetup.ScreenDimensions.y, 32, RenderTextureFormat.ARGB32)
            {
                dimension = TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Clamp
            };
            // editModeRenderOutputTexture.Create();
            // editModeRenderOutputTexture.hideFlags = HideFlags.HideAndDontSave;
            cavernPreviewRenderPass?.SetDestinationTexture(previewTexture);
        }

        public void EnqueuePass(ScriptableRenderContext context, Camera camera)
        {
            if (camera == cavernSetup.RenderCamera)
            {
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(cavernPreviewRenderPass);
                didRenderLastFrame = true;
            }
        }

        void ManuallyRenderCamera()
        {
            // We only want to manually render if the camera didn't already render this frame
            if (!didRenderLastFrame)
            {
                if (editModeRenderOutputTexture == null)
                {
                    CreatePreviewTexture();
                }
                var requestData = new UniversalRenderPipeline.StandardRequest()
                {
                    destination = editModeRenderOutputTexture
                };

                // Check compatibility and submit it to the Render Graph pipeline
                if (UniversalRenderPipeline.SupportsRenderRequest(cavernSetup.RenderCamera, requestData))
                {
                    didRenderLastFrame = true;
                    UniversalRenderPipeline.SubmitRenderRequest(cavernSetup.RenderCamera, requestData);
                }
            }
            didRenderLastFrame = false;
        }

        public override void Reset()
        {
            base.Reset();
            // CreatePreviewMesh();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            // This method is called whenever a setting is changed in the inspector, or at the beginning of scene mode rendering.
            // If any of the Cavern size settings are changed, we need to regenerate the mesh.
            // CreatePreviewMesh();
            cavernPreviewRenderPass?.SetPreviewEye(previewEye);
        }

        private void OnDrawGizmos()
        {
            if (!enabled) return;
            if (previewMaterial == null)
            {
                Debug.LogAssertion("CavernPreviewFeature: Preview material cannot be null!");
            }
            previewMaterial.mainTexture = previewTexture;

            previewMaterial.SetPass(0);
            // We need to use Graphics.DrawMeshNow instead of Gizmos.DrawMesh so we can get a texture on it.
            Graphics.DrawMeshNow(previewMesh, transform.position, transform.rotation);
        }


        public class CavernPreviewRenderPass : ScriptableRenderPass
        {
            const string name = "CavernPreviewRenderPass";
            private RenderTexture destination;
            private PreviewEye previewEye;

            public CavernPreviewRenderPass(RenderTexture destination)
            {
                this.destination = destination;
                this.requiresIntermediateTexture = true;
                this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }

            public void SetPreviewEye(PreviewEye eye)
            {
                previewEye = eye;
            }

            public void SetDestinationTexture(RenderTexture texture)
            {
                destination = texture;
            }

            class PassData
            {
                public TextureHandle source;
                public TextureHandle destination;
                public Vector4 writeArea;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) { return; }
                var source = resourceData.activeColorTexture;

                RTHandle rtHandleWrapper = RTHandles.Alloc(destination);
                TextureHandle destinationHandle = renderGraph.ImportTexture(rtHandleWrapper);
                // TextureHandle destinationHandle = renderGraph.ImportTexture(destinationRT);

                // RenderGraphUtils.BlitMaterialParameters para = new(source, destinationHandle, blitMaterial, 0);
                // using var builder = renderGraph.AddBlitPass(para, passName: name, returnBuilder: true);
                // using var builder = renderGraph.AddBlitPass(source, destinationHandle, new Vector2(1, 1), new Vector2(0, 0), passName: name);
                // builder.AllowPassCulling(false);

                // Using this instead of addblitpass so I can adjust which eye gets rendered
                using var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData);
                passData.source = source;
                passData.destination = destinationHandle;
                if (previewEye == PreviewEye.Left)
                {
                    passData.writeArea = new Vector4(1, 1, 0, 0f);
                }
                else
                {
                    passData.writeArea = new Vector4(1, 1, 0, -0.5f);
                }

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, data.writeArea, 0, false);
                });

            }
        }
#endif
    }
}