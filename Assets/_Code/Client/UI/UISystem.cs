using System.Collections.Generic;
using Arena.Client.Baking;
using Arena.Client.UI;
using Arena.Dialogue;
using Arena.ScriptViz;
using TzarGames.GameCore;
using TzarGames.GameCore.Items;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace Arena.Client
{
    [System.Serializable]
    struct UserInterfaceData : IComponentData
    {
        public Entity Entity;
    }
    
    [DisableAutoCreation]
    [UpdateAfter(typeof(DialogueSystem))]
    public partial class UISystem : SystemBase
    {
        public GameObject UiPrefab { get; private set; }
        private EntityQuery uiQuery;
        private EntityQuery dialogueQuery;
        private EntityQuery messageRequestQuery;
        private EntityQuery hideMessageRequestQuery;
        private EntityQuery localizedEntityQuery;

        public static readonly Message OpenConfirmExitMessage = Message.CreateFromString("open confirm exit");
        public static readonly Message MapDisableMessage = Message.CreateFromString("disable map");
        public static readonly Message UI_CreatedMessage = Message.CreateFromString("ui created");

        public bool TryGetSingleton<T>(out T singletonData) where T : unmanaged, IComponentData
        {
            return SystemAPI.TryGetSingleton(out singletonData);
        }

        public bool TryGetSingletonEntity<T>(out Entity entity) where T : unmanaged, IComponentData
        {
            return SystemAPI.TryGetSingletonEntity<T>(out entity);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            UiPrefab = ClientGameSettings.Get.GameUiPrefab;

            uiQuery = GetEntityQuery(ComponentType.ReadOnly<GameUI>());

            var simSystem = World.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelHitUISystem>());
            simSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelUI_ItemTakeEventSystem>());
            simSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelUI_CommonSystem>());
            
            var presentSystem = World.GetOrCreateSystemManaged<PresentationSystemGroup>();
            presentSystem.AddSystemToUpdateList(World.CreateSystemManaged<FloatingLabelCharacterLabelUiSystem>());
            presentSystem.AddSystemToUpdateList(World.CreateSystemManaged<UIEventHandlerSystem>());
            
            LocalizationSettings.SelectedLocaleChanged += LocalizationSettingsOnSelectedLocaleChanged;
        }

        private void LocalizationSettingsOnSelectedLocaleChanged(Locale obj)
        {
            Debug.Log("updating scene localized entities");
            localizedEntityQuery.ResetFilter();
            
            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref localizedEntityQuery)
                .ForEach((LocalizeStringEvent loc) =>
                {
                    // trigger reset
                }).Run();
        }

        static async void loadLocalizationForText(TMPro.TextMeshPro text, LocalizedString str)
        {
            var result = await str.GetLocalizedStringAsync().Task;
            if (text)
            {
                text.text = result;
            }
        }

        protected override void OnUpdate()
        {
            localizedEntityQuery.SetChangedVersionFilter(ComponentType.ReadOnly<LocalizeStringEvent>());
            
            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref localizedEntityQuery)
                .ForEach((LocalizeStringEvent loc, TMPro.TextMeshPro tmpro, in TextMeshProData data) =>
                {
                    Debug.Log($"resetting localized entity {loc.name}");
                    loc.RefreshString();
                    loadLocalizationForText(tmpro, loc.StringReference);
                    var bounds = data.Bounds;
                    bounds.center = tmpro.transform.position;
                    tmpro.renderer.bounds = bounds;

                }).Run();
            
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
                .WithoutBurst()
                .WithStructuralChanges()
                .WithChangeFilter<DialogueState>()
                .ForEach((in DialogueState state) =>
                {
                    if (state.DialogueEntity != Entity.Null)
                    {
                        return;
                    }
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        return;
                    }

                    if (ui.DialogueUI.IsVisible)
                    {
                        ui.ShowDialogueWindow(false);
                    }
                    
                }).Run();

            Entities
                .WithoutBurst()
                .WithChangeFilter<AbilityPointChangedEvent>().ForEach(() =>
            {
                if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                {
                    return;
                }

                ui.HUD.OnAbilityPointsChanged();
                ui.CharacterUI.UpdateAbilityNotification();
                
            }).Run();
            
            Entities
                .WithStoreEntityQueryInField(ref dialogueQuery)
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in DynamicBuffer<DialogueAnswer> answers, in DialogueMessage message) =>
                {
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        Debug.LogError("Failed to find UI entity");
                        return;
                    }

                    showDialogueMessageAsync(message, answers, ui);
                }).Run();
            
            EntityManager.DestroyEntity(dialogueQuery);

            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref messageRequestQuery)
                .ForEach((Entity entity, in ShowMessageRequest showMessageRequest) =>
                {
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        Debug.LogError("Failed to find UI entity");
                        return;
                    }
                    var localizedMessage = LocalizationSettings.StringDatabase.GetLocalizedString(showMessageRequest.LocalizedMessageID);

                    if (EntityManager.HasComponent<MessageNumbers>(entity))
                    {
                        var messageNumbers = EntityManager.GetComponentData<MessageNumbers>(entity);
                        localizedMessage = localizedMessage.Replace("{numA}", messageNumbers.NumberA.ToString());
                        localizedMessage = localizedMessage.Replace("{numB}", messageNumbers.NumberB.ToString());
                    }

                    localizedMessage = localizedMessage.Trim();
                    
                    ui.HUD.Notifications.AddConstantNotification(localizedMessage, showMessageRequest.ID.ToString());
                    ui.ShowAlert(localizedMessage);

                })
                .Run();
            
            EntityManager.DestroyEntity(messageRequestQuery);
            
            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref hideMessageRequestQuery)
                .ForEach((in HideMessageRequest showMessageRequest) =>
                {
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        Debug.LogError("Failed to find UI entity");
                        return;
                    }
                    ui.HUD.Notifications.RemoveConstantNotificationById(showMessageRequest.MessageID.ToString());

                })
                .Run();
            
            Entities
                .WithoutBurst()
                .WithChangeFilter<ItemUsageSystem.TimeModificator>()
                .ForEach((Entity entity, in Target target) =>
                {
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        Debug.LogError("Failed to find UI entity");
                        return;
                    }

                    if (ui.OwnerEntity != target.Value)
                    {
                        return;
                    }
                    
                    ui.HUD.SetFoodIndicatorEntity(entity);

                })
                .Run();
            
            EntityManager.DestroyEntity(hideMessageRequestQuery);

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
                        EntityManager.AddBuffer<IncomingMessages>(uiEntity);

                        var listenerRequestEntity = EntityManager.CreateEntity();
                        
                        var requests = EntityManager.AddBuffer<RegisterListenerRequestMessage>(listenerRequestEntity);
                        requests.Add( new RegisterListenerRequestMessage
                        {
                            Listener = uiEntity,
                            MessageToListen = OpenConfirmExitMessage
                        });
                        requests.Add( new RegisterListenerRequestMessage
                        {
                            Listener = uiEntity,
                            MessageToListen = MapDisableMessage
                        });

                        var uiCreatedMessageEntity = EntityManager.CreateEntity(typeof(Message));
                        EntityManager.SetComponentData(uiCreatedMessageEntity, UI_CreatedMessage);
                    }
                }
            }).Run();
            
            Entities
                .WithoutBurst()
                .ForEach((in DynamicBuffer<IncomingMessages> messages) =>
            {
                if (IncomingMessages.Contains(messages, OpenConfirmExitMessage))
                {
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        Debug.LogError("Failed to find UI entity");
                        return;
                    }
                    ui.ShowGameplayMenuWithMode(true, true);
                }

                if (IncomingMessages.Contains(messages, MapDisableMessage))
                {
                    if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                    {
                        Debug.LogError("Failed to find UI entity");
                        return;
                    }
                    Debug.Log("disabling minimap by request");
                    ui.HUD.EnableMinimap(false);
                }
            }).Run();

            foreach (var (_, entity) 
                     in SystemAPI.Query<RefRO<PlayerAbilities>>().WithChangeFilter<PlayerAbilities>().WithEntityAccess())
            {
                var playerController = EntityManager.GetComponentData<PlayerController>(entity);
                var player = EntityManager.GetComponentData<Player>(playerController.Value);

                if (player.ItsMe == false)
                {
                    return;
                }
                
                if(uiQuery.TryGetSingleton<GameUI>(out var ui) == false)
                {
                    Debug.LogError("Failed to find UI entity");
                    return;
                }

                var panel = ui.HUD.GetComponentInChildren<SkillPanelUI>();
                if (panel)
                {
                    panel.UpdateData();
                }
            }
        }

        private static async void showDialogueMessageAsync(DialogueMessage message, DynamicBuffer<DialogueAnswer> answers, GameUI ui)
        {
            var loadTask = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(message.LocalizedStringID).Task;
            await loadTask;

            var answerList = new List<DialogueAnswerData>();

            foreach (var answer in answers)
            {
                var localizedAnswerTask = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(answer.LocalizedStringID).Task;
                await localizedAnswerTask;
                
                answerList.Add(new DialogueAnswerData
                {
                    Text = localizedAnswerTask.Result,
                    CommandAddress = answer.AnswerAddress
                });
            }

            if (ui == false)
            {
                return;
            }

            Texture2D image = null;

            if (ui.EntityManager.HasComponent<DialogueIcon>(message.Companion))
            {
                var icon = ui.EntityManager.GetComponentData<DialogueIcon>(message.Companion);
                image = await DialogueIcon.LoadIcon(icon.Value);
            }
            
            ui.ShowDialogueWindow(true);
            ui.DialogueUI.ShowDialogue(message.Player, message.DialogueEntity, loadTask.Result, image, answerList);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            LocalizationSettings.SelectedLocaleChanged -= LocalizationSettingsOnSelectedLocaleChanged;
            
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
