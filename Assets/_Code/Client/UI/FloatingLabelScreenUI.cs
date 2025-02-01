using System.Collections.Generic;
using Arena.Client;
using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameFramework;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;
using TzarGames.GameCore.Client;
using Unity.Mathematics;
using Arena;
using Arena.Items;
using Unity.Transforms;

namespace Arena.Client.UI
{
    public class FloatingLabelScreenUI : GameUIBase
    {
        [SerializeField] string inactiveContainerName = "___UI Floating Labels";
        [SerializeField] private RectTransform container = default;
        [SerializeField] private float showTime = 2;
        [SerializeField] private FloatingTextLabelUI criticalLabelPrefab = default;
        [SerializeField] private FloatingTextLabelUI hitLabelPrefab = default;
        [SerializeField] private FloatingTextLabelUI itemTakeLabelPrefab = default;
        [SerializeField] private FloatingTextLabelUI blockLabelPrefab = default;
        [SerializeField] private FloatingTextLabelUI playerLabelPrefab = default;
        [SerializeField] private float itemLabelRange = 2;
        [SerializeField] private Color defaultItemColor = Color.white;
        private int currentItemPos;
        
        [SerializeField]
        private LocalizedStringAsset critMessage = default;
        
        [SerializeField]
        private LocalizedStringAsset blockMessage = default;
        
        [System.NonSerialized]
        public Camera Camera = default;
        private Transform inactiveContainer;
        
        private Pool<LabelInfo> labelInfos;
        private Pool<FloatingTextLabelUI> commonLabelPool;
        private Pool<FloatingTextLabelUI> criticalLabelPool;
        private Pool<FloatingTextLabelUI> hitLabelPool;
        private Pool<FloatingTextLabelUI> itemLabelPool;
        private Pool<FloatingTextLabelUI> blockLabelPool;
        private Pool<FloatingTextLabelUI> playerLabelPool;

        private List<LabelInfo> activeLabels = new List<LabelInfo>();
        private List<PlayerLabelInfo> playerLabels = new List<PlayerLabelInfo>();
        private Dictionary<FloatingLabelBaseUI, IPool> labelsAndPools = new Dictionary<FloatingLabelBaseUI, IPool>();
       
        [System.Serializable]
        class LabelInfo
        {
            public float StartTime;
            public float Time;
            public Vector3 WorldPosition;
            public FloatingLabelBaseUI LabelUI;
        }

        [System.Serializable]
        class PlayerLabelInfo
        {
            public Entity Player = default;
            //public PlayerCharacter PlayerCharacter;
            public FloatingTextLabelUI LabelUI = default;
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            manager.AddComponentObject(uiEntity, this);
        }

        public void OnPlayerExit(Entity player)
        {
            for (var index = playerLabels.Count - 1; index >= 0; index--)
            {
                var label = playerLabels[index];
                if (label.Player == player)
                {
                    removePlayerLabel(label);
                    break;
                }
            }
        }

        //private void CharacterOnOnHitBlockedByShield(ICharacterDamageInfo characterDamageInfo)
        //{
        //    addBlockLabel(characterDamageInfo.Hit.Point);
        //}

        //private void CharacterOnOnHitOtherCharacter(ICharacterDamageInfo hitData)
        //{
        //    if (hitData.IsAuthoritative == false)
        //    {
        //        return;
        //    }

        //    if (hitData.IsCritical)
        //    {
        //        addCriticalLabel(hitData.Hit.Point, hitData.Damage);
        //    }
        //    else
        //    {
        //        addHitLabel(hitData.Hit.Point, hitData.Damage);
        //    }
        //}

        public void AddHitLabel(Vector3 worldPosition, float damage)
        {
            var label = hitLabelPool.Get();
            label.Text = "-" + ((int)damage);
            addLabel(worldPosition, label);
        }
        
        void addBlockLabel(Vector3 worldPosition)
        {
            var label = blockLabelPool.Get();
            label.Text = blockMessage;
            addLabel(worldPosition, label);
        }

