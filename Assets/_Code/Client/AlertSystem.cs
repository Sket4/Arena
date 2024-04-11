using Arena.Client.UI;
using Unity.Entities;
using TzarGames.GameCore;

namespace Arena.ArenaGame
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    public partial class AlertSystem : SystemBase
    {
        struct MatchAlertState : IComponentData
        {
            public ArenaMatchStateData Data;
        }

        AlertUI getUI()
        {
            return UnityEngine.Object.FindObjectOfType<AlertUI>();
        }

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, ref ArenaMatchStateData matchData) =>
            {
                if(EntityManager.HasComponent<MatchAlertState>(entity) == false)
                {
                    var alertUi = getUI();

                    if(alertUi != null)
                    {
                        EntityManager.AddComponentData(entity, new MatchAlertState { Data = matchData });
                        showMessage(alertUi, matchData);
                    }
                    return;
                }

                var state = EntityManager.GetComponentData<MatchAlertState>(entity);
                if(state.Data.CurrentStage != matchData.CurrentStage)
                {
                    var ui = getUI();

                    if(ui != null)
                    {
                        showMessage(ui, matchData);
                        state.Data = matchData;
                        EntityManager.SetComponentData(entity, state);
                    }
                }
            }).Run();
        }

        void showMessage(AlertUI ui, ArenaMatchStateData data)
        {
            ui.Show($"Уровень {data.CurrentStage}");
        }
    }
}
