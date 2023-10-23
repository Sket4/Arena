using Unity.Burst;
using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace TzarGames.Renderer
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(RenderingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct SkinnedMeshDeformationSystem : ISystem
    {
        SkinningJob skinningJob;

        void OnCreate(ref SystemState state)
        {
            skinningJob = new SkinningJob
            {
                L2WLookup = state.GetComponentLookup<LocalToWorld>(true)
            };
        }

        public void OnUpdate(ref SystemState state)
        {
            skinningJob.L2WLookup.Update(ref state);
            state.Dependency = skinningJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct SkinningJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalToWorld> L2WLookup;

        public void Execute(ref DynamicBuffer<SkinMatrix> skinMatrices,
                in LocalToWorld thisL2W,
                in DynamicBuffer<BoneArrayElement> bones,
                in DynamicBuffer<BindPoseMatrix> bindPoses)
        {
            var rootMatrixInv = math.inverse(thisL2W.Value);

            for (int i = 0; i < bones.Length; ++i)
            {
                var bone = bones[i].Entity;

                var boneL2W = L2WLookup[bone];
                var boneMatRootSpace = math.mul(rootMatrixInv, boneL2W.Value);

                var bindPose = bindPoses[i];
                var skinMatRootSpace = math.mul(boneMatRootSpace, bindPose.Value);

                skinMatrices[i] = new SkinMatrix { Value = new float3x4(skinMatRootSpace.c0.xyz, skinMatRootSpace.c1.xyz, skinMatRootSpace.c2.xyz, skinMatRootSpace.c3.xyz) };
            }
        }
    }
}
