using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TzarGames.Renderer.Baking
{
    public class ReflectionProbeBaker : Baker<ReflectionProbe>
    {
        public override void Bake(ReflectionProbe authoring)
        {
            var thisEntity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(thisEntity, authoring);
            AddComponent(thisEntity, new WorldRenderBounds { Value = authoring.bounds.ToAABB() });
        }
    }
}