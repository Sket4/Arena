using Arena;
using Arena.Client.UI;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    struct UserInterfaceData : IComponentData
    {
        public Entity Entity;
    }
    
    [DisableAutoCreation]
    public partial class UISystem : SystemBase
    {
        public GameObject UiPrefab { get; private set; }

        public bool TryGetSingleton<T>(out T singletonData) where T : unmanaged, IComponentData
        {
            return SystemAPI.TryGetSingleton(out singletonData);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            UiPrefab = ClientGameSettings.Get.GameUiPrefab;

            var simSystem = World.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelHitUISystem>());
            simSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelUI_ItemTakeEventSystem>());
            
            var presentSystem = World.GetOrCreateSystemManaged<PresentationSystemGroup>();
            presentSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelCharacterLabelUiSystem>());
            presentSystem.AddSystemToUpdateList(World.CreateSystemManaged<UIEventHandlerSystem>());
        }

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, GameUI ui) =>
            {
                if (EntityManager.Exists(ui.OwnerEntity) == false)
                {
                    Debug.Log("Destroying UI object");
                    EntityManager.DestroyEntity(entity);
                    if(ui != null && ui && ui.gameObject != null && ui.gameObject)
                    {
                        Object.Destroy(ui.gameObject.transform.root.gameObject);
                    }
                    
                    return;
                }
                ui.OnSystemUpdate(this);
                
            }).Run();

            Entities
                .WithChangeFilter<ActivatedState>()
                .WithoutBurst()
                .ForEach((in Item item) =>
                {
                    if(EntityManager.HasComponent<UserInterfaceData>(item.Owner) == false)
                    {
                        return;
                    }
                    var uiData = EntityManager.GetComponentData<UserInterfaceData>(item.Owner);
                    var ui = EntityManager.GetComponentObject<GameUI>(uiData.Entity);
                    
                    Debug.Log($"item {item.ID} changed, updating inventory");
                    ui.UpdateInventory();

                }).Run();

            Entities
                .WithNone<UserInterfaceData>()
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity playerCharacterEntity, ref PlayerController controller) =>
            {
                var player = EntityManager.GetComponentData<Player>(controller.Value);

                if (player.ItsMe)
                {
                    var uiInstance = Object.Instantiate(UiPrefab);

                    var ui = uiInstance.GetComponentInChildren<GameUI>();

                    if (ui != null)
                    {
                        var uiEntity = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(playerCharacterEntity, new UserInterfaceData { Entity = uiEntity });
                        Debug.Log($"UI entity {uiEntity.Index} created");
                        ui.transform.root.name += $" ({uiEntity})";
#if UNITY_EDITOR
                        EntityManager.SetName(uiEntity, "Player UI");
#endif
                        EntityManager.AddComponentObject(uiEntity, ui);

                        ui.Setup(playerCharacterEntity, uiEntity, World.EntityManager);
                        ui.InitializeUiEntity(uiEntity, World.EntityManager);
                        return;
                    }
                }
            }).Run();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Entities
                .WithoutBurst()
                .ForEach((GameUI ui) =>
            {
                if (ui)
                {
                    Debug.Log($"Destroying {ui.transform.root.name}");
                    Object.Destroy(ui.transform.root.gameObject);    
                }
                
            }).Run();
        }
    }

    [DisableAutoCreation]
    partial class UIEventHandlerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((in LevelUpEventData levelUp) =>
            {
                if(SystemAPI.HasComponent<UserInterfaceData>(levelUp.Target) == false)
                {
                    return;
                }

                var uiEntity = SystemAPI.GetComponent<UserInterfaceData>(levelUp.Target).Entity;
                var ui = EntityManager.GetComponentObject<GameUI>(uiEntity);
                ui.ShowLevelUpAlert();

            }).Run();
        }
    }
}
