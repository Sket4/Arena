using Unity.Entities;
using TzarGames.MultiplayerKit;
using TzarGames.GameCore.Server;
using TzarGames.GameCore;

namespace Arena.Server
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class ArenaMatchNetSystem : SystemBase
    {
        GameCommandBufferSystem commandSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetOrCreateSystemManaged<GameCommandBufferSystem>();
        }
        protected override void OnUpdate() 
		{
            var commands = commandSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithAll<ArenaMatchStateData>()
                .WithNone<NetworkID>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
            {
                commands.AddComponent(entityInQueryIndex, entity, new NetworkID());
            }).Schedule();
            
            commandSystem.AddJobHandleForProducer(Dependency);
		}
    }
}
