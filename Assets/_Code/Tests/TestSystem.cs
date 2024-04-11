#if UNITY_EDITOR
using Unity.Entities;
using Unity.Transforms;

namespace TzarGames.GameCore.Tests
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class TestSystem : SystemBase
    {
        private TimeSystem timeSystem;

        protected override void OnUpdate()
        {
            if (timeSystem == null)
            {
                timeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            }
            
            var time = timeSystem.GameTime;
            var delta = timeSystem.TimeDelta;
            
            Entities.ForEach((ref TestForwardBackMover mover, ref LocalTransform translation) =>
            {
                if(time - mover.LastTime >= mover.MoveTime)
                {
                    mover.LastTime = time;
                    mover.Direction = -mover.Direction;
                }

                translation.Position += mover.Direction * delta * mover.Speed;

            }).Run();
        }
    }
}
#endif