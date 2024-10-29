using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    public struct MapBounds : IComponentData
    {
        public Bounds Bounds;
    }
    
    [UseDefaultInspector]
    public class MapBoundsComponent : ComponentDataBehaviour<MapBounds>
    {
        [SerializeField]
        Renderer boundsRenderer;

        private void Reset()
        {
            boundsRenderer = GetComponent<Renderer>();
        }

        protected override void Bake<K>(ref MapBounds serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            if (boundsRenderer)
            {
                serializedData.Bounds = boundsRenderer.bounds;    
            }
        }
    }
}