        public void AddPlayerLabel(Entity playerEntity)
        {
            if(EntityManager.Exists(playerEntity) == false)
            {
                Debug.LogError("Failed to add player label - entity does not exist");
                return;
            }

            for (int i = 0; i < playerLabels.Count; i++)
            {
                var p = playerLabels[i];
                if(p.Player == playerEntity)
                {
                    return;
                }
            }

            var characterName = EntityManager.GetComponentData<Name30>(playerEntity);

            var label = playerLabelPool.Get();
            label.Text = characterName.ToString();
            
            var labelInfo = new PlayerLabelInfo();
            labelInfo.Player = playerEntity;
            label.Transform.SetParent(container);
            label.Transform.localScale = Vector3.one;
            labelInfo.LabelUI = label;
            
            playerLabels.Add(labelInfo);
            
            var position = EntityManager.GetComponentData<LocalTransform>(playerEntity);

            var screenPoint = Camera.WorldToScreenPoint(position.Position);
            label.Transform.position = screenPoint;
            
            label.Show();
        }

        public void RemovePlayerLabel(Entity entity)
        {
            for (var index = playerLabels.Count - 1; index >= 0; index--)
            {
                var label = playerLabels[index];

                if (label.Player == entity)
                {
                    removePlayerLabel(label);
                    break;
                }
            }
        }

        void removePlayerLabel(PlayerLabelInfo info)
        {
            var label = info.LabelUI;
            info.LabelUI = null;

            if (label != null)
            {
                var c = label.Color;
                c.a = 1;
                label.Color = c;
                label.Transform.SetParent(inactiveContainer);
                playerLabelPool.Set(label as FloatingTextLabelUI);
            }

            playerLabels.Remove(info);
        }

        void addLabel(Vector3 worldPosition, FloatingLabelBaseUI label)
        {
            var labelInfo = labelInfos.Get();
            labelInfo.StartTime = Time.time;
            labelInfo.Time = showTime;
            labelInfo.WorldPosition = worldPosition;
            label.Transform.SetParent(container);
            label.Transform.localScale = Vector3.one;
            labelInfo.LabelUI = label;
                
            activeLabels.Add(labelInfo);
            
            var screenPoint = Camera.WorldToScreenPoint(worldPosition);
            label.Transform.position = screenPoint;

            //if (Character.Connected)
            {
                label.Show();
            }
        }

        public void AddCriticalLabel(Vector3 worldPosition, float damage)
        {
            var label = criticalLabelPool.Get();
            label.Text = string.Format(critMessage, damage);
            addLabel(worldPosition, label);
        }

        private void OnDestroy()
        {
            if(inactiveContainer != null && inactiveContainer)
            {
                Destroy(inactiveContainer.gameObject);
            }
            
            foreach (var label in labelsAndPools)
            {
                if(label.Key == null || !label.Key)
                {
                    continue;
                }
                Destroy(label.Key.gameObject);
            }
        }
        
        protected override void Awake ()
        {
            base.Awake();

            var inactiveContainerObj = new GameObject(inactiveContainerName);
            inactiveContainerObj.SetActive(false);
            inactiveContainer = inactiveContainerObj.transform;
            
            labelInfos = new Pool<LabelInfo>(createLabelInfo, int.MaxValue);
		    labelInfos.CreateObjects(20);

            criticalLabelPool = new Pool<FloatingTextLabelUI>(createCriticalLabel, int.MaxValue);
            criticalLabelPool.CreateObjects(10);
            
            hitLabelPool = new Pool<FloatingTextLabelUI>(createHitlLabel, int.MaxValue);
            hitLabelPool.CreateObjects(20);
            
            itemLabelPool = new Pool<FloatingTextLabelUI>(createItemLabel, int.MaxValue);
            itemLabelPool.CreateObjects(10);
            
            blockLabelPool = new Pool<FloatingTextLabelUI>(createBlockLabel, int.MaxValue);
            blockLabelPool.CreateObjects(10);

            commonLabelPool = new Pool<FloatingTextLabelUI>(createCommonLabel, int.MaxValue);
            commonLabelPool.CreateObjects(10);  

            playerLabelPool = new Pool<FloatingTextLabelUI>(createPlayerLabel, int.MaxValue);
            playerLabelPool.CreateObjects(10);
        }

        LabelInfo createLabelInfo()
        {
            return new LabelInfo();
        }

        static T MakeInstance<T>(T prefab) where T : Object
        {
            return Instantiate(prefab);
        }

        FloatingTextLabelUI createCriticalLabel()
        {
            var label = MakeInstance(criticalLabelPrefab);
            label.Transform.SetParent(inactiveContainer);
            labelsAndPools.Add(label, criticalLabelPool);
            return label;
        }
        
