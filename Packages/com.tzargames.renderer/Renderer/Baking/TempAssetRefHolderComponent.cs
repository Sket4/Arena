using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using UnityEngine;

namespace TzarGames.Renderer
{
    /// <summary>
    /// TODO временное решение, для сериализации материалов и мешей в билде. 
    /// Убрать, когда в RenderInfo исправится сериализация WeakObjectReference
    /// </summary>
    [System.Serializable]
    internal struct TempAssetReferenceElement : IBufferElementData
    {
        public UntypedWeakReferenceId Value;
    }

    public class TempAssetRefHolderComponent : MonoBehaviour
    {
        class Baker : Baker<TempAssetRefHolderComponent>
        {
            public override void Bake(TempAssetRefHolderComponent authoring)
            {
                var thisEntity = GetEntity(TransformUsageFlags.None);
                AddBuffer<TempAssetReferenceElement>(thisEntity);
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    partial class TempAssetReferenceCleanup : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .WithChangeFilter<TempAssetReferenceElement>()
                .ForEach((Entity entity, DynamicBuffer<TempAssetReferenceElement> refs) =>
            {
                if(refs.Length == 0)
                {
                    return;
                }
                Debug.Log("CLEANING TEMP REFS");

                refs.Clear();

            }).Run();
        }
    }
}

