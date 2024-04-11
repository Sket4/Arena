 // Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using UniRx;
using UniRx.Triggers;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Arena.Client.UI
{
	[System.Serializable]
	public class EntityEvent : UnityEvent<Entity>
	{
	}


    public class InventoryUI : InventoryBaseUI
	{
        [System.Serializable]
        class ItemTab
        {
            public Button Button = default;
            public UIBase Window = default;
        }

		[SerializeField] private UIBase inventoryWindow = default;
		[SerializeField] private UIBase dialogWindow = default;
		
        [SerializeField]
        ItemTab inventoryTab = default;

        [SerializeField]
        ItemTab maskTab = default;

		[SerializeField]
		Button wearButton = default;

        [SerializeField]
        Button wearLeftButton = default;

        [SerializeField]
		Button unwearButton = default;
		
		[SerializeField]
		Button sellButton = default;

		[SerializeField] private TextUI sellDialogText = default;
		
		[SerializeField] private LocalizedStringAsset sellDialogMessage = default;
		
		[SerializeField]
		Button useButton = default;

		[SerializeField] private LocalizedStringAsset itemInfoLevel = default;

		[SerializeField] private InventoryItemInfo itemInfo = default;
		[SerializeField] private RectTransform itemInfoContainer = default;

		[SerializeField] private EntityEvent onItemUse = default;
		[SerializeField] private EntityEvent onItemWear = default;
		[SerializeField] private EntityEvent onItemUnwear = default;
		[SerializeField] private EntityEvent onItemSell = default;
		[SerializeField] private UnityEvent onCannotWear = default;

        ItemTab currentTab;

        protected override void OnVisible()
        {
	        base.OnVisible();
            ShowInvenoryTab();
		}

        public void ShowInvenoryTab()
        {
            showTab(inventoryTab);
        }

        public void ShowMaskTab()
        {
            showTab(maskTab);
        }

        private void showTab(ItemTab tab)
        {
            StartCoroutine(showTabRoutine(tab));
        }

        IEnumerator showTabRoutine(ItemTab tab)
        {
            if (currentTab == tab)
            {
                yield break;
            }

            yield return null;

            tab.Window.SetVisible(true);
            tab.Button.interactable = false;

            if (currentTab != null)
            {
                currentTab.Window.SetVisible(false);
                currentTab.Button.interactable = true;
            }
            currentTab = tab;
        }

		public void OnUseClicked()
		{
			if (LastSelected == null || LastSelected.ItemEntity == Entity.Null)
			{
				return;
			}

			var itemEntity = LastSelected.ItemEntity;
			var requestEntity = EntityManager.CreateEntity(typeof(UseItemRequest));
			EntityManager.SetComponentData(requestEntity, new UseItemRequest
			{
				ItemEntity = LastSelected.ItemEntity
			});

			Observable
				.EveryLateUpdate()
				.TakeWhile(x => EntityManager.Exists(requestEntity))
				.Subscribe((unit) =>
			{
				var req = GetData<UseItemRequest>(requestEntity);
				EntityManager.DestroyEntity(requestEntity);
				if (req.IsFinished == false)
				{
					return;
				}
				Debug.Log($"Завершен запрос на использование предмета {itemEntity}: {req.Status}");

				if (req.Status == UseRequestStatus.Success)
				{
					RefreshItems();
					onItemUse.Invoke(itemEntity);	
				}
			});
		}

		public void OnWearClicked()
		{
            wear(0);
		}

        public void OnWearLeftClicked()
        {
            wear(1);
        }

        private void wear(int slot)
        {
            var requestEntity = EntityManager.CreateEntity(typeof(ActivateItemRequest));
            var request = new ActivateItemRequest(true, LastSelected.ItemEntity);
            EntityManager.SetComponentData(requestEntity, request);

            
            
            // Observable.EveryLateUpdate()
            //     .TakeWhile(x => EntityManager.Exists(requestEntity))
            //     .SkipWhile(_ => EntityManager.GetComponentData<ActivateItemRequest>(requestEntity).State == ActivateItemRequestState.Processing)
            //     .Subscribe(_ =>
            //     {
	           //      var requestComponent = EntityManager.GetComponentData<ActivateItemRequest>(requestEntity);
	           //      if (requestComponent.State == ActivateItemRequestState.Success)
	           //      {
		          //       onItemWear.Invoke(LastSelected.ItemEntity);
	           //      }
	           //      else
	           //      {
		          //       onCannotWear.Invoke();
	           //      }
            //         UpdateUI();
            //         EntityManager.DestroyEntity(requestEntity);
            //     });
            
            
            
            

            // StartCoroutine(() =>
            // {
            //     yield return null;
            // });

            //LastSelected.ItemEntity;

            // var canWear = playerCharacter.TemplateInstance.CanActivateItem(LastSelected.ItemInstance, slot);
            //
            // if (canWear)
            // {
            //     playerCharacter.TemplateInstance.SetItemActive(LastSelected.ItemInstance, true, slot);
            //
            //     Debug.LogError("Not implemented");
            //     //var gameInstance = EndlessGameState.Instance;
            //     //if (gameInstance != null && gameInstance.IsItSafeStateToSaveGame())
            //     //{
            //     //    (EndlessGameManager.Instance as EndlessGameManager).SaveGameWithPlayerData();
            //     //}
            //     LastSelected.IsActivated = playerCharacter.TemplateInstance.IsItemActivated(LastSelected.ItemInstance);
            //
            //     UpdateUI();
            //
            //     if (LastSelected.IsActivated)
            //     {
            //         onItemWear.Invoke(LastSelected.Item);
            //     }
            // }
            // else
            // {
            //     onCannotWear.Invoke();
            // }
        }

        public void OnUnwearClicked()
		{
			var requestEntity = EntityManager.CreateEntity(typeof(ActivateItemRequest), typeof(DisableAutoDestroy));
            var request = new ActivateItemRequest(false, LastSelected.ItemEntity);
            EntityManager.SetComponentData(requestEntity, request);

            Observable.EveryLateUpdate()
                .TakeWhile(x => EntityManager.Exists(requestEntity))
                .SkipWhile(_ => EntityManager.GetComponentData<ActivateItemRequest>(requestEntity).State == ActivateItemRequestState.Processing)
                .Subscribe(_ =>
                {
                    UpdateUI();
                    EntityManager.DestroyEntity(requestEntity);
                });
		}
		
		public void OnSellClicked()
		{
            Debug.LogError("Not implemented");
            //var gameManager = GameManager.Instance as EndlessLobbyGameManager;
            //if (gameManager == null)
            //{
            //	return;
            //}
   //         var cost = gameManager.GetItemSellCost(LastSelected.Item);
			
			//sellDialogText.text = string.Format(sellDialogMessage, cost);
			
			//inventoryWindow.SetVisible(false);
			//dialogWindow.SetVisible(true);
		}

		public void CancelDialog()
		{
			inventoryWindow.SetVisible(true);
			dialogWindow.SetVisible(false);
		}

		public void OnConfirmSellClicked()
		{
			try
			{
				SellItem();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			
			CancelDialog();
		}

		public void SellItem()
		{
			if (LastSelected == null)
			{
				return;
			}

            Debug.LogError("Not implemented");
            //var gameManager = GameManager.Instance as EndlessLobbyGameManager;
            //if (gameManager == null)
            //{
            //	return;
            //}

            //if (LastSelected.ItemInstance != null)
            //{
            //	gameManager.SellItemInstance(LastSelected.ItemInstance);
            //}
            //else if(LastSelected.Item != null)
            //{
            //	gameManager.SellConsumableItem(LastSelected.Item, 1);
            //}
            //else
            //{
            //	Debug.LogError("No item to sell");
            //	return;
            //}
            //var soldItem = LastSelected.Item;
            //LastSelected = null;
            //RefreshItems();

            //onItemSell.Invoke(soldItem);
        }

		public void OnDestroyClick()
		{
			// var item = LastSelected.ItemInstance;
			// var bag = playerCharacter.TemplateInstance.Inventory.GetBagOfItemInstance(item);
			// if (bag != null && bag.IsItemInstanceLocked (item) == false) 
			// {
			// 	bag.RemoveItem (item);
   //              Debug.LogError("Not implemented");
   //              //var gameInstance = EndlessGameState.Instance;
   //              //if (gameInstance != null && gameInstance.IsItSafeStateToSaveGame())
   //              //{
   //              //    (EndlessGameManager.Instance as EndlessGameManager).SaveGameWithPlayerData();
   //              //}
   //              LastSelected.IsActivated = false;
			// 	RefreshItems ();
			// }
		}

		public override void UpdateUI ()
		{
            if(LastSelected != null)
            {
                var itemEntity = LastSelected.ItemEntity;

                itemInfo.gameObject.SetActive(true);
                itemInfo.UpdateData(itemEntity, GetData<Level>().Value, EntityManager);

                wearButton.gameObject.SetActive(HasData<ActivatedState>(itemEntity) && GetData<ActivatedState>(itemEntity).Activated == false);
                unwearButton.gameObject.SetActive(HasData<ActivatedState>(itemEntity) && GetData<ActivatedState>(itemEntity).Activated);
                
                useButton.gameObject.SetActive(HasData<Usable>(itemEntity));
            }
            else
            {
	            itemInfo.gameObject.SetActive(false);
	            
                wearButton.gameObject.SetActive(false);
                unwearButton.gameObject.SetActive(false);
                sellButton.gameObject.SetActive(false);
                useButton.gameObject.SetActive(false);
                wearLeftButton.gameObject.SetActive(false);
            }

			base.UpdateUI ();
		}
	}
}
