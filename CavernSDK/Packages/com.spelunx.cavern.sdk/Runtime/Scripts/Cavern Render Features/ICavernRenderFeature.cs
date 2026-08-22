using UnityEngine;
using UnityEngine.Rendering;

namespace Spelunx
{
    public interface ICavernRenderFeature
    {

        // Exposing this, which is naturally implemented on components
        bool enabled { get; set; }
        // Exposing this, which is naturally implemented on components
        bool isActiveAndEnabled { get; }
        public void EnqueuePass(ScriptableRenderContext context, Camera camera);
    }
}
