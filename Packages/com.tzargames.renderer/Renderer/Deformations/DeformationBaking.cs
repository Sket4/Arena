using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using UnityEngine;

namespace TzarGames.Renderer.Baking
{
    class SkinnedMeshRendererDeformBaker : Baker<SkinnedMeshRenderer>
    {
        public override void Bake(SkinnedMeshRenderer authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new RootBone
            {
                Entity = authoring.rootBone != null ? GetEntity(authoring.rootBone, TransformUsageFlags.Dynamic) : Entity.Null
            });

            var bones = authoring.bones;
            var meshBindPoses = authoring.sharedMesh.bindposes;

            AddBuffer<BoneArrayElement>(entity);
            AddBuffer<BindPoseMatrix>(entity);

            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];

                if(bone == null)
                {
                    Debug.LogError("Null bone at " + authoring.name);
                    continue;
                }

                var boneEntity = GetEntity(bone);

                AppendToBuffer(entity, new BoneArrayElement
                {
                    Entity = boneEntity
                });

                AppendToBuffer(entity, new BindPoseMatrix { Value = meshBindPoses[i] });
            }
        }
    }

    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial class DeformationConversion : SystemBase
    {
        protected override void OnUpdate()
        {
            using (var ecb = new EntityCommandBuffer(Allocator.Temp))
            {
                Entities
                    .WithAll<RootBone>()
                    .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((Entity entity, DynamicBuffer<BoneArrayElement> bones, in RootBone rootBone) =>
                {
                    if (HasBuffer<SkinMatrix>(entity) == false)
                    {
                        ecb.RemoveComponent<RootBone>(entity);
                        ecb.RemoveComponent<BoneArrayElement>(entity);
                        ecb.RemoveComponent<BindPoseMatrix>(entity);
                        return;
                    }

                }).Run();

                ecb.Playback(EntityManager);
            }
        }
    }
}