        FloatingTextLabelUI createItemLabel()
        {
            var label = MakeInstance(itemTakeLabelPrefab);
            label.Transform.SetParent(inactiveContainer);
            labelsAndPools.Add(label, itemLabelPool);
            return label;
        }
        
        FloatingTextLabelUI createBlockLabel()
        {
            var label = MakeInstance(blockLabelPrefab);
            label.Transform.SetParent(inactiveContainer, false);
            labelsAndPools.Add(label, blockLabelPool);
            return label;
        }
        
        FloatingTextLabelUI createHitlLabel()
        {
            var label = MakeInstance(hitLabelPrefab);
            label.Transform.SetParent(inactiveContainer);
            labelsAndPools.Add(label, hitLabelPool);
            return label;
        }

        FloatingTextLabelUI createCommonLabel()
        {
            var label = MakeInstance(itemTakeLabelPrefab);
            label.Transform.SetParent(inactiveContainer, false);
            labelsAndPools.Add(label, commonLabelPool);
            return label;
        }

        FloatingTextLabelUI createPlayerLabel()
        {
            var label = MakeInstance(playerLabelPrefab);
            label.Transform.SetParent(inactiveContainer, false);
            labelsAndPools.Add(label, playerLabelPool);
            return label;
        }

        void removeLabelInfo(LabelInfo info)
        {
            activeLabels.Remove(info);
            var label = info.LabelUI;
            info.LabelUI = null;
            labelInfos.Set(info);
            
            label.Transform.SetParent(inactiveContainer);
            var pool = labelsAndPools[label];
            if (pool == criticalLabelPool)
            {
                criticalLabelPool.Set(label as FloatingTextLabelUI);
            }
            else if(pool == hitLabelPool)
            {
                hitLabelPool.Set(label as FloatingTextLabelUI);
            }
            else if(pool == blockLabelPool)
            {
                blockLabelPool.Set(label as FloatingTextLabelUI);
            }
            else if (pool == commonLabelPool)
            {
                commonLabelPool.Set(label as FloatingTextLabelUI);
            }
            else if(pool == itemLabelPool)
            {
                itemLabelPool.Set(label as FloatingTextLabelUI);
                currentItemPos--;
                if (currentItemPos < 0)
                {
                    currentItemPos = 0;
                    //Debug.LogError("Wrong position");
                }
            }
        }

        public void AddCommonLabel(string text, Color color, Vector3 position)
        {
            var label = commonLabelPool.Get();
            label.Text = text;
            label.Color = color;
            addLabel(position, label);
        }

        public void AddItemLabel(FloatingLabelScreenUI screen, float3 position, Entity itemEntity, uint count, EntityManager em)
        {
            var label = screen.itemLabelPool.Get();

            //label.Text = item.GetLocalizedName();
            // var colorAttribute = item.GetAttributeOfType<ItemColorAttribute>();
            // if (colorAttribute != null)
            // {
            //     label.Color = colorAttribute.Color;
            // }
            // else
            // {
            //     label.Color = defaultItemColor;
            // }

            if (em.HasComponent<MainCurrency>(itemEntity))
            {
                label.Color = Color.yellow;
                label.Text = count.ToString();
            }
            else
            {
                label.Text = EntityManager.GetSharedComponentManaged<ItemName>(itemEntity).ToString();
                label.Color = Color.white;
            }

            screen.addLabel(position, label);
        }

