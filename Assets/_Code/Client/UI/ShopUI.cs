// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections.Generic;
using Arena;
using Arena.Client;
using Arena.Items;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameFramework;
using UniRx;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class ShopUI : TzarGames.GameFramework.UI.GameUIBase
    {
        [SerializeField]
        ShopItemUI shopItemPrefab = default;

        [SerializeField] private UIBase shopWindow = default;
        [SerializeField] private UIBase dialogWindow = default;

        [SerializeField]
        UnityEngine.UI.Extensions.HorizontalScrollSnap itemScroll = default;

        [SerializeField]
        Button buyButton = default;

        [SerializeField] private GameObject itemRestrictButton = default;

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
        [SerializeField] private Button weaponTabButton = default;

        [SerializeField]
        private GameObject shieldsTabButton = default;

        int currentSelectedItem = -1;

        List<ShopItemUI> availableItems = new List<ShopItemUI>();
        List<ShopItemUI> selectedItems = new List<ShopItemUI>();

        [SerializeField]
        UnityEngine.Events.UnityEvent onNotEnoughGold = default;

        [SerializeField]
        UnityEngine.Events.UnityEvent onNotEnoughRuby = default;

        [SerializeField] private UnityEvent onBuy = default;

        private bool initialized = false;
        private bool pendingShowFirstTab = true;
        private Entity itemDatabaseEntity;

        protected override void OnVisible()
        {
            base.OnVisible();
            
            if(pendingShowFirstTab)
            {
                InitializeItems();
                pendingShowFirstTab = false;
                ShowWeapons(weaponTabButton);
            }

            //var template = (CharacterOwner as PlayerCharacter).PlayerTemplateInstance;

            //shieldsTabButton.SetActive(template.CanWearItemType(typeof(Shield)));

            updateUI();
        }

        public void ShowWeapons(Button button)
        {
            showItemsOfType<OneHandedItem>(button, true);
        }

        public void ShowArmor(Button button)
        {
            showItemsOfType<ArmorSet>(button, true);
        }

        public void ShowShields(Button button)
        {
            //showItemsOfType<GameFramework.Shield>(button, true);
        }
        
        public void ShowOtherItems(Button button)
        {
            showItemsOfType<OtherCategoryStoreTag>(button, false);
        }

        public void ShowArtefacts(Button button)
        {
            //
        }

        private void showItemsOfType<T>(Button selectedTab, bool sort, Type[] attributeTypes = null) where T : struct, IComponentData
        {
            currentSelectedItem = 0;
            GameObject[] tmp;
            var defaultScale = Vector3.one;
            var classData = GetData<CharacterClassData>();

            for (int i = 0; i < availableItems.Count; i++)
            {
                var availableItem = availableItems[i];
                availableItem.gameObject.SetActive(false);
                var tr = availableItem.transform;
                if(tr.localScale != defaultScale)
                {
                    tr.localScale = defaultScale;
                }
                tr.SetParent(null, false);

                DontDestroyOnLoad(availableItem.gameObject);
            }
			
            try
			{
				itemScroll.RemoveAllChildren(out tmp);
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
			}
            
            selectedItems.Clear();
            
            foreach (var availableItem in availableItems)
            {
                var itemEntity = availableItem.Item;
                
                if (HasData<T>(itemEntity) == false)
                {
                    continue;
                }

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
            
            
            var childItems = new GameObject[selectedItems.Count];
            for (var index = 0; index < selectedItems.Count; index++)
            {
                var selectedItem = selectedItems[index];
                childItems[index] = selectedItem.gameObject;
                childItems[index].gameObject.SetActive(true);
            }

            itemScroll.AddChilds(childItems);
            itemScroll.UpdateLayout();

            var buttons = tabContainer.GetComponentsInChildren<Button>();
            foreach (var button in buttons)
            {
                if (selectedTab == button)
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
            if (initialized)
            {
                return;
            }
            try
            {
				refreshItems();
                for (int i = 0; i < availableItems.Count; i++)
                {
                    ShopItemUI availableItem = availableItems[i];
                    availableItem.gameObject.SetActive(false);
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            initialized = true;
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
            foreach (var availableItem in availableItems)
            {
                availableItem.transform.localScale = Vector3.one;
                Destroy(availableItem.gameObject);
            }

            availableItems.Clear();
            selectedItems.Clear();
            GameObject[] tmp;
            try
            {
                itemScroll.RemoveAllChildren(out tmp);    
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
            }

            if (itemDatabaseEntity == Entity.Null)
            {
                itemDatabaseEntity = EntityManager.World.GetExistingSystemManaged<UISystem>().GetSingletonEntity<MainDatabaseTag>();
            }

            var storeEntity = getCurrentStore();
            
            if (storeEntity == Entity.Null)
            {
                Debug.LogError("Не найден магазин");
                return;
            }

            var itemDatabase = EntityManager.GetBuffer<IdToEntity>(itemDatabaseEntity);
            var items = EntityManager.GetBuffer<StoreItems>(storeEntity);
            var inventory = GetBuffer<InventoryElement>();
            
            foreach (var item in items)
            {
                var itemEntity = IdToEntity.GetEntityByID(itemDatabase, item.ItemID);
                var newItem = createInstance(itemEntity, inventory);
                if (newItem != null)
                {
                    availableItems.Add(newItem);   
                }
            }
            itemScroll.UpdateLayout();
            currentSelectedItem = 0;
            updateUI();
        }

        void updateUI()
        {
            if (availableItems.Count == 0)
            {
                return;
            }

            //var item = availableItems[currentSelectedItem];
            
            itemRestrictButton.SetActive(false);//!localPlayer.PlayerTemplateInstance.IsLevelRestrictionDisabled);
            var inventory = GetBuffer<InventoryElement>();
            uint currentMoney = 0;
            if (inventory.TryGetItemWithComponent<MainCurrency>(EntityManager, out Entity moneyEntity))
            {
                currentMoney = GetData<Consumable>(moneyEntity).Count;
            }
            
            goldText.text = currentMoney.ToString();
            rubyText.text = "0";    
            
            foreach (var selectedItem in selectedItems)
            {
                selectedItem.RefreshIcon(EntityManager);
                updateItemExistance(selectedItem, inventory);
            }
        }

        ShopItemUI createInstance(Entity item, DynamicBuffer<InventoryElement> inventory)
        {
            try
            {
                var newItem = Instantiate(shopItemPrefab);
                var level = EntityManager.GetComponentData<Level>(OwnerEntity);
                newItem.UpdateData(item, level.Value, EntityManager);// requiredLevelText);

                newItem.Disabled = false;
            
                updateItemExistance(newItem, inventory);

                return newItem;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }        

        void updateItemExistance(ShopItemUI itemUi, in DynamicBuffer<InventoryElement> inventory)
        {
            var itemEntity = itemUi.Item;

            foreach (var inventoryElement in inventory)
            {
                if (GetData<Item>(itemEntity).ID == GetData<Item>(inventoryElement.Entity).ID)
                {
                    itemUi.ExistInInventory = true;
                    return;
                }
            }

            itemUi.ExistInInventory = false;
        }

        public void NotifyItemIndexChanged(int index)
        {
            currentSelectedItem = index;
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
            var itemUi = selectedItems[currentSelectedItem];
            var itemEntity = itemUi.Item;

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
                
                var itemUi = selectedItems[currentSelectedItem];
                var itemEntity = itemUi.Item;

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
