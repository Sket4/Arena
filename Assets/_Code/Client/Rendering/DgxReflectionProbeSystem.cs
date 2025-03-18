using DGX.SRP;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena.Client.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(RenderingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DgxReflectionProbeSystem : SystemBase
    {
        private DGX.SRP.RenderPipeline pipeline;
        private EntityQuery reflectionProbeQuery;

        struct SystemData : IComponentData
        {
            public ulong LastReflectionProbeManagerVersion;
        }
        
        protected override void OnCreate()
        {
            base.OnCreate();
            EntityManager.AddComponent<SystemData>(SystemHandle);
            reflectionProbeQuery = GetEntityQuery(ComponentType.ReadOnly<ReflectionProbeData>());
            reflectionProbeQuery.SetChangedVersionFilter(ComponentType.ReadOnly<ReflectionProbeData>());
        }

        protected override void OnUpdate()
        {
            var data = EntityManager.GetComponentData<SystemData>(SystemHandle);

            if (pipeline == null || pipeline.IsValid == false)
            {
                pipeline = RenderPipelineManager.currentPipeline as DGX.SRP.RenderPipeline;
                if (pipeline == null)
                {
                    return;
                }
            }

            if (reflectionProbeQuery.IsEmpty 
                && pipeline.ReflectionProbeManager.Version == data.LastReflectionProbeManagerVersion)
            {
                return;
            }

            data.LastReflectionProbeManagerVersion = pipeline.ReflectionProbeManager.Version;
            EntityManager.SetComponentData(SystemHandle, data);
            
            foreach (var (probe, probeData) in SystemAPI.Query<
                         SystemAPI.ManagedAPI.UnityEngineComponent<ReflectionProbe>,
                         RefRW<ReflectionProbeData>
                     >())
            {
                ref var probeDataRW = ref probeData.ValueRW;
                var index = pipeline.ReflectionProbeManager.GetReflectionProbeIndex(probe.Value);
                if (index < 0)
                {
                    continue;
                    //Debug.LogError($"Failed to find index for probe {probe.Value}");
                }
                //Debug.Log($"Set reflection probe {probe.Value.name} index to {index}");
                probeDataRW.Index = (uint)index;
            }
        }
    }
}