        public void DoUpdate()
        {
            if(Camera == null)
            {
                var tpc = FindObjectOfType<ThirdPersonCamera>();
                if(tpc != null)
                {
                    Camera = tpc.Camera;
                }
            }

            if(Camera == null)
            {
                return;
            }

            if(EntityManager == null || EntityManager.World.IsCreated == false)
            {
                return;
            }
            
            float deltaTime = Time.deltaTime;

            //if (Character.Connected)
            {
                for (var index = playerLabels.Count - 1; index >= 0; index--)
                {
                    var label = playerLabels[index];

                    if(label == null)
                    {
#if UNITY_EDITOR
                        Debug.LogError("null player label");
#endif
                        continue;
                    }

                    var playerCharacter = label.Player;
                    
                    if(EntityManager.Exists(playerCharacter) == false)
                    {
                        removePlayerLabel(label);
                        continue;
                    }

                    if(HasData<LocalTransform>(playerCharacter) == false)
                    {
                        continue;
                    }

                    var position = GetData<LocalTransform>(playerCharacter);

                    var screenPoint = Camera.WorldToScreenPoint(position.Position + math.up() * 2);

                    if (screenPoint.z < 0)
                    {
                        screenPoint.x = Camera.scaledPixelWidth - screenPoint.x;
                        screenPoint.y = Camera.scaledPixelHeight - screenPoint.y;

                        if(label.LabelUI.Graphic != null && label.LabelUI.Graphic.enabled)
                        {
                            label.LabelUI.Graphic.enabled = false;
                        }
                    }
                    else
                    {
                        if (label.LabelUI.Graphic != null && label.LabelUI.Graphic.enabled == false)
                        {
                            label.LabelUI.Graphic.enabled = true;
                        }
                    }

                    label.LabelUI.Transform.position = screenPoint;

                    // fading
                    //var c = label.LabelUI.Color;
                    //var fader = playerCharacter.Fader;

                    // if(fader != null && fader.IsFading)
                    // {
                    //     if(c.a > 0.0f)
                    //     {
                    //         c.a -= deltaTime * 2.0f;
                    //         if(c.a < 0.0f)
                    //         {
                    //             c.a = 0;
                    //         }
                    //     }
                    // }
                    // else
                    // {
                    //     if (c.a < 1.0f)
                    //     {
                    //         c.a += deltaTime * 2.0f;
                    //         if (c.a > 1.0f)
                    //         {
                    //             c.a = 1.0f;
                    //         }
                    //     }
                    // }
                    // label.LabelUI.Color = c;
                }
            }

            var activeCount = activeLabels.Count;
            if (activeCount == 0)
            {
                return;
            }

            var time = Time.time;
            for (int i = activeCount - 1; i >= 0; i--)
            {
                var label = activeLabels[i];
                if (time - label.StartTime >= label.Time)
                {
                    removeLabelInfo(label);
                    continue;
                }

                var screenPoint = Camera.WorldToScreenPoint(label.WorldPosition);
                
                if (screenPoint.z < 0)
                {
                    screenPoint.x = Camera.scaledPixelWidth - screenPoint.x;
                    screenPoint.y = Camera.scaledPixelHeight - screenPoint.y;

                    if (label.LabelUI.Graphic != null && label.LabelUI.Graphic.enabled)
                    {
                        label.LabelUI.Graphic.enabled = false;
                    }
                }
                else
                {
                    if (label.LabelUI.Graphic != null && label.LabelUI.Graphic.enabled == false)
                    {
                        label.LabelUI.Graphic.enabled = true;
                    }
                }

                label.LabelUI.Transform.position = screenPoint;
            }
        }
        
        #if UNITY_EDITOR
        [ConsoleCommand]
        void addCriticalLabel()
        {
            var rand = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
            var pos = GetData<LocalTransform>().Position + (float3)rand;
            
            AddCriticalLabel(pos, Random.Range(0, float.MaxValue));
        }
        
