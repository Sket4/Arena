using System;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameFramework;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class HealingPotionHudUI : GameUIBase
	{
		//[SerializeField] private TH_Health healingItem = default;
		[SerializeField] private RectTransform container = default;
		[SerializeField] private TextUI counter = default;
		[SerializeField] private Image cooldownIcon = default;

		[SerializeField] private UnityEvent onUsed = default;

		// protected override void OnPlayerCharacterSet(Entity entity, EntityManager manager)
		// {
		// 	base.OnPlayerCharacterSet(entity, manager);
		// 	playerCharacter.TemplateInstance.Inventory.DefaultBag.OnItemsChanged += DefaultBagOnOnItemsChanged;
		// }

		// private void Update()
		// {
		// 	if (container.gameObject.activeSelf)
		// 	{
		// 		var fill = cooldownIcon.fillAmount;
		// 		var cooldown = playerCharacter.HealingPotionCooldownTime;
		// 		if (Math.Abs(fill - cooldown) > FMath.KINDA_SMALL_NUMBER)
		// 		{
		// 			cooldownIcon.fillAmount = cooldown;	
		// 		}
		// 	}
		// }

		protected override void OnVisible()
		{
			base.OnVisible();
			refreshItems();
		}

		//private void DefaultBagOnOnItemsChanged(InventoryBag inventoryBag)
		//{
		//	refreshItems();
		//}

		private void refreshItems()
		{
			//var count = playerCharacter.TemplateInstance.Inventory.DefaultBag.GetConsumableItemCount(healingItem.Id);
			// if (count == 0)
			// {
			// 	if (container.gameObject.activeSelf)
			// 	{
			// 		container.gameObject.SetActive(false);	
			// 	}
			// 	
			// 	return;
			// }
			// if (container.gameObject.activeSelf == false)
			// {
			// 	container.gameObject.SetActive(true);	
			// }
			//
			// counter.text = count.ToString();
		}

		public void OnUseClick()
		{
			// var count = playerCharacter.TemplateInstance.Inventory.DefaultBag.GetConsumableItemCount(healingItem.Id);
			// if (count > 0)
			// {
			// 	if (playerCharacter.UseItem(healingItem, null))
			// 	{
			// 		var result = playerCharacter.TemplateInstance.Inventory.DefaultBag.RemoveConsumableItem(healingItem, 1);
			// 		if (result)
			// 		{
			// 			refreshItems();	
			// 		}	
			// 		onUsed.Invoke();
			// 	}
			// }
		}
	}
}
