using System.Text;
using Arena.ScriptViz;
using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MatchFramework;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Client;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Arena.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DebugSystem : SystemBase
    {
        [ConsoleCommand]
        public void Refl()
        {
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ReflectionProbe probe)=>
                {
                    var hideFlag = ~(HideFlags.HideInHierarchy | HideFlags.NotEditable);
                    probe.hideFlags = probe.hideFlags & hideFlag;
                    probe.gameObject.hideFlags = probe.hideFlags;
                    
                    Debug.Log($"go hide flags: {probe.hideFlags}");
                    Debug.Log($"scene: {probe.gameObject.scene.name} is subscene: {probe.gameObject.scene.isSubScene}, loaded: {probe.gameObject.scene.isLoaded}");

                }).Run();
        }
        
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
        public static void GenerateEncryptKey()
        {
            var key = new AsymmetricEncryptionKey();
            var bytes = key.ConvertToByteArray(true);
            var sb = new StringBuilder();

            foreach (var b in bytes)
            {
                sb.Append(b);
                sb.Append(',');
            }
            Debug.Log(sb.ToString());
        }

        [ConsoleCommand]
        public static void EnableDarkMode(bool darkMode)
        {
            DGX.SRP.RenderPipeline.EnableDarkMode(darkMode);
        }

        [ConsoleCommand]
        public void LogSceneTagAndLinked()
        {
            Entities
                .WithAll<SceneTag, LinkedEntityGroup>()
                .ForEach((Entity entity) =>
            {
                Debug.Log($"{entity.Index}:{entity.Version}");
            }).Run();
        }

        [ConsoleCommand]
        public void ResetLocalizedEntities()
        {
            Entities
                .WithoutBurst()
                .ForEach((LocalizeStringEvent loc, TMPro.TextMeshPro tmpro) =>
            {
                Debug.Log($"resetting localized entity {loc.name}");
                loc.RefreshString();
                var localized = loc.StringReference.GetLocalizedString();
                tmpro.text = localized;
                
            }).Run();
        }

        #if UNITY_EDITOR
        [ConsoleCommand]
        void hp()
        {
            Entities
                .ForEach((ref Health hp, in PlayerController pc) =>
                {
                    var player = SystemAPI.GetComponent<Player>(pc.Value);
                    
                    if (player.ItsMe == false)
                    {
                        return;
                    }

                    hp.ActualHP = float.MaxValue;
                    hp.HP = float.MaxValue;
                    hp.ModifiedHP = float.MaxValue;
                }).Run();
        }
        
        [UnityEditor.MenuItem("Arena/Утилиты/Перенести персонажа к камере сцены _F11")]
        static void movePlayerToSceneCamera()
        {
            foreach (var world in Unity.Entities.World.All)
            {
                var system = world.GetExistingSystemManaged<DebugSystem>();
                if (system != null)
                {
                    system.MovePlayerToSceneCamera();
                }
            }
        }
        
        [ConsoleCommand]
        public void MovePlayerToSceneCamera()
        {
            var camera = UnityEditor.SceneView.lastActiveSceneView.camera;

            Entities
                .WithoutBurst()
                .ForEach((ref LocalTransform transform, in PlayerController pc) =>
                {
                    var player = EntityManager.GetComponentData<Player>(pc.Value);
                    if (player.ItsMe == false)
                    {
                        return;
                    }
                    transform.Position = camera.transform.position;
            }).Run();
        }
        #endif

        [ConsoleCommand]
        public void KillAllEnemiesServer()
        {
            if (World.GetExistingSystemManaged<ClientSystem>() != null)
            {
                return;
            }
            killAllEnemies();
        }

        [ConsoleCommand]
        public void SetQuestState(int questId, string questState)
        {
            if (System.Enum.TryParse<QuestState>(questState, out var state) == false)
            {
                Debug.LogError($"invalid state {state}");
                return;
            }
            
            var entityRequest = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(entityRequest, new AddGameProgressQuestRequest
            {
                QuestKey = questId,
                State = state 
            });
        }

        [ConsoleCommand]
        public void SetGameProgressValue(int key, int value)
        {
            var entityRequest = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entityRequest, new SetGameProgressKeyRequest
            {
                Key = key,
                Value = value
            });
        }

        [ConsoleCommand]
        public void SetQuestLevel(int value)
        {
            SetGameProgressValue(3, value);
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
        
        [ConsoleCommand]
        public void ToggleAffectedByLightProbeDebug()
        {
            var renderingSystem = World.GetExistingSystemManaged<EditorDebugSystem>();
            renderingSystem.ShowAffectedByLightProbes = !renderingSystem.ShowAffectedByLightProbes;
        }
        #endif

        protected override void OnUpdate()
        {
            // Entities
            //     .WithoutBurst()
            //     .ForEach((MeshRenderer renderer, TMPro.TextMeshPro tmpro) =>
            // {
            //     var bounds = renderer.bounds;
            //     var min = bounds.min;
            //     var max = bounds.max;
            //     Debug.DrawLine(min, new Vector3(min.x, min.y, max.z));
            //     Debug.DrawLine(min, new Vector3(min.x, max.y, min.z));
            //     Debug.DrawLine(min, new Vector3(max.x, min.y, min.z));
            //     Debug.DrawLine(max, new Vector3(max.x, max.y, min.z));
            //     Debug.DrawLine(max, new Vector3(max.x, min.y, max.z));
            //     Debug.DrawLine(max, new Vector3(min.x, max.y, max.z));
            //     
            // }).Run();
        }   
    }
}

