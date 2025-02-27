// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using Arena.Items;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Items;
using TzarGames.GameFramework;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class InventoryBaseUI : TzarGames.GameFramework.UI.GameUIBase
	{
		[SerializeField]
		RectTransform container = default;

		[SerializeField]
		InventoryItemUI itemPrefab = default;

		protected List<InventoryItemUI> itemUiElements = new List<InventoryItemUI>();
		protected InventoryItemUI LastSelected 
		{
			get;
			set;
		}

		[SerializeField]
		TextUI goldText = default;
		
		[SerializeField]
		TextUI rubyText = default;

		private bool needRefreshItems = true;

		private List<ComponentType> filter = new();

		protected void ClearFilter()
		{
			filter.Clear();
		}

		public void AddFilters(IEnumerable<ComponentType> filters)
		{
			filter.AddRange(filters);
		}

		public void AddFilter<T>()
		{
			filter.Add(ComponentType.ReadOnly(typeof(T)));
		}

		protected override void OnVisible()
		{
			base.OnVisible();
			
			if (needRefreshItems)
			{
				needRefreshItems = false;
				RefreshItems();
			}
			else
			{
				UpdateUI ();	
			}
		}

		Transform getContainerForItem(Entity item)
		{
			return container;
		}

		public void RefreshItems()
		{
            Entity lastSelectedInstance = Entity.Null;
            if(LastSelected != null )
            {
                lastSelectedInstance = LastSelected.ItemEntity;
            }
			for(int i=container.childCount-1; i>=0; i--)
			{
				var child = container.GetChild(i);
				var layoutElement = child.GetComponent<LayoutElement>();
				if (layoutElement && layoutElement.ignoreLayout)
				{
					continue;
				}
				child.SetParent(null);
				Destroy(child.gameObject);
			}
			itemUiElements.Clear();



			// if (_playerCharacter == null)
			// {
			// 	_playerCharacter = CharacterOwner as PlayerCharacter;
			// 	if (_playerCharacter != null)
			// 	{
			// 		_playerCharacter.TemplateInstance.Inventory.DefaultBag.OnItemsChanged += DefaultBagOnOnItemsChanged;
			// 	}
			// 	else
			// 	{
			// 		Debug.LogError("player character is null");
			// 		return;
			// 	}
			// }

			var items = GetBuffer<InventoryElement>();

			items.ForEach<Item>(EntityManager, (itemEntity, item) =>
			{
				if(HasData<MainCurrency>(itemEntity))
                {
					return;
                }

				uint count = 1;
				
				if(TryGetData(itemEntity, out Consumable consumable))
                {
					count = consumable.Count;
				}

				bool activated = false;
				if (HasData<ActivatedState>(itemEntity))
				{
					activated = GetData<ActivatedState>(itemEntity).Activated;
				}

				createItem(itemEntity, activated, count);
			});

            if(lastSelectedInstance != null)
            {
                foreach(var item in this.itemUiElements)
                {
                    if(item.ItemEntity == lastSelectedInstance)
                    {
                        SelectItem(item);
                        break;
                    }
                }
            }
			UpdateUI();
		}

		//private void DefaultBagOnOnItemsChanged(InventoryBag inventoryBag)
		//{
		//	needRefreshItems = true;
		//}

		IEnumerator loadIcon(InventoryItemUI itemUI, WeakObjectReference<Sprite> spriteRef)
		{
			if (spriteRef.LoadingStatus == ObjectLoadingStatus.None)
			{
				//Debug.Log($"Start loading sprite ref {spriteRef} for item {itemUI.ItemEntity}");
				spriteRef.LoadAsync();
			}
			else
			{
				//Debug.Log($"Waiting for sprite ref {spriteRef} for item {itemUI.ItemEntity}");
			}
			
			while (spriteRef.LoadingStatus != ObjectLoadingStatus.Completed && spriteRef.LoadingStatus != ObjectLoadingStatus.Error)
			{
				yield return null;
			}

			if (spriteRef.LoadingStatus == ObjectLoadingStatus.Error)
			{
				//Debug.LogError($"Failed to load icon for item {itemUI.ItemEntity}, ref: {spriteRef}");
				yield break;
			}

			//Debug.Log($"Finished loading sprite ref {spriteRef} for item {itemUI.ItemEntity}");
			if (itemUI == false)
			{
				yield break;
			}
			
			itemUI.ItemIcon = spriteRef.Result;
		}

		void createItem(Entity itemInstance, bool activated, uint count)
		{
			var instance = Instantiate(itemPrefab);
			instance.transform.SetParent(getContainerForItem(itemInstance), false);

			instance.IsActivated = activated;
			instance.ItemEntity = itemInstance;
			instance.Selected = false;
			instance.ItemIcon = null;
			instance.OnItemClicked += onItemClicked;
			
			StartCoroutine(loadIcon(instance, EntityManager.GetComponentData<ItemIcon>(itemInstance).Sprite));
			
			if (EntityManager.HasComponent<Consumable>(itemInstance))
			{
				instance.ShowCount = true;
				instance.Count = count.ToString();
			}
			else
			{
				instance.ShowCount = false;
			}
			itemUiElements.Add(instance);
		}

		public virtual void UpdateUI()
		{
			if(OwnerEntity == Entity.Null)
			{
				return;
			}

            foreach (var item in itemUiElements)
            {
	            var passFilter = false;

	            if (filter.Count > 0)
	            {
		            foreach (var type in filter)
		            {
			            if (EntityManager.HasComponent(item.ItemEntity, type))
			            {
				            passFilter = true;
				            break;
			            }
		            }
	            }
	            else
	            {
		            passFilter = true;
	            }
	            
	            item.gameObject.SetActive(passFilter);
				item.IsActivated = HasData<ActivatedState>(item.ItemEntity) ? (bool)GetData<ActivatedState>(item.ItemEntity).Activated : false;
			}

			var items = GetBuffer<InventoryElement>();

			if (goldText)
			{
				if(items.TryGetItemWithComponent<MainCurrency>(EntityManager, out Entity itemEntity))
				{
					var count = GetData<Consumable>(itemEntity).Count;
					goldText.text = count.ToString();
				}
				else
				{
					goldText.text = "0";
				}	
			}
            
			if (rubyText)
			{
                rubyText.text = "0";// EndlessGameState.Instance.SelectedCharacter.Ruby.ToString();
			}
		}

		void onItemClicked(InventoryItemUI itemUI)
		{
            SelectItem(itemUI);
            UpdateUI();
        }

        protected void SelectItem(InventoryItemUI itemUI)
        {
            if (LastSelected != null)
            {
                LastSelected.Selected = false;
            }
            itemUI.Selected = true;
            LastSelected = itemUI;
            HandleItemClick(itemUI);
        }

		protected virtual void HandleItemClick(InventoryItemUI itemUI)
		{
		}

		// protected override void OnPlayerCharacterSet(Entity entity, EntityManager manager)
		// {
  //           base.OnPlayerCharacterSet(entity, manager);
  //
		// 	if (_playerCharacter != null)
		// 	{
		// 		_playerCharacter.TemplateInstance.Inventory.DefaultBag.OnItemsChanged -= DefaultBagOnOnItemsChanged;
		// 		_playerCharacter = null;	
		// 	}
		// }
	}
}
