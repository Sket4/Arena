using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using Unity.Entities;

namespace Arena.Server
{
    [DisableAutoCreation]
    public partial class TestMatchSystem : GameplayStateSystemBase
    {
        GameServerLoop gameServer;

        public TestMatchSystem(GameServerLoop server)
        {
            gameServer = server;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            RegisterState<WaitingForNewPlayer>();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var commands = Commands;

            Entities.ForEach((Entity entity, ref PlayerDataLoadRequest request) => 
            {
                if(request.State == PlayerDataRequestState.Pending || request.State == PlayerDataRequestState.Running)
                {
                    return;
                }

                commands.RemoveComponent<PlayerDataLoadRequest>(entity);

                if(request.State == PlayerDataRequestState.Failed)
                {   
                    return;
                }

                //var characterData = EntityManager.GetComponentData<CharacterInfo>(entity);
                //UnityEngine.Debug.Log("Character data loaded " + characterData.XP);

                UnityEngine.Debug.Log("Player data loaded");
            }).Run();
        }

        protected class WaitingForNewPlayer : WaitingForPlayers
        {
            protected override void OnPlayerAuthorized(Entity playerEntity, PlayerId userId)
            {
                base.OnPlayerAuthorized(playerEntity, userId);
                Commands.AddComponent(playerEntity, new PlayerDataLoadRequest());
            }
        }


        //protected class ReadyToStart : BaseState<MatchState_ReadyToStart>
        //{
        //    EntityCommandBuffer commands;

        //    public override void OnBeforeUpdate()
        //    {
        //        base.OnBeforeUpdate();
        //        commands = System.PostUpdateCommands;
        //    }

        //    public override void OnEnter(Entity entity, ref MatchState_ReadyToStart state)
        //    {
        //        base.OnEnter(entity, ref state);

        //        var players = System.EntityManager.GetBuffer<PlayerInMatchElement>(entity);

        //        for(int i=0; i<players.Length; i++)
        //        {
        //            var player = players[i];
        //            commands.AddComponent(player.PlayerEntity, new ReadyForMatch());
        //        }

        //        RequestStateChange<TestMatchState_Running>(entity);
        //    }
        //}

        //public struct TestMatchState_Running : IComponentData
        //{
        //}

        //class Running : BaseState<TestMatchState_Running>
        //{
        //}

        //class FailedToStartEx : FailedToStart
        //{
        //    public override void OnEnter(Entity entity, ref MatchState_FailedToStart state)
        //    {
        //        base.OnEnter(entity, ref state);
        //        UnityEngine.Debug.Log("Shutting down match failed");
        //        (System as TestMatchSystem).gameServer.PendingShutdown = true;
        //    }
        //}
    }
}
