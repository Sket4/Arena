// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using Arena.Items;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class PlayerCharacterUI : TzarGames.GameFramework.UI.UICharacterHUD
    {
        [Header("TotAL RPG settings")]
        [SerializeField]
        TextUI goldText = default;

        [SerializeField] private Map mapCameraPrefab;
        [SerializeField] private GameObject minimapContainer;
        [SerializeField] private RawImage miniMapImage;

        [SerializeField] private TextUI rubyText = default;
        
        [SerializeField] private GameObject chatWindow = default;
        [SerializeField] private GameObject skillWindow = default;
        [SerializeField] private InteractionUI interaction = default;
        [SerializeField] private GameObject difficultyContainer;
        [SerializeField] private TextUI enemyStrengthText;
        private ushort lastEnemyStrengthValue;

        [SerializeField] private GameObject foodIndicator;
        [SerializeField] private Image foodIndicatorCooldown;
        private Entity foodIndicatorEntity;

        public void SetFoodIndicatorEntity(Entity entity)
        {
            foodIndicatorEntity = entity;
        }

        [SerializeField]
        TextUI allyName = default;

        [SerializeField]
        TextUI pingText = default;
        int lastPing;

        TzarGames.MultiplayerKit.Client.ClientSystem clientSystem;

        [SerializeField]
        Slider allyHealthBar = default;

        private ulong lastCharacterGold = ulong.MaxValue;
        uint currentGold = 0;
        private ulong lastCharacterRuby = ulong.MaxValue;

        [SerializeField]
        GameObject statsButton = default;

        [SerializeField]
        GameObject adsButton = default;

        private GameState gameState;

        [Header("Notifications")]
        [SerializeField] private NotificationUI notification = default;
        [SerializeField] private float itemTakeNotificationTime = 5;
        [SerializeField] private TextUI questInfoText = default;

        public NotificationUI Notifications => notification;

        private Map mapCamera;
        private bool mapCameraEnabledState = true;
        
        public TextUI QuestInfoText
        {
            get
            {
                return questInfoText;
            }
        }

        // protected override void Update()
        // {
        //     base.Update();

        //     if (gameState != null)
        //     {
        //         adsButton.SetActive(rewardWindow.AdsAvailable);
        //     }
        // }

        protected override void OnVisible()
        {
            base.OnVisible();
            
            //if (topPlayerWidget != null)
            //{
            //    topPlayerWidget.ResetMessages();
            //}

            var gameState = GameState.Instance;
            
            if(gameState != null)
            {
                //chatWindow.SetActive(true);
                //skillWindow.SetActive(true);
                //statsButton.SetActive(true);
            }

            var childUIs = GetComponentsInChildren<TzarGames.GameFramework.UI.GameUIBase>();
            if (childUIs != null)
            {
                foreach (var c in childUIs)
                {
                    if (c == this)
                    {
                        continue;
                    }
                    if(c.OwnerEntity == Entity.Null)
                    {
                        c.Setup(OwnerEntity, UIEntity, EntityManager);
                    }
                }
            }
        }

        public Map GetOrCreateMapCamera(Entity targetEntity, EntityManager em)
        {
            if (mapCamera)
            {
                return mapCamera;
            }
            mapCamera = Object.Instantiate(mapCameraPrefab);
            mapCamera.Setup(targetEntity, em);
            mapCamera.Camera.enabled = mapCameraEnabledState;
            return mapCamera;
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            //questInfoText.text = "";
            interaction.Setup(ownerEntity, uiEntity, manager);
            clientSystem = manager.World.GetExistingSystemManaged<TzarGames.MultiplayerKit.Client.ClientSystem>();

            var map = GetOrCreateMapCamera(ownerEntity, manager);
            miniMapImage.texture = map.CameraTexture;
        }

		private void OnDestroy()
		{
            // if (CharacterOwner != null)
            // {
            //     CharacterOwner.OnCharacterLevelUp -= Handle_OnCharacterLevelUp;
            // }
		}
        
        //[UpdateAfter(typeof(GameCommandBufferSystem))]
        //[DisableAutoCreation]
        //public class UISystem : SystemBase
        //{
        //    protected override void OnUpdate()
        //    {
        //        Entities.ForEach((in InventoryEvent evt) =>
        //        {
                    

        //        }).Run();

        //        Entities.ForEach((Entity entity, PlayerCharacterUI ui) =>
        //        {
                    

        //        }).Run();
        //    }
        //}

        protected override void Update()
        {
            base.Update();

            if(clientSystem != null && clientSystem.IsConnected)
            {
                var ping = (int)(clientSystem.RTT * 1000.0f);

                if(lastPing != ping)
                {
                    lastPing = ping;
                    pingText.text = ping.ToString();
                }
            }
            else
            {
                pingText.gameObject.SetActive(false);
            }
        }

        void updateGold(Entity entity)
        {
            var consumable = EntityManager.GetComponentData<Consumable>(entity);
            currentGold = consumable.Count;
        }

        protected override void updateCharacterStats()
        {
            base.updateCharacterStats();

            if(HasData<InventoryElement>() == false)
            {
                return;
            }

            var items = GetBuffer<InventoryElement>();

            items.ForEach<MainCurrency>(EntityManager, updateGold);

            if (lastCharacterGold != currentGold)
            {
                lastCharacterGold = currentGold;
                goldText.text = currentGold.ToString();
            }

            // var allies = PlayerCharacter.Allies;
            // Character ally = null;

            // foreach (var a in allies)
            // {
            //     ally = a;
            //     break;
            // }

            // if (ally != null)
            // {
            //     allyName.gameObject.SetActive(true);
            //     allyHealthBar.gameObject.SetActive(true);

            //     var template = ally.TemplateInstance;
            //     var hp = template.ActualHitPoints / template.HitPoints;

            //     if (Mathf.Abs(allyHealthBar.value - hp) > FMath.KINDA_SMALL_NUMBER)
            //     {
            //         allyHealthBar.value = hp;
            //     }

            //     allyName.text = template.localizedName;
            // }
            // else
            {
                allyName.gameObject.SetActive(false);
                allyHealthBar.gameObject.SetActive(false);
            }

            if (gameState == null)
            {
                gameState = GameState.Instance;
                if (gameState == null)
                {
                    return;
                }
            }

            var ruby = (ulong)0;//gameState.SelectedCharacter.Ruby;
            if (ruby != lastCharacterRuby)
            {
                lastCharacterRuby = ruby;
                rubyText.text = ruby.ToString();
            }
        }

        public override void OnSystemUpdate(UISystem system)
        {
            base.OnSystemUpdate(system);

            if (system.TryGetSingleton(out DifficultyData difficultyData))
            {
                if (lastEnemyStrengthValue != difficultyData.EnemyStrengthMultiplier)
                {
                    lastEnemyStrengthValue = difficultyData.EnemyStrengthMultiplier;

                    if (lastEnemyStrengthValue > 1)
                    {
                        enemyStrengthText.text = $"Усиление врагов <color=yellow>x{lastEnemyStrengthValue}</color>";
                    
                        if (difficultyContainer.activeSelf == false)
                        {
                            difficultyContainer.SetActive(true);
                        }    
                    }
                    else
                    {
                        if (difficultyContainer.activeSelf)
                        {
                            difficultyContainer.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                if (difficultyContainer.activeSelf)
                {
                    difficultyContainer.SetActive(false);
                }
            }

            var elapsedTime = system.World.Time.ElapsedTime;

            if (foodIndicatorEntity != Entity.Null && EntityManager.HasComponent<ItemUsageSystem.TimeModificator>(foodIndicatorEntity))
            {
                var timeMod = EntityManager.GetComponentData<ItemUsageSystem.TimeModificator>(foodIndicatorEntity);
                
                if (foodIndicator.activeSelf == false)
                {
                    foodIndicator.SetActive(true);
                }
                foodIndicatorCooldown.fillAmount = 1.0f - (float)((elapsedTime - timeMod.StartTime) / timeMod.Duration);
            }
            else
            {
                foodIndicatorEntity = Entity.Null;
                
                if (foodIndicator.activeSelf)
                {
                    foodIndicator.SetActive(false);
                }
            }
        }

        public void EnableMinimap(bool enable)
        {
            minimapContainer.SetActive(enable);
            if (mapCamera)
            {
                mapCamera.Camera.enabled = enable;
            }
            mapCameraEnabledState = enable;
        }
    }
}
