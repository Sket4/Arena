using TzarGames.GameCore;
using TzarGames.GameCore.RVO;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(ApplyDamageSystem))]
    public partial class CharacterSystem : GameSystemBase
    {
        private const float minDamageFallHeight = 5;
        private const float fallDamageHeightRange = 10;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<ModifyHealthSystem.Singleton>();
        }

        protected override unsafe void OnSystemUpdate()
        {
            var commands = CreateCommandBuffer();
            var modifyHealthArchetype = SystemAPI.GetSingleton<ModifyHealthSystem.Singleton>().ModifyEventArchetype;
            
            Entities.ForEach((Entity entity, in KinematicCharacterBody body, in LocalTransform transform, in Falling fallingState, in Health hp) =>
            {
                if (body.IsGrounded)
                {
                    if (fallingState.IsInAir)
                    {
                        // check fall damage
                        var heightDiff = fallingState.FallingStartHeight - transform.Position.y;

                        if (heightDiff >= minDamageFallHeight)
                        {
                            var damageFactor = (heightDiff - minDamageFallHeight) / fallDamageHeightRange;
                            if (damageFactor > 1)
                            {
                                damageFactor = 1;
                            }

                            var damage = hp.ModifiedHP * damageFactor;

                            if (damage >= hp.ActualHP)
                            {
                                damage = hp.ActualHP - 1;
                            }

                            var damageRequest = commands.CreateEntity(modifyHealthArchetype);
                            
                            commands.SetComponent(damageRequest, new ModifyHealth
                            {
                                Value = -damage,
                                Mode = ModifyHealthMode.Add
                            });
                            commands.SetComponent(damageRequest, new Target(entity));
                            
                            Debug.Log($"Fall damage: {damage}, height diff: {heightDiff}");
                        }
                        
                        commands.SetComponent(entity, new Falling { FallingStartHeight = 0, IsInAir = false });
                    }
                }
                else
                {
                    if (fallingState.IsInAir)
                    {
                        if (transform.Position.y > fallingState.FallingStartHeight)
                        {
                            commands.SetComponent(entity, new Falling
                            {
                                IsInAir = true,
                                FallingStartHeight = transform.Position.y
                            });    
                        }
                    }
                    else
                    {
                        commands.SetComponent(entity, new Falling
                        {
                            IsInAir = true,
                            FallingStartHeight = transform.Position.y
                        });
                    }
                }
                
            }).Run();

            // отключаем коллайдеры у персонажей при их смерти
            Entities
                .WithChangeFilter<DeathData>()
                .WithNone<SkipDisableColliderOnDeath>()
                .WithAll<PhysicsWorldIndex>()
                .ForEach((Entity entity, in LivingState livingState) =>
                {
                    if(livingState.IsDead)
                    {
                        commands.RemoveComponent<PhysicsWorldIndex>(entity);
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

