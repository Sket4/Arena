// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections.Generic;
using Arena.Client.PreviewRendering;
using Arena.Items;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class ShopUI : TzarGames.GameFramework.UI.GameUIBase
    {
        [SerializeField]
        ShopItemUI shopItemInfo;

        [SerializeField] private UIBase shopWindow = default;
        [SerializeField] private UIBase dialogWindow = default;

        [SerializeField]
        Button buyButton = default;

        [SerializeField]
        TextUI goldText = default;

        [SerializeField]
        TextUI rubyText = default;

        /// <summary>
        ///  SERIALIZEME
        /// </summary>
        [SerializeField]
        LocalizedStringAsset requiredLevelText = default;

        [SerializeField] private Transform tabContainer = default;
        [SerializeField] private GameObject tabPrefab;
        
        int currentSelectedItemIndex = -1;

        struct ShopItemEntry
        {
            public StoreItems ItemData;
            public Entity ItemPrefab;
        }

        List<ShopItemEntry> availableItems = new();
        List<ShopItemEntry> selectedItems = new();

        [SerializeField]
        UnityEvent onNotEnoughGold = default;

        [SerializeField]
        UnityEvent onNotEnoughRuby = default;

        [SerializeField] private UnityEvent onBuy = default;

        private Entity itemDatabaseEntity;
        private Entity currentStoreEntity = Entity.Null;

        protected override void OnVisible()
        {
            base.OnVisible();

            PreviewRenderGameWorldLauncher.Instance.EnableRendering = true;

            var store = getCurrentStore();

            if (currentStoreEntity != store)
            {
                InitializeItems();
                showItems(true, 0);
            }

            updateUI();
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            PreviewRenderGameWorldLauncher.Instance.EnableRendering = false;
        }

        private void showItems(bool sort, byte groupID)
        {
            currentSelectedItemIndex = 0;
            var classData = GetData<CharacterClassData>();
            
            selectedItems.Clear();
            
            foreach (var availableItem in availableItems)
            {
                if (availableItem.ItemData.GroupID != groupID)
                {
                    continue;
                }
                var itemEntity = availableItem.ItemPrefab;

                if (HasData<ClassUsage>(itemEntity))
                {
                    var classUsage = GetData<ClassUsage>(itemEntity);
                    if (classUsage.HasFlag(classData.Value) == false)
                    {
                        continue;
                    }
                }

                selectedItems.Add(availableItem);
            }

            if (selectedItems.Count == 0)
            {
                Debug.LogError("no selected store items");
                return;
            }

            // if (sort)
            // {
            //     selectedItems.Sort((x, y) =>
            //     {
            //         if (x.Item.MinimumLevel > y.Item.MinimumLevel)
            //         {
            //             return 1;
            //         }
            //         if (x.Item.MinimumLevel < y.Item.MinimumLevel)
            //         {
            //             return -1;
            //         }
            //         return 0;
            //     });    
            // }

            var buttons = tabContainer.GetComponentsInChildren<Button>();
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                
                if (index == groupID)
                {
                    button.interactable = false;
                }
                else
                {
                    button.interactable = true;
                }
            }

            updateUI();
        }

        public void InitializeItems()
        {
            try
            {
				refreshItems();
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        Entity getCurrentStore()
        {
            Entity storeEntity = Entity.Null;
            
            var linkeds = GetBuffer<LinkedEntityGroup>();
            var interactor = Entity.Null;
			
            foreach (var linked in linkeds)
            {
                if (HasData<OverlappingEntities>(linked.Value))
                {
                    interactor = linked.Value;
                    break;
                }
            }
            
            var overlappings = GetBuffer<OverlappingEntities>(interactor);
            
            foreach (var overlapping in overlappings)
            {
                if (HasData<StoreItems>(overlapping.Entity))
                {
                    storeEntity = overlapping.Entity;
                    break;
                }
            }

            return storeEntity;
        }

        void refreshItems()
        {
            availableItems.Clear();
            selectedItems.Clear();

            if (itemDatabaseEntity == Entity.Null)
            {
                itemDatabaseEntity = EntityManager.World.GetExistingSystemManaged<UISystem>().GetSingletonEntity<MainDatabaseTag>();
            }
            
            Utility.DestroyAllChilds(tabContainer);

            var storeEntity = getCurrentStore();
            
            if (storeEntity == Entity.Null)
            {
                Debug.LogError("Не найден магазин");
                return;
            }

            currentStoreEntity = storeEntity;
            
            var groups = GetBuffer<StoreGroups>(storeEntity);
            
            for (byte index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var localizedGroupName = LocalizationSettings.StringDatabase.GetLocalizedString(group.LocalizationID);
                var tabInstance = Instantiate(tabPrefab, tabContainer);
                tabInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = localizedGroupName;
                var groupID = index;
                tabInstance.GetComponent<Button>().onClick.AddListener(() =>
                {
                    showItems(true, groupID);
                });
                tabInstance.SetActive(true);
            }
            
            var itemDatabase = GetBuffer<IdToEntity>(itemDatabaseEntity);
            var items = GetBuffer<StoreItems>(storeEntity);
            var inventory = GetBuffer<InventoryElement>();
            
            foreach (var item in items)
            {
                var itemEntity = IdToEntity.GetEntityByID(itemDatabase, item.ItemID);
                var newItem = new ShopItemEntry
                {
                    ItemPrefab = itemEntity,
                    ItemData = item,
                };
                availableItems.Add(newItem);
            }

            if (currentSelectedItemIndex < 0)
            {
                currentSelectedItemIndex = 0;
                showItems(true, 0);    
            }
            updateUI();
        }

        void updateUI()
        {
            if (availableItems.Count == 0)
            {
                return;
            }

            var inventory = GetBuffer<InventoryElement>();
            uint currentMoney = 0;
            if (inventory.TryGetItemWithComponent<MainCurrency>(EntityManager, out Entity moneyEntity))
            {
                currentMoney = GetData<Consumable>(moneyEntity).Count;
            }
            
            goldText.text = currentMoney.ToString();
            rubyText.text = "0";    

            var currentSelectedItem = selectedItems[currentSelectedItemIndex];
            
            var level = GetData<Level>(OwnerEntity);
            shopItemInfo.UpdateData(currentSelectedItem.ItemPrefab, level.Value, EntityManager);
            shopItemInfo.Disabled = false;
            shopItemInfo.PreviewImage = PreviewRenderGameWorldLauncher.Instance.PreviewCameraTexture;
            updateItemExistance(inventory);
            
            var item = GetData<Item>(currentSelectedItem.ItemPrefab);
            PreviewRenderGameWorldLauncher.Instance.ShowPreviewItem(item.ID);
        }

        void updateItemExistance(in DynamicBuffer<InventoryElement> inventory)
        {
            var itemEntity = shopItemInfo.Item;

            foreach (var inventoryElement in inventory)
            {
                if (GetData<Item>(itemEntity).ID == GetData<Item>(inventoryElement.Entity).ID)
                {
                    shopItemInfo.ExistInInventory = true;
                    return;
                }
            }

            shopItemInfo.ExistInInventory = false;
        }

        public void NotifyItemIndexChanged(int index)
        {
            currentSelectedItemIndex = index;
            updateUI();
        }

        public void ShowBuyDialog()
        {
            shopWindow.SetVisible(false);
            dialogWindow.SetVisible(true);
        }
        
        public void ShowShop()
        {
            shopWindow.SetVisible(true);
            dialogWindow.SetVisible(false);
        }

        public void OnBuyClick()
        {
            var itemUi = selectedItems[currentSelectedItemIndex];
            var itemEntity = itemUi.ItemPrefab;

            if (HasData<OneHandedItem>(itemEntity) || HasData<ArmorSet>(itemEntity))
            {
                ShowBuyDialog();    
            }
            else
            {
                BuyItem();
            }
        }

        public async void BuyItem()
        {
            try
            {
                var store = getCurrentStore();

                if (store == Entity.Null)
                {
                    Debug.LogError("Не найден магазин");
                    return;
                }
                
                var itemUi = selectedItems[currentSelectedItemIndex];
                var itemEntity = itemUi.ItemPrefab;

                var itemID = GetData<Item>(itemEntity).ID;

                var storeSystem = EntityManager.World.GetExistingSystemManaged<StoreSystem>();
                var pc = GetData<PlayerController>();
                var list = new NativeArray<PurchaseRequest_Item>(1, Allocator.Temp);
                list[0] = new PurchaseRequest_Item { ItemID = itemID, Count = 1 };
                var result = await storeSystem.RequestPuchase(pc.Value, store, list);

                Debug.Log($"Результат покупки {result}");

                if (result == PurchaseRequestStatus.NotEnoughMoney)
                {
                    onNotEnoughGold.Invoke();
                }
                else if (result == PurchaseRequestStatus.Success)
                {
                    onBuy.Invoke();
                }

                updateUI();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            ShowShop();
        }
    }
}
