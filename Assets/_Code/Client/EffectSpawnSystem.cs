using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    public partial class EffectSpawnSystem : GameSystemBase
    {
        struct StunEffectAddedTag : IComponentData
        {
        }

        struct StunEffectInstance : IComponentData
        {
            public Entity StunnedEntity;
        }

        EntityQuery levelQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            levelQuery = GetEntityQuery(ComponentType.ReadOnly<Level>());
            levelQuery.SetChangedVersionFilter(typeof(Level));
            RequireForUpdate<WaterEffects>();
        }

        protected override void OnSystemUpdate()
        {
            var commands = CommandBufferSystem.CreateCommandBuffer();
            var waterEffects = SystemAPI.GetSingleton<WaterEffects>();
            
            Entities.ForEach((in WaterEnterEvent enterEvent) =>
            {
                if (enterEvent.EnterSpeed >= WaterEnterEvent.EnterEventTreshold)
                {
                    var effectEntity = commands.Instantiate(waterEffects.SplashPrefab);
                    commands.SetComponent(effectEntity, LocalTransform.FromPosition(enterEvent.WaterPointLocation));
                }

                var rippleEffects = commands.Instantiate(waterEffects.RipplesPrefab);
                commands.SetComponent(rippleEffects, LocalTransform.FromPositionRotation(enterEvent.WaterPointLocation, enterEvent.EntityRotation));
                commands.SetComponent(rippleEffects, new Target(enterEvent.EnteredEntity));
                
                commands.SetComponent(enterEvent.EnteredEntity, new WaterRippleEffectInstance
                {
                    Instance = rippleEffects
                });

            }).Run();
            
            Entities.ForEach((in WaterExitEvent exitEvent) =>
            {
                var rippleEffectInstance = SystemAPI.GetComponent<WaterRippleEffectInstance>(exitEvent.EnteredEntity);
                
                if (rippleEffectInstance.Instance != Entity.Null)
                {
                    commands.SetComponent(exitEvent.EnteredEntity, new WaterRippleEffectInstance { Instance = Entity.Null });
                    commands.AddComponent(rippleEffectInstance.Instance, new DestroyTimer(3));
                    commands.SetComponent(rippleEffectInstance.Instance, new ParticleSystemEmissionState { Enabled = false });
                }

            }).Run();
            
            Entities
                .WithoutBurst()
                .WithNone<StunEffectAddedTag>().ForEach((Entity stunnedEntity, StunEffect effect, ref Stunned stunned, ref LocalTransform transform, ref Height height) =>
            {
                Debug.LogError("Not implemented");
                // var requestEntity = commands.CreateEntity();
                // commands.AddComponent(requestEntity, new InstantiateRequest { Prefab = effect.Prefab });
                // commands.AddComponent(requestEntity, new StunEffectInstance { StunnedEntity = stunnedEntity });
                // commands.AddComponent(requestEntity, new Translation { Value = math.up() * height + translation.Value });
                //
                // commands.AddComponent(stunnedEntity, new StunEffectAddedTag()); 
            }).Run();

            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref StunEffectInstance stunEffect) =>
                {
                    bool destroy = EntityManager.Exists(stunEffect.StunnedEntity) == false;

                    if(destroy == false && EntityManager.HasComponent<Stunned>(stunEffect.StunnedEntity) == false)
                    {
                        destroy = true;

                        commands.RemoveComponent<StunEffectAddedTag>(stunEffect.StunnedEntity);
                    }

                    if(destroy)
                    {
                        commands.DestroyEntity(entity);
                    }
                }).Run();
        }
    }

    [DisableAutoCreation]
    [UpdateBefore(typeof(EventCleanSystem))]
    partial class LevelUpEffectSpawnSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var commands = CommandBufferSystem.CreateCommandBuffer();
            
            Entities.ForEach((ref LevelUpEventData levelUpEvent) =>
            {
                var target = levelUpEvent.Target;

                if(SystemAPI.HasComponent<LevelUpEffect>(target) == false)
                {
                    return;
                }

                var effect = SystemAPI.GetComponent<LevelUpEffect>(target);

                var effectEntity = commands.Instantiate(effect.Prefab);

                commands.AddComponent(effectEntity, LocalTransform.Identity);
                commands.AddComponent(effectEntity, new Parent { Value = target });
            }).Run();
        }
    }
}
