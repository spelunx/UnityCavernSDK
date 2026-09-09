using UnityEngine;
using UnityEngine.Rendering;

namespace Spelunx
{
    [ExecuteInEditMode]
    [AddComponentMenu("Cavern/Features/CAVERN Renderer")]
    [DisallowMultipleComponent]
    public class CavernRenderer : CavernFeature
    {
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            foreach (var renderFeature in GetComponentsInChildren<ICavernRenderFeature>()) //cavernSetup.features.GetAllOfType<ICavernRenderFeature>())
            {
                if (!renderFeature.isActiveAndEnabled) continue;
                renderFeature.EnqueuePass(context, camera);
            }
        }
    }
}
