using Spelunx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TestRenderPassFeature : CavernFeature, ICavernRenderFeature
{
    [SerializeField] private Material mat;

    private TestRenderPass rp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        rp = new TestRenderPass(mat);
    }
    public void EnqueuePass(ScriptableRenderContext context, Camera camera)
    {
        if (camera == cavernSetup.RenderCamera)
        {
            camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(rp);
        }
    }
}