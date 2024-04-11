// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using TzarGames.Common;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using TzarGames.GameCore;

namespace TzarGames.GameFramework.UI
{
    public class UICharacterHUD : GameUIBase
    {
        [SerializeField]
        float updateInterval = 0.1f;

        

        [Header("Target")]
        [SerializeField]
        RectTransform targetInfoPanel = default;

        [SerializeField]
        Slider targetHealthSlider = default;

        [SerializeField]
        TextUI targetNameText = default;

	    [SerializeField] private LocalizedStringAsset targetFormatString = default;
        
        private Entity lastTarget = Entity.Null;
        float lastUpdateTime;
        
        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            updateCharacterStats();
        }

        System.Text.StringBuilder targetTextBuilder = new System.Text.StringBuilder(256);
        System.Text.StringBuilder hpTextBuilder = new System.Text.StringBuilder(256);

        protected virtual void updateCharacterStats()
        {
            if(HasData<Target>() == false)
            {
                return;
            }

            var target = GetData<Target>();
            
			if(target.Value != Entity.Null && targetInfoPanel != null)
			{
				if(targetInfoPanel.gameObject.activeSelf == false)
				{
					targetInfoPanel.gameObject.SetActive(true);
				}

                if(EntityManager.HasComponent<Health>(target.Value))
                {
                    var targetHealth = EntityManager.GetComponentData<Health>(target.Value);
                    var val = targetHealth.ActualHP / targetHealth.ModifiedHP;

                    if (Mathf.Abs(targetHealthSlider.value - val) > FMath.KINDA_SMALL_NUMBER)
                    {
                        targetHealthSlider.value = val;
                    }
                }

				string nameOfTarget = null;
                
				//var targetCharacterTemplate = targetCharacter.TemplateInstance;

                if (target.Value != lastTarget)
                {
                    lastTarget = target.Value;

                    targetTextBuilder.Length = 0;
                    //if(targetCharacterTemplate.localizedName != null)
                    //{
                    //    targetTextBuilder.Append(targetCharacterTemplate.localizedName.Text);    
                    //}
                    Level level = default;

                    if(EntityManager.HasComponent<Level>(target.Value))
                    {
                        level = EntityManager.GetComponentData<Level>(target.Value);
                    }
                    targetTextBuilder.Append(string.Format(targetFormatString.Text, level.Value));

                    nameOfTarget = targetTextBuilder.ToString();
                }
                
                if (string.IsNullOrEmpty(nameOfTarget) == false && string.Equals(targetNameText.text, nameOfTarget) == false)
				{
					targetNameText.text = nameOfTarget;	
				}
			}
			else
			{
				if(targetInfoPanel != null && targetInfoPanel.gameObject.activeSelf)
				{
					targetInfoPanel.gameObject.SetActive(false);
				}
			}
		}

		protected virtual void Update()
		{
			if(OwnerEntity == Entity.Null)
			{
                if (targetInfoPanel != null && targetInfoPanel.gameObject.activeSelf)
                {
                    targetInfoPanel.gameObject.SetActive(false);
                }

                return;
			}

            if(Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                updateCharacterStats();
            }
		}
	}
}

