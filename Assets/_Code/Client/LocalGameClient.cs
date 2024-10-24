using TzarGames.GameCore;
using TzarGames.MatchFramework.Server;
using Unity.Entities;

namespace Arena.Client
{
    public class LocalGameClient : GameLoopBase
    {
        PresentationSystemGroup presentationSystemGroup;

        public LocalGameClient(string name, ClientGameSettings gameSettings, Hash128[] additionalScenes) : base(name)
        {
            InitSceneLoading(additionalScenes);

            MatchBaseSystem.WaitPlayerTimeoutEnabled = false;
            Utils.AddSharedSystems(this, true, "Client");
            presentationSystemGroup = World.GetOrCreateSystemManaged<PresentationSystemGroup>();
            GameLoopUtils.AddSystems(this, false, true, gameSettings);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            presentationSystemGroup.Update();
        }
    }
}
