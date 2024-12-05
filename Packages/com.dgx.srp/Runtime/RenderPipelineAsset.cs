using UnityEngine;

namespace DGX.SRP
{
    [CreateAssetMenu(menuName = "Rendering/DinogeniX render pipeline asset")]
    public class RenderPipelineAsset : UnityEngine.Rendering.RenderPipelineAsset
    {
        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame
        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            return new RenderPipeline(this);
        }
    }
}