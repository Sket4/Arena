using System;
using TzarGames.GameCore;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class ShopSellInventoryUI : InventoryBaseUI
    {
        [SerializeField] private Button sellButton;
        [SerializeField] private TMPro.TextMeshProUGUI sellButtonText;
        [SerializeField] private InventoryItemInfo itemInfo;
        [SerializeField] private LocalizedString sellButtonFormatText;
        [SerializeField] private LocalizedString sellButtonDefaultText;
        [SerializeField] private UnityEvent onSelled;

        protected override void OnVisible()
        {
            base.OnVisible();
            updateSellButtonState();
        }

        protected override void HandleItemClick(InventoryItemUI itemUI)
        {
            base.HandleItemClick(itemUI);

            updateSellButtonState();
            var level = GetData<Level>(OwnerEntity);
            itemInfo.UpdateData(itemUI.ItemEntity, level.Value, EntityManager);
        }

        void updateSellButtonState()
        {
            bool canSell;

            if (LastSelected != null)
            {
                if (HasData<ActivatedState>(LastSelected.ItemEntity))
                {
                    var state = GetData<ActivatedState>(LastSelected.ItemEntity);
                    canSell = state.Activated == false;
                }
                else
                {
                    canSell = true;
                }    
            }
            else
            {
                canSell = false;
            }

            sellButton.interactable = canSell;
            
            if (canSell)
            {
                var sellPrice = StoreSystem.GetSellPrice(GetData<Price>(LastSelected.ItemEntity).Value);
                sellButtonText.text = string.Format(sellButtonFormatText.GetLocalizedString(), sellPrice);    
            }
            else
            {
                sellButtonText.text = sellButtonDefaultText.GetLocalizedString();
            }
        }

        public void OnSellClicked()
        {
            FindObjectOfType<ShopUI>().ShowSellDialog();
        }

        public async void Sell()
        {
            var store = FindObjectOfType<ShopUI>().GetCurrentStore();
            
            Debug.Log($"Попытка покупки предмета {LastSelected.ItemEntity.Index} в магазине {store}");
            sellButton.gameObject.SetActive(false);

            try
            {
                var storeSystem = EntityManager.World.GetExistingSystemManaged<StoreSystem>();
                var list = new NativeArray<SellRequest_Item>(1, Allocator.Temp);
                list[0] = new SellRequest_Item { ItemEntity = LastSelected.ItemEntity, Count = 1 };
                var sellTask = storeSystem.RequestSell(OwnerEntity, store, list);

                var result = await sellTask;
                Debug.Log($"Результат продажи: {result}");

                if (result == SellRequestStatus.Success)
                {
                    RefreshItems();
                    updateSellButtonState();
                    onSelled.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                sellButton.gameObject.SetActive(true);
                FindObjectOfType<ShopUI>().ShowShop();
            }
        }
    }
}
