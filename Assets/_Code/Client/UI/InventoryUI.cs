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
		[SerializeField] private UIBase inventoryWindow = default;
		[SerializeField] private UIBase dialogWindow = default;

		[SerializeField] private Button allItemsButton;
		[SerializeField] private Button weaponButton;
		[SerializeField] private Button armorButton;

		[SerializeField]
		Button wearButton = default;

        [SerializeField]
        Button wearLeftButton = default;

        [SerializeField]
		Button unwearButton = default;

		[SerializeField] private Button[] tabs;

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

		private Button currentTab = null;

        protected override void OnVisible()
        {
	        base.OnVisible();
            ShowAllItems();
		}

        public void ShowAllItems()
        {
            showTab(allItemsButton);
            ClearFilter();
            UpdateUI();
        }

        public void ShowWeapons()
        {
	        showTab(weaponButton);
	        ClearFilter();
	        AddFilter<Weapon>();
	        UpdateUI();
        }

        public void ShowArmors()
        {
	        showTab(armorButton);
	        ClearFilter();
	        AddFilter<ArmorSet>();
	        AddFilter<Shield>();
	        UpdateUI();
        }

        private void showTab(Button tab)
        {
            StartCoroutine(showTabRoutine(tab));
        }

        IEnumerator showTabRoutine(Button tab)
        {
            if (currentTab == tab)
            {
                yield break;
            }

            yield return null;

            foreach (var button in tabs)
            {
	            button.interactable = tab != button;
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

		public void CancelDialog()
		{
			inventoryWindow.SetVisible(true);
			dialogWindow.SetVisible(false);
		}

		public override void UpdateUI ()
		{
			if (LastSelected == null)
			{
				foreach (var item in itemUiElements)
				{
					if (item.gameObject.activeSelf)
					{
						SelectItem(item);
						break;
					}
				}
			}
            if(LastSelected != null)
            {
                var itemEntity = LastSelected.ItemEntity;

                itemInfoContainer.gameObject.SetActive(true);
                itemInfo.UpdateData(itemEntity, GetData<Level>().Value, EntityManager);

                wearButton.gameObject.SetActive(HasData<ActivatedState>(itemEntity) && GetData<ActivatedState>(itemEntity).Activated == false);

                bool canUnwear = HasData<ActivatedState>(itemEntity) && GetData<ActivatedState>(itemEntity).Activated;
                if (canUnwear && (HasData<Weapon>(itemEntity) || HasData<ArmorSet>(itemEntity)))
                {
	                canUnwear = false;
                }
                
                unwearButton.gameObject.SetActive(canUnwear);
                useButton.gameObject.SetActive(HasData<Usable>(itemEntity));
                wearLeftButton.gameObject.SetActive(false);
            }
            else
            {
	            itemInfoContainer.gameObject.SetActive(false);
	            
                wearButton.gameObject.SetActive(false);
                unwearButton.gameObject.SetActive(false);
                useButton.gameObject.SetActive(false);
                wearLeftButton.gameObject.SetActive(false);
            }

			base.UpdateUI ();
		}
	}
}
