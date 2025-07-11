#if ARENA_TEST || UNITY_EDITOR
using Unity.Burst;
using Unity.Core;
using Unity.Entities;
using Unity.Transforms;

namespace TzarGames.GameCore.Tests
{
    //[DisableAutoCreation]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial struct TestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var commands = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new Job
            {
                Commands = commands,
                Time = SystemAPI.Time
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public TimeData Time;
            public EntityCommandBuffer.ParallelWriter Commands;
            
            public void Execute(Entity entity, [ChunkIndexInQuery] int index, in TestForwardBackMover mover, ref LocalTransform transform)
            {
                if(Time.ElapsedTime - mover.LastTime >= mover.MoveTime)
                {
                    var newMover = mover;
                    newMover.LastTime = Time.ElapsedTime;
                    newMover.Direction = -mover.Direction;
                    Commands.SetComponent(index, entity, newMover);
                }

                transform.Position += mover.Direction * Time.DeltaTime * mover.Speed;
            }
        }
    }
}
#endif