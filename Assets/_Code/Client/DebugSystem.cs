using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Client;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DebugSystem : SystemBase
    {
        [ConsoleCommand]
        public void GetNetIdEntity(int netid)
        {
            Entity result = Entity.Null;

            Entities.ForEach((Entity entity, in NetworkID networkID)=>
            {
                if(networkID.ID == netid)
                {
                    result = entity;
                }
            }).Run();

            Debug.Log($"{result} {World.Name}");
        }

        [ConsoleCommand]
        public void KillAllEnemiesServer()
        {
            if (World.GetExistingSystemManaged<ClientSystem>() != null)
            {
                return;
            }
            killAllEnemies();
        }

        void killAllEnemies()
        {
            Debug.Log($"Killing all enemies in world {World.Name}");
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            Entities
                .WithAll<Enemy>()
                .ForEach((Entity entity, in Health hp)=>
                {
                    var hitEntity = ecb.CreateEntity();
                    ecb.AddComponent(hitEntity, new Hit
                    {
                        Target = entity,
                        Normal = math.up()
                    });
                    ecb.AddComponent(hitEntity, new Damage
                    {
                        BaseValue = hp.ActualHP * 2,
                        Value = hp.ActualHP * 2
                    });

                }).Run();
            
            ecb.Playback(EntityManager);
        }
        
        [ConsoleCommand]
        public void LogRenderingInfo()
        {
            var renderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
            renderingSystem.LogInfo();
        }
        
        #if UNITY_EDITOR
        [ConsoleCommand]
        public void RenderChunkBounds()
        {
            var system = World.GetExistingSystemManaged<EditorDebugSystem>();
            system.RenderChunkBounds = !system.RenderChunkBounds;
        }
        
        [ConsoleCommand]
        public void RenderWorldBounds()
        {
            var system = World.GetExistingSystemManaged<EditorDebugSystem>();
            system.RenderWorldBounds = !system.RenderWorldBounds;
        }
        #endif
        
        [ConsoleCommand]
        public void ToggleAbilityDebugLogging()
        {
            CharacterAbilityStateSystem.EnableDebugLogging = !CharacterAbilityStateSystem.EnableDebugLogging;
            Debug.Log($"Ability debug logging set to {CharacterAbilityStateSystem.EnableDebugLogging}");
        }

        [ConsoleCommand]
        public void TriggerLightProbeUpdate()
        {
            Entities
                .WithAll<LightProbeInterpolation>()
                .WithNone<LocalTransform>()
                .ForEach((ref LocalToWorld l2w) =>
                {
                    l2w.Value = l2w.Value;
                }).Run();
            
            Entities
                .WithAll<LightProbeInterpolation>()
                .ForEach((ref LocalTransform lt) =>
                {
                    lt.Position = lt.Position;
                }).Run();
        }
        
        [ConsoleCommand]
        public void TriggerRenderBoundsUpdate()
        {
            Entities
                .ForEach((ref LocalRenderBounds bounds) =>
                {
                    bounds.Value = bounds.Value;
                    
                }).Run();
        }

        #if UNITY_EDITOR
        [ConsoleCommand]
        public void ToggleLightProbeDebug()
        {
            var renderingSystem = World.GetExistingSystemManaged<EditorDebugSystem>();
            renderingSystem.ShowLightProbes = !renderingSystem.ShowLightProbes;
        }
        #endif

        protected override void OnUpdate()
        {
        }
    }
}

