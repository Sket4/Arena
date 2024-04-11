using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena
{
    [System.Serializable]
    public struct AimHitPoint : IComponentData
    {
        public float3 Value;
    }


    [System.Serializable]
    public struct RotateToAimHitPoint : IComponentData, IAbilityComponentJob<RotateToAimHitPointAbilityComponentJob>
    {
        byte fakeValue;
    }

    public class RotateToAimHitPointAbilityComponent : ComponentDataBehaviour<RotateToAimHitPoint>
    {
    }

    public struct RotateToAimHitPointAbilityComponentJob
    {
        [ReadOnly]
        public ComponentLookup<AimHitPoint> AimHitPointFromEntity;

        [MethodPriority(AbilitySystem.DefaultLowPriority)]
        public void OnStarted(in AbilityOwner abilityOwner, ref LocalTransform transfom)
        {
            process(in abilityOwner, ref transfom);
        }

        [MethodPriority(AbilitySystem.DefaultLowPriority)]
        public void OnUpdate(in AbilityOwner abilityOwner, ref LocalTransform transform)
        {
            process(in abilityOwner, ref transform);
        }

        void process(in AbilityOwner abilityOwner, ref LocalTransform transform)
        {
            var aimHitPoint = AimHitPointFromEntity[abilityOwner.Value];
            var dir = aimHitPoint.Value - transform.Position;
            dir = math.normalizesafe(dir, -math.up());
            transform.Rotation = quaternion.LookRotation(dir, math.up());
        }
    }
}
