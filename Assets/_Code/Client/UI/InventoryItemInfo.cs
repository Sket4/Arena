// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections;
using Arena;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Items;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    enum ModificatorDisplayOptions
	{
		Default,
		Percent,
		Multiplier
	}
	
	public class InventoryItemInfo : MonoBehaviour, TzarGames.GameFramework.IPoolable
    {
		[SerializeField]
		Image iconImage = default;

		[SerializeField]
		TextUI itemNameText = default;
		
		[SerializeField]
		TextUI itemDescriptionText = default;

		[SerializeField]
		TextUI sellCost = default;
		
		[SerializeField]
		TextUI costText = default;

		[SerializeField]
		TextUI rubyCostText = default;

		[SerializeField]
		TextUI levelText = default;

		[SerializeField]
		TextUI damageText = default;

        [SerializeField]
        TextUI damageModificatorText = default;

        [SerializeField]
		TextUI defenceText = default;

        [SerializeField]
        TextUI defenceModificatorText = default;

        [SerializeField]
		TextUI attackSpeedText = default;
		
		[SerializeField]
		TextUI speedText = default;
		
		[SerializeField]
		TextUI hpRegen = default;
		
		[SerializeField]
		TextUI hpBoost = default;
		
		[SerializeField]
		TextUI critChance = default;
		
		[SerializeField]
		TextUI critMultiplier = default;
		
		[SerializeField]
		TextUI shieldBlockChance = default;

        [SerializeField]
        LocalizedStringAsset defaultLevelFormat = default;

        private Entity _item;
        Coroutine itemAnimationCoroutine;

        public Entity Item
        {
            get
            {
                return _item;
            }
            set
            {
                if(_item == value)
                {
                    return;
                }

                _item = value;

                updateSpriteAnimationState();
            }
        }

        void updateSpriteAnimationState()
        {
            if (_item != null)
            {
                if (itemAnimationCoroutine != null)
                {
                    StopCoroutine(itemAnimationCoroutine);
                    itemAnimationCoroutine = null;
                }

                //var animComponent = _item.GetComponent<AnimatedIconItemComponent>();
                //if (animComponent != null)
                //{
                //    itemAnimationCoroutine = StartCoroutine(itemAnimation(_item, animComponent));
                //}
            }
            else
            {
                if (itemAnimationCoroutine != null)
                {
                    StopCoroutine(itemAnimationCoroutine);
                }
            }
        }

        void OnEnable()
        {
            updateSpriteAnimationState();
        }

        void OnDisable()
        {
            if (itemAnimationCoroutine != null)
            {
                StopCoroutine(itemAnimationCoroutine);
                itemAnimationCoroutine = null;
            }
        }

        //IEnumerator itemAnimation(Item item, AnimatedIconItemComponent animComponent)
        //{
        //    var sprites = animComponent.Sprites;
        //    var frameTime = 1.0f / animComponent.FPS;
        //    int currentFrame = 0;

        //    while(true)
        //    {
        //        iconImage.sprite = sprites[currentFrame];
        //        yield return new WaitForSeconds(frameTime);
        //        currentFrame++;
        //        if(currentFrame >= sprites.Length)
        //        {
        //            currentFrame = 0;
        //        }
        //    }
        //}
        

		public string Cost
		{
			get
			{
				if (costText == null)
				{
					return null;
				}
				return costText.text;
			}
			set
			{
				if (costText == null)
				{
					return;
				}
				costText.text = value;
			}
		}

		public string RubyCost
		{
			get
			{
				if (rubyCostText == null)
				{
					return null;
				}
				return rubyCostText.text;
			}
			set
			{
				if (rubyCostText == null)
				{
					return;
				}
				rubyCostText.text = value;
			}
		}

		
		public string RequiredLevel
		{
			get
			{
				if (levelText == null)
				{
					return null;
				}
				return levelText.text;
			}
			set
			{
				if (levelText == null)
				{
					return;
				}
				levelText.text = value;
			}
		}

		public string Damage
		{
			get 
			{
				if (damageText == null)
				{
					return null;
				}
				return damageText.text;
			}
			set 
			{
				if (damageText == null)
				{
					return;
				}
				damageText.text = value;
			}
		}

        public string DamageModificator
        {
            get
            {
                if (damageModificatorText == null)
                {
                    return null;
                }
                return damageModificatorText.text;
            }
            set
            {
                if (damageModificatorText == null)
                {
                    return;
                }
                damageModificatorText.text = value;
            }
        }

        public string Defence
		{
			get 
			{
				if (defenceText == null)
				{
					return null;
				}
				return defenceText.text;
			}
			set 
			{
				if (defenceText == null)
				{
					return;
				}
				defenceText.text = value;
			}
		}

        public string DefenceModificator
        {
            get
            {
                if (defenceModificatorText == null)
                {
                    return null;
                }
                return defenceModificatorText.text;
            }
            set
            {
                if (defenceModificatorText == null)
                {
                    return;
                }
                defenceModificatorText.text = value;
            }
        }

        public string AttackSpeed
		{
			get 
			{
				if (attackSpeedText == null)
				{
					return null;
				}
				return attackSpeedText.text;
			}
			set 
			{
				if (attackSpeedText == null)
				{
					return;
				}
				attackSpeedText.text = value;
			}
		}
		
		public string Speed
		{
			get 
			{
				if (speedText == null)
				{
					return null;
				}
				return speedText.text;
			}
			set 
			{
				if (speedText == null)
				{
					return;
				}
				speedText.text = value;
			}
		}
		
		public string HpRegen
		{
			get 
			{
				if (hpRegen == null)
				{
					return null;
				}
				return hpRegen.text;
			}
			set 
			{
				if (hpRegen == null)
				{
					return;
				}
				hpRegen.text = value;
			}
		}
		
		public string HpBoost
		{
			get 
			{
				if (hpBoost == null)
				{
					return null;
				}
				return hpBoost.text;
			}
			set 
			{
				if (hpBoost == null)
				{
					return;
				}
				hpBoost.text = value;
			}
		}
		
		public string CritChance
		{
			get 
			{
				if (critChance == null)
				{
					return null;
				}
				return critChance.text;
			}
			set 
			{
				if (critChance == null)
				{
					return;
				}
				critChance.text = value;
			}
		}
		
		public string CritMultiplier
		{
			get 
			{
				if (critMultiplier == null)
				{
					return null;
				}
				return critMultiplier.text;
			}
			set 
			{
				if (critMultiplier == null)
				{
					return;
				}
				critMultiplier.text = value;
			}
		}
		
		public string ShieldBlockChance
		{
			get 
			{
				if (shieldBlockChance == null)
				{
					return null;
				}
				return shieldBlockChance.text;
			}
			set 
			{
				if (shieldBlockChance == null)
				{
					return;
				}
				shieldBlockChance.text = value;
			}
		}


		public string ItemName
		{
			get
			{
				if (itemNameText == null)
				{
					return null;
				}
				return itemNameText.text;
			}
			set
			{
				if (itemNameText == null)
				{
					return;
				}
				itemNameText.text = value;
			}
		}

		public Sprite ItemIcon
		{
			get
			{
				if (iconImage == null)
				{
					return null;
				}
				return iconImage.sprite;
			}
			set
			{
				if (iconImage == null)
				{
					return;
				}
				iconImage.sprite = value;
			}
		}
		
		public string ItemDescription
		{
			get
			{
				if (itemDescriptionText == null)
				{
					return null;
				}
				return itemDescriptionText.text;
			}
			set
			{
				if (itemDescriptionText == null)
				{
					return;
				}
				
				if (string.IsNullOrEmpty(value))
				{
					itemDescriptionText.gameObject.SetActive(false);
				}
				else
				{
					itemDescriptionText.text = value;
					itemDescriptionText.gameObject.SetActive(true);
				}
			}
		}


		public void ShowGoldCost(bool show)
		{
			if (costText == null)
			{
				return;
			}
			costText.gameObject.SetActive (show);
		}
		
		public void ShowRubyCost(bool show)
		{
			if (rubyCostText == null)
			{
				return;
			}
			rubyCostText.gameObject.SetActive (show);
		}

		public void ShowRequiredLevel(bool show)
		{
			if (levelText == null)
			{
				return;
			}
			levelText.gameObject.SetActive (show);
		}

		public void ShowDamage(bool show)
		{
			if (damageText == null)
			{
				return;
			}
			damageText.gameObject.SetActive (show);
		}

        public void ShowDamageModificator(bool show)
        {
            if (damageModificatorText == null)
            {
                return;
            }
            damageModificatorText.gameObject.SetActive(show);
        }

        public void ShowDefence(bool show)
		{
			if (defenceText == null)
			{
				return;
			}
			defenceText.gameObject.SetActive (show);
		}

        public void ShowDefenceModificator(bool show)
        {
            if (defenceModificatorText == null)
            {
                return;
            }
            defenceModificatorText.gameObject.SetActive(show);
        }

        public void ShowAttackSpeed(bool show)
		{
			if (attackSpeedText == null)
			{
				return;
			}
			attackSpeedText.gameObject.SetActive (show);
		}
		
		public void ShowSpeed(bool show)
		{
			if (speedText == null)
			{
				return;
			}
			speedText.gameObject.SetActive (show);
		}
		
		public void ShowHpRegen(bool show)
		{
			if (hpRegen == null)
			{
				return;
			}
			hpRegen.gameObject.SetActive (show);
		}
		
		public void ShowHpBoost(bool show)
		{
			if (hpBoost == null)
			{
				return;
			}
			hpBoost.gameObject.SetActive (show);
		}
		
		public void ShowCritChance(bool show)
		{
			if (critChance == null)
			{
				return;
			}
			critChance.gameObject.SetActive (show);
		}
		
		public void ShowCritMultiplier(bool show)
		{
			if (critMultiplier == null)
			{
				return;
			}
			critMultiplier.gameObject.SetActive (show);
		}
		
		public void ShowShieldBlockChance(bool show)
		{
			if (shieldBlockChance == null)
			{
				return;
			}
			shieldBlockChance.gameObject.SetActive (show);
		}

        public void UpdateData(Entity item, uint ownerLevel, EntityManager manager)
        {
            UpdateData(item, ownerLevel, manager, defaultLevelFormat);
        }
        IEnumerator loadIcon(Entity item, EntityManager manager)
        {
	        var spriteRef = manager.GetComponentData<ItemIcon>(item).Sprite;
	        
	        if (spriteRef.LoadingStatus == ObjectLoadingStatus.None)
	        {
		        spriteRef.LoadAsync();   
	        }
	        
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
	        if (spriteRef.Result == false)
	        {
		        yield break;
	        }
			
	        ItemIcon = spriteRef.Result;
        }

        public void RefreshIcon(EntityManager manager)
        {
	        if (enabled && gameObject.activeInHierarchy)
	        {
		        StartCoroutine(loadIcon(Item, manager));	
	        }
        }

		public void UpdateData(Entity item, uint ownerLevel, EntityManager manager, string levelInfoFormat)
		{
			Item = item;

			RefreshIcon(manager);
			
			ItemName = manager.GetSharedComponentManaged<ItemName>(item).ToString();

			ShowDamageModificator(false);
			ShowDefenceModificator(false);
			ShowGoldCost(false);
			ShowRubyCost(false);
			ShowCritChance(false);
			ShowCritMultiplier(false);
			ShowHpBoost(false);
			ShowHpRegen(false);
			ShowAttackSpeed(false);
			ShowSpeed(false);
			ShowShieldBlockChance(false);
			ShowRequiredLevel(false);

			ItemDescription = "";//template.GetDescription();
			if (manager.HasComponent<Price>(item))
			{
				var price = manager.GetComponentData<Price>(item);
				if (price.Value > 0)
				{
					ShowGoldCost(true);
					Cost = string.Format("{0:N0}", price.Value);
				}
				else
				{
					ShowGoldCost(false);
				}
			}
			
			ShowRubyCost(false);
			// if (template.PremiumCost > 0)
			// {
			// 	ShowRubyCost(true);
			// 	RubyCost = string.Format("{0:N0}", template.PremiumCost);
			// }
			// else
			// {
			// 	ShowRubyCost(false);
			// }
            
           	if (manager.HasComponent<Defense>(item))
			{
				Defence = string.Format("{0:0.0}", manager.GetComponentData<Defense>(item).Value);
				ShowDefence(true);
			}
			else
			{
				ShowDefence(false);
			}

   //         if (template.AdditionalModificators.DefenceModificators.Count > 0)
   //         {
   //             ShowDefenceModificator(true);
   //             DefenceModificator = getModificatorText(template.AdditionalModificators.DefenceModificators[0]);
   //         }
   //         else
   //         {
   //             ShowDefenceModificator(false);
   //         }

   //         var blockChanceItem = template as IBlockChanceItem;
   //         if (blockChanceItem != null && Mathf.Abs(blockChanceItem.BlockChance) > FMath.KINDA_SMALL_NUMBER)
   //         {
   //             ShieldBlockChance = string.Format(blockChanceItem.BlockChance > 0 ? "+{0:0}%" : "{0:0}%", Utility.GetIntPercent(blockChanceItem.BlockChance));
   //             ShowShieldBlockChance(true);
   //         }
   //         else
   //         {
   //             ShowShieldBlockChance(false);
   //         }

   //         if (ownerLevel < template.MinimumLevel)
			//{
			//	ShowRequiredLevel(true);
			//	RequiredLevel = string.Format(levelInfoFormat, template.MinimumLevel);
			//}
			//else
			//{
			//	ShowRequiredLevel(false);
			//}


   //         GameFramework.CharacteristicModificator attackSpeedModificator = null;

           if (manager.HasComponent<Damage>(item))
           {
               var damage = manager.GetComponentData<Damage>(item);
               Damage = string.Format("{0:0.0}", damage.Value);
               ShowDamage(true);

            //    var op = weapon.AttackSpeedModificator.Operator;
            //    var val = weapon.AttackSpeedModificator.Value;

            //    if (op == GameFramework.ModificatorOperators.ADD && Math.Abs(val) < FMath.KINDA_SMALL_NUMBER
            //        || op != GameFramework.ModificatorOperators.ADD && Math.Abs(val - 1.0f) < FMath.KINDA_SMALL_NUMBER)
            //    {
            //        //
            //    }
            //    else
            //    {
            //        attackSpeedModificator = weapon.AttackSpeedModificator;
            //    }
           }
           else
           {
               ShowDamage(false);
           }

   //         if (template.AdditionalModificators.DamageModificators.Count > 0)
   //         {
   //             ShowDamageModificator(true);
   //             DamageModificator = getModificatorText(template.AdditionalModificators.DamageModificators[0]);
   //         }
   //         else
   //         {
   //             ShowDamageModificator(false);
   //         }

   //         if (attackSpeedModificator == null && template.AdditionalModificators.AttackSpeedModificators.Count > 0)
			//{
			//	attackSpeedModificator = template.AdditionalModificators.AttackSpeedModificators[0];
			//}
            
			//if (attackSpeedModificator != null)
			//{
			//	ShowAttackSpeed(true);
			//	AttackSpeed = getModificatorText(attackSpeedModificator);
			//}
			//else
			//{
			//	ShowAttackSpeed(false);
			//}
            
			//var speedModificators = template.AdditionalModificators.WalkingSpeedModificators;
			//if (speedModificators.Count > 0)
			//{
			//	ShowSpeed(true);
			//	var modificator = speedModificators[0];
			//	Speed = getModificatorText(modificator);
			//}
			//else
			//{
			//	ShowSpeed(false);
			//}
            
			//var hpRegenModificators = template.AdditionalModificators.HitPointsRegenModificators;
			//if (hpRegenModificators.Count > 0)
			//{
			//	ShowHpRegen(true);
			//	var modificator = hpRegenModificators[0];
			//	HpRegen = getModificatorText(modificator);
			//}
			//else
			//{
			//	ShowHpRegen(false);
			//}
            
			//var hpBoostModificators = template.AdditionalModificators.HitPointsModificators;
			//if (hpBoostModificators.Count > 0)
			//{
			//	ShowHpBoost(true);
			//	var modificator = hpBoostModificators[0];
			//	HpBoost = getModificatorText(modificator);
			//}
			//else
			//{
			//	ShowHpBoost(false);
			//}
			
			//var critChanceModificators = template.AdditionalModificators.CritChanceModificators;
			//if (critChanceModificators.Count > 0)
			//{
			//	ShowCritChance(true);
			//	var modificator = critChanceModificators[0];
			//	CritChance = getModificatorText(modificator, ModificatorDisplayOptions.Percent);
			//}
			//else
			//{
			//	ShowCritChance(false);
			//}
			
			//var critMultiplierModificators = template.AdditionalModificators.CritMultipliierModificators;
			//if (critMultiplierModificators.Count > 0)
			//{
			//	ShowCritMultiplier(true);
			//	var modificator = critMultiplierModificators[0];
			//	CritMultiplier = getModificatorText(modificator, ModificatorDisplayOptions.Multiplier);
			//}
			//else
			//{
			//	ShowCritMultiplier(false);
			//}
		}
		
		//string getModificatorText(GameFramework.CharacteristicModificator modificator, ModificatorDisplayOptions options = ModificatorDisplayOptions.Default)
		//{
		//	string format = "";
		//	float val = modificator.Value;
            
		//	switch (modificator.Operator)
		//	{
  //              case GameFramework.ModificatorOperators.ADD_ACTUAL:
		//		case GameFramework.ModificatorOperators.ADD:
		//			format = modificator.Value > 0 ? "+{0:0.0}" : "{0:0.0}";
		//			if (options == ModificatorDisplayOptions.Percent)
		//			{
		//				format += "%";
		//			}
		//			break;
		//		case GameFramework.ModificatorOperators.MULTIPLY_BASE:
		//		case GameFramework.ModificatorOperators.MULTIPLY_ACTUAL:

		//			if (options == ModificatorDisplayOptions.Multiplier)
		//			{
		//				format = "x{0}";
		//			}
		//			else
		//			{
  //                      int percent = Utility.FloatToPercent(modificator.Value);
						
		//				format = percent > 0 ? "+{0}%" : "{0}%";
		//				val = percent;
		//			}
					
		//			break;
		//		default:
		//			throw new ArgumentOutOfRangeException();
		//	}
            
		//	return string.Format(format, val);
		//}

        public void OnPushedToPool()
        {
            iconImage.sprite = null;
            enableTexts(this, false);
        }

        public void OnPulledFromPool()
        {
            enableTexts(this, true);
        }

        static void enableTexts(InventoryItemInfo itemInfo, bool on)
        {
            enableText(itemInfo.itemNameText, on);
            enableText(itemInfo.itemDescriptionText, on);
            enableText(itemInfo.sellCost, on);
            enableText(itemInfo.costText, on);
            enableText(itemInfo.rubyCostText, on);
            enableText(itemInfo.levelText, on);
            enableText(itemInfo.damageText, on);
            enableText(itemInfo.defenceText, on);
            enableText(itemInfo.attackSpeedText, on);
            enableText(itemInfo.speedText, on);
            enableText(itemInfo.hpRegen, on);
            enableText(itemInfo.hpBoost, on);
            enableText(itemInfo.critChance, on);
            enableText(itemInfo.critMultiplier, on);
            enableText(itemInfo.shieldBlockChance, on);
        }

        static void enableText(TextUI t, bool on)
        {
            if (t == null)
            {
                return;
            }
            t.enabled = on;
        }
    }
}
