using UnityEngine;
using Unity.Entities;

namespace Arena.Editor
{
    [TemporaryBakingType]
    struct CleanTag : IComponentData {}

    public class CleaningBaker : Baker<Transform>
    {
        public override void Bake(Transform authoring)
        {
            if (authoring.gameObject.CompareTag("EditorOnly"))
            {
                //Debug.Log($"EditorOnly {authoring.name}");
                AddComponent(new CleanTag());
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial class ConversionCleanSystem : SystemBase
    {
        EntityQuery cleanEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            cleanEntityQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]  { typeof(CleanTag) },
                Options = EntityQueryOptions.IncludePrefab
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(cleanEntityQuery);
        }
    }
}