        [ConsoleCommand]
        public void AddHitLabel()
        {
            var rand = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
            var pos = GetData<LocalTransform>().Position + (float3)rand;
            
            AddHitLabel(pos, Random.Range(0, float.MaxValue));
        }
#endif
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(ApplyDamageSystem))]
    public partial class FloatingLabelHitUISystem : SystemBase
    {
        bool isNetworkedGame = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            isNetworkedGame = World.GetExistingSystemManaged<TzarGames.MultiplayerKit.Client.ClientSystem>() != null;
        }

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((FloatingLabelScreenUI screenUI) =>
                {
                    screenUI.DoUpdate();

                }).Run();

            Entities
                .WithoutBurst()
                .WithChangeFilter<Hit>()
                .ForEach((Entity entity, in Hit hit, in Damage damage) =>
                {
                    if(isNetworkedGame && HasComponent<CreatedByHitCreatorSystemTag>(entity))
                    {
                        return;
                    }

                    var instigator = hit.Instigator;

                    if (EntityManager.HasComponent<UserInterfaceData>(instigator) == false)
                    {
                        return;
                    }

                    //if (EntityManager.HasComponent<ReceivedHitElement>(hit.Target) == false)
                    //{
                    //    return;
                    //}

                    var ui = EntityManager.GetComponentData<UserInterfaceData>(instigator);
                    var screen = EntityManager.GetComponentObject<FloatingLabelScreenUI>(ui.Entity);

                    if (HasComponent<CriticalHitState>(entity) && GetComponent<CriticalHitState>(entity).IsCritical)
                    {
                        screen.AddCriticalLabel(hit.Position, damage.Value);
                    }
                    else
                    {
                        screen.AddHitLabel(hit.Position, damage.Value);
                    }

                }).Run();
        }
    }

    struct PlayerLabelState : ICleanupComponentData
    {

    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(InventorySystem))]
    [UpdateBefore(typeof(EventCleanSystem))]
    public partial class FloatingLabelUI_ItemTakeEventSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((in ItemPickupEvent itemPickupEvent) =>
                {
                    if (HasComponent<Item>(itemPickupEvent.ItemEntity) == false)
                    {
                        return;
                    }

                    var item = GetComponent<Item>(itemPickupEvent.ItemEntity);

                    var owner = item.Owner;
                    UserInterfaceData uiData;

                    if (HasComponent<UserInterfaceData>(owner))
                    {
                        uiData = GetComponent<UserInterfaceData>(owner);
                    }
                    else
                    {
                        uiData = GetSingleton<UserInterfaceData>();
                    }

                    var screen = EntityManager.GetComponentObject<FloatingLabelScreenUI>(uiData.Entity);

                    screen.AddItemLabel(screen, itemPickupEvent.Position, itemPickupEvent.ItemEntity, itemPickupEvent.Count, EntityManager);

                }).Run();

            Entities
            .WithAll<ItemPickupTransactionTag>()
            .WithoutBurst()
            .ForEach((int entityInQueryIndex, DynamicBuffer<ItemsToAdd> addedItems, in InventoryTransaction transaction, in Target target) =>
            {
                if (transaction.Status != InventoryTransactionStatus.Success)
                {
                    return;
                }

                if (HasComponent<PlayerController>(target.Value) == false)
                {
                    return;
                }
                var playerController = GetComponent<PlayerController>(target.Value);
                if (HasComponent<TzarGames.MultiplayerKit.NetworkPlayer>(playerController.Value) == false)
                {
                    return;
                }
                var player = GetComponent<TzarGames.MultiplayerKit.NetworkPlayer>(playerController.Value);

                if (player.ItsMe == false)
                {
                    return;
                }

                LocalTransform translation = default;

                if (SystemAPI.HasComponent<LocalTransform>(target.Value))
                {
                    translation = SystemAPI.GetComponent<LocalTransform>(target.Value);
                }

                if (SystemAPI.HasComponent<UserInterfaceData>(target.Value) == false)
                {
                    return;
                }

                var uiData = SystemAPI.GetComponent<UserInterfaceData>(target.Value);
                var screen = EntityManager.GetComponentObject<FloatingLabelScreenUI>(uiData.Entity);

                foreach (var addedItem in addedItems)
                {
                    var item = SystemAPI.GetComponent<Item>(addedItem.Item);
                    uint count;

                    if (SystemAPI.HasComponent<Consumable>(addedItem.Item))
                    {
                        count = SystemAPI.GetComponent<Consumable>(addedItem.Item).Count;
                    }
                    else
                    {
                        count = 1;
                    }
                    screen.AddItemLabel(screen, translation.Position, addedItem.Item, count, EntityManager);
                }

            }).Run();
        }

        
    }

    [DisableAutoCreation]
    public partial class FloatingLabelCharacterLabelUiSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if(SystemAPI.HasSingleton<TzarGames.MultiplayerKit.Client.ClientConnectionState>() 
               && SystemAPI.TryGetSingletonEntity<CameraData>(out Entity cameraEntity)
               && SystemAPI.TryGetSingleton(out UserInterfaceData uiData))
            {
                Entities
                    .WithoutBurst()
                    .WithStructuralChanges()
                    .WithNone<PlayerLabelState>()
                    .ForEach((Entity characterEntity, in Name30 name) =>
                {
                    if (name.ToString().Length == 0)
                    {
                        return;
                    }
                    
                    var screen = EntityManager.GetComponentObject<FloatingLabelScreenUI>(uiData.Entity);
                    
                    if (screen.Camera == null)
                    {
                        screen.Camera = EntityManager.GetComponentObject<ThirdPersonCamera>(cameraEntity).Camera;
                    }
                    
                    screen.AddPlayerLabel(characterEntity);
                    EntityManager.AddComponentData(characterEntity, new PlayerLabelState());
                    
                }).Run();
                
            }
            
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<Name30>().WithAll<PlayerLabelState>().ForEach((Entity entity) =>
                {
                    EntityManager.RemoveComponent<PlayerLabelState>(entity);
                }).Run();
        }
    }
}
