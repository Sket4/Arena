using Unity.Entities;
using UnityEngine;

namespace TzarGames.Renderer.Baking
{
    public class ParticleSystemBaker : Baker<ParticleSystem>
    {
        public override void Bake(ParticleSystem authoring)
        {
            var renderer = GetComponent<ParticleSystemRenderer>();

            if(renderer == null)
            {
                Debug.LogError($"Null particle system renderer on {authoring.name}");
                return;
            }

            foreach(var mat in renderer.sharedMaterials)
            {
                if(mat == null)
                {
                    Debug.LogError($"Failed to bake particle system {authoring.name}, because it has null or missing material");
                    return;
                }
            }

            var thisEntity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(thisEntity, authoring);
            AddComponentObject(thisEntity, renderer);
        }
    }
}