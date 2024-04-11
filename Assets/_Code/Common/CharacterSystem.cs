using System;
using TzarGames.GameCore;
using TzarGames.GameCore.RVO;
using TzarGames.GameCore.ScriptViz;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Physics;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(ApplyDamageSystem))]
    public partial class CharacterSystem : GameSystemBase
    {
        protected override unsafe void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();

            // отключаем коллайдеры у персонажей при их смерти
            Entities
                .WithChangeFilter<DeathData>()
                .WithNone<SkipDisableColliderOnDeath>()
                .WithAll<PhysicsWorldIndex>()
                .ForEach((Entity entity, in LivingState livingState) =>
                {
                    if(livingState.IsDead)
                    {
                        commands.RemoveComponent<PhysicsWorldIndex>(0, entity);
                    }
                }).Run();

            Entities.ForEach((ref Agent agent, in LivingState livingState) =>
            {
                if (livingState.IsDead)
                {
                    agent.IsDisabled = true;
                }
                else
                {
                    agent.IsDisabled = false;
                }

            }).Run();
        }
    }
}

