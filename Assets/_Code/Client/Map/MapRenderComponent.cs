using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.MapWorks
{
    [System.Serializable]
    public struct MapRender : IComponentData
    {
        [HideInAuthoring] public float MapCameraOrthoSize;
        [HideInAuthoring] public int MapRenderLayers;
        public int MapTextureSize;
        [HideInAuthoring] public Entity MapPlaneMesh;
    }
    
    [UseDefaultInspector(true)]
    public class MapRenderComponent : ComponentDataBehaviour<MapRender>
    {
        public Camera Camera;

        protected override void Reset()
        {
            base.Reset();
            Camera = GetComponent<Camera>();
        }

        protected override MapRender CreateDefaultValue()
        {
            return new MapRender
            {
                MapTextureSize = 512
            };
        }

        [SerializeField] private MeshRenderer mapPlaneMesh;

        protected override void Bake<K>(ref MapRender serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            var camera = baker.GetComponent<Camera>();
            if (camera)
            {
                serializedData.MapRenderLayers = camera.cullingMask;
                serializedData.MapCameraOrthoSize = camera.orthographicSize;
            }
            
            serializedData.MapPlaneMesh = baker.GetEntity(mapPlaneMesh, TransformUsageFlags.Renderable);
        }
    }
}

