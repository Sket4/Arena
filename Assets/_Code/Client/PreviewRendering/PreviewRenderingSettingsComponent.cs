using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.PreviewRendering
{
    [System.Serializable]
    public struct PreviewRenderingSettings : IComponentData
    {
        public Entity ItemPivot;
        public float CameraSizeMultiplier;
        [HideInAuthoring]
        public int RenderLayer;
    }

    [UseDefaultInspector(true)]
    public class PreviewRenderingSettingsComponent : ComponentDataBehaviour<PreviewRenderingSettings>
    {
        public Transform ItemPivot;

        protected override void Bake<K>(ref PreviewRenderingSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.ItemPivot = baker.GetEntity(ItemPivot);
            serializedData.RenderLayer = gameObject.layer;
        }
    }
}
