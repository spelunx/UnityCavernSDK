using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class RenderCamScript : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private Camera renderCam;
    [SerializeField] private Material mat;

    private TestRenderPass rp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        rp = new TestRenderPass(mat);
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        // RenderPipelineManager.beginContextRendering += Test;
    }

    private void Test(ScriptableRenderContext context, List<Camera> list)
    {
        Debug.Log("hi");
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        // RenderPipelineManager.beginContextRendering -= Test;
    }

    void LateUpdate()
    {
        // UniversalRenderPipeline.SingleCameraRequest request = new();
        // RenderPipeline.StandardRequest request = new();
        // request.destination = renderCam.targetTexture;
        // // Check if the request is supported by the render pipeline
        // if (RenderPipeline.SupportsRenderRequest(renderCam, request))
        // {
        //     RenderPipeline.SubmitRenderRequest(renderCam, request);
        // }
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        /*
        // "Highjack" the GUI camera insert a render pass into the URP RenderGraph to render the output.
        if (camera == guiCamera)
        {
            camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(cavernRenderPass);
#if UNITY_EDITOR
            // Only render if we are playing and or showing a live preview.

            if (UnityEditor.EditorApplication.isPlaying || livePreview)
            {
                camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(cavernPreviewRenderPass);
            }

#endif
        }
        */
        if (camera == renderCam)
        {
            camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(rp);
        }
    }
}
