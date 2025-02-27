// Copyright 2012-2025 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using TzarGames.Common.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class ShopItemUI : InventoryItemInfo 
	{
		[SerializeField]
		Image disabledImage = default;

		[SerializeField] private TextUI existInInventory = default;

		public bool Disabled
		{
			get 
			{
				return disabledImage.gameObject.activeSelf;
			}
			set
			{
				disabledImage.gameObject.SetActive (value);
			}
		}
		
		public bool ExistInInventory
		{
			get { return existInInventory.gameObject.activeSelf; }
			set { existInInventory.gameObject.SetActive(value); }
		}
	}
}
