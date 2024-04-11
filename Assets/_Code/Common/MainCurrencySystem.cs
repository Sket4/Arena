using Arena.Items;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Mathematics;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class MainCurrencySystem : SystemBase
    {
        private GameCommandBufferSystem commandSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetOrCreateSystemManaged<GameCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var currentTime = math.max(World.Time.ElapsedTime, 1.0f) * 1000.0f;
            var commands = commandSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithChangeFilter<Droppable>()
                .ForEach((Entity entity, int entityInQueryIndex, ref Consumable consumable, in MainCurrencyDrop drop, in Droppable dropped) =>
            {
                if(dropped.IsDropped == false)
                {
                    return;
                }

                commands.RemoveComponent<MainCurrencyDrop>(entityInQueryIndex,entity);
                var seed = (uint)(entityInQueryIndex + currentTime);

                var random = new Random(seed);
                consumable.Count = random.NextUInt(drop.Min, drop.Max);
            }).Schedule();

            commandSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
