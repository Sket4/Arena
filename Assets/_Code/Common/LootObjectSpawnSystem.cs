using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class LootObjectSpawnSystem : GameSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<SpawnPointObjectPrefabReference>();
        }

        protected override void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();

            Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<SpawnPointObjectPrefabReference> prefabs, in LocalToWorld l2w) =>
            {
                commands.DestroyEntity(entityInQueryIndex, entity);

                var random = Random.CreateFromIndex((uint)entity.Index);
                var prefab = prefabs[random.NextInt(0, prefabs.Length)];

                var instance = commands.Instantiate(entityInQueryIndex, prefab.Prefab);

                commands.SetComponent(entityInQueryIndex, instance, LocalTransform.FromPositionRotation(l2w.Position, l2w.Rotation));
                
                //UnityEngine.Debug.DrawRay(l2w.Position, math.forward(l2w.Rotation), Color.magenta, 100);

                //UnityEngine.Debug.Log($"Spawning object on position {l2w.Position}");
            }).Run();
        }
    }
}
