// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using TzarGames.Common;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using TzarGames.GameCore;

namespace TzarGames.GameFramework.UI
{
    public class CharacterStatsUI : GameUIBase
    {

        [SerializeField]
        float updateInterval = 0.1f;

        [Header("Character stats")]
        [SerializeField]
        Slider health = default;

        [SerializeField]
        TextUI healthNumeric = default;

        [SerializeField]
        TextUI damage = default;

        [SerializeField]
        TextUI defence = default;

        [SerializeField]
        TextUI speed = default;

        [SerializeField]
        TextUI attackSpeed = default;

        [SerializeField]
        TextUI critChance = default;

        [SerializeField]
        TextUI critPower = default;

        [SerializeField]
        TextUI hpRegen = default;

        [SerializeField]
        Slider xp = default;

        [SerializeField]
        TextUI xpNumber = default;

        [SerializeField]
        TextUI level = default;

        private uint lastXpValue = uint.MaxValue;
        private uint lastCharacterLevel = uint.MaxValue;
        private float lastCharacterDamage = float.MaxValue;
        private float lastCharacterDefence = float.MaxValue;
        private float lastCharacterSpeed = float.MaxValue;
        private float lastCharacterAttackSpeed = float.MaxValue;
        private float lastCharacterCritChance = float.MaxValue;
        private float lastCharacterHpRegen = float.MaxValue;
        private float lastCharacterCritPower = float.MaxValue;
        private float lastActualHp = float.MaxValue;
        private float lastMaxHp = float.MaxValue;

        float lastUpdateTime;

        private void OnEnable()
        {
            if(OwnerEntity != Entity.Null && EntityManager.Exists(OwnerEntity))
            {
                UpdateCharacterStats();
            }
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            UpdateCharacterStats();
        }
        
        System.Text.StringBuilder hpTextBuilder = new System.Text.StringBuilder(256);


        protected virtual void UpdateCharacterStats()
        {
            if(HasData<Health>() == false)
            {
                return;
            }

	        var healthData = GetData<Health>();
            
            var actualHp = healthData.ActualHP;
            var maxHp = healthData.ModifiedHP;

            if (health != null)
            {
                var hp = actualHp / maxHp;
                if (Mathf.Abs(health.value - hp) > FMath.KINDA_SMALL_NUMBER)
                {
                    health.value = hp;
                }
            }

            var levelData = GetData<Level>();

            if (xp != null || xpNumber != null)
            {
                var xpNormalizedValue = 0.0f;
                uint xpValue = 0;

                if(HasData<XP>())
                {
                    var xpData = GetData<XP>();
                    xpValue = xpData.Value;
                    xpNormalizedValue = Level.GetNormalizedProgress(xpData, levelData);
                }
                
                if(Mathf.Abs(xp.value - xpNormalizedValue) > FMath.KINDA_SMALL_NUMBER)
                {
                    if (xp != null)
                    {
                        xp.value = xpNormalizedValue;
                    }
                }

                if(xpNumber != null && xpValue != lastXpValue)
                {
                    lastXpValue = xpValue;
                    xpNumber.text = string.Format("{0}/{1}", xpValue - levelData.XpForCurrentLevel, levelData.XpForNextLevel - levelData.XpForCurrentLevel);
                }
            }

            if (level != null && level.gameObject.activeInHierarchy)
            {
                var currentLevel = levelData.Value;
                if (lastCharacterLevel != currentLevel)
                {
                    lastCharacterLevel = currentLevel;
                    level.text = levelData.Value.ToString();
                }
            }

            if (healthNumeric != null 
                && healthNumeric.gameObject.activeInHierarchy
                && (Mathf.Abs(actualHp - lastActualHp) > FMath.KINDA_SMALL_NUMBER 
                    || Mathf.Abs(maxHp - lastMaxHp) > FMath.KINDA_SMALL_NUMBER))
            {
				lastMaxHp = maxHp;
				lastActualHp = actualHp;

                //var hpValues = string.Format("{0:0}/{1:0}", actualHp, maxHp);
                hpTextBuilder.Length = 0;
                hpTextBuilder.Append(actualHp.ToString("F1"));
                hpTextBuilder.Append('/');
                hpTextBuilder.Append(maxHp.ToString("F1"));

                var hpValues = hpTextBuilder.ToString();

                if (healthNumeric.text.Equals(hpValues) == false)
                {
                    healthNumeric.text = hpValues;
                }
            }
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("Characteristics");
#endif

            if (damage != null && damage.gameObject.activeInHierarchy)
            {
	            var currentDamage = GetData<Damage>().Value;
                if (Mathf.Abs(currentDamage - lastCharacterDamage) > FMath.KINDA_SMALL_NUMBER)
				{
					lastCharacterDamage = currentDamage;
                    damage.text = currentDamage.ToString("F1");
				}
			}
			
			if (defence != null && defence.gameObject.activeInHierarchy)
			{
				var currentDefence = GetData<Defense>().Value;

                if (Mathf.Abs(currentDefence - lastCharacterDefence) > FMath.KINDA_SMALL_NUMBER)
				{
					lastCharacterDefence = currentDefence;
					defence.text = currentDefence.ToString("F1");
				}
			}
			if (speed != null && speed.gameObject.activeInHierarchy)
			{
				var currentSpeed = GetData<Speed>().Value;
               if (Mathf.Abs(currentSpeed - lastCharacterSpeed) > FMath.KINDA_SMALL_NUMBER)
				{
					lastCharacterSpeed = currentSpeed;
					speed.text = currentSpeed.ToString("F1");
				}
				
			}
			if (attackSpeed != null && attackSpeed.gameObject.activeInHierarchy)
			{
				var currentAttackSpeed = GetData<AttackSpeed>().Value;
                if (Mathf.Abs(currentAttackSpeed - lastCharacterAttackSpeed) > FMath.KINDA_SMALL_NUMBER)
				{
					lastCharacterAttackSpeed = currentAttackSpeed;
					attackSpeed.text = currentAttackSpeed.ToString("F2");
				}
			}
            if (critChance != null && critChance.gameObject.activeInHierarchy && HasData<CriticalDamageChance>())
            {
                var currentCritChance = GetData<CriticalDamageChance>().Value;
                if (Mathf.Abs(currentCritChance - lastCharacterCritChance) > FMath.KINDA_SMALL_NUMBER)
                {
                    lastCharacterCritChance = currentCritChance;
                    critChance.text = $"{currentCritChance}%";
                }
            }
            if (critPower != null && critPower.gameObject.activeInHierarchy && HasData<CriticalDamageMultiplier>())
            {
                var currentCritPower = GetData<CriticalDamageMultiplier>().Value;
                if (Mathf.Abs(currentCritPower - lastCharacterCritPower) > FMath.KINDA_SMALL_NUMBER)
                {
                    lastCharacterCritPower = currentCritPower;
                    critPower.text = string.Format("{0}", currentCritPower.ToString("F2"));
                }
            }
            
           if (hpRegen != null && hpRegen.gameObject.activeInHierarchy)
           {
               var currentHpRegen = GetData<HealthRegeneration>().Value;
               if (Mathf.Abs(currentHpRegen - lastCharacterHpRegen) > FMath.KINDA_SMALL_NUMBER)
               {
                   lastCharacterHpRegen = currentHpRegen;
                   hpRegen.text = currentHpRegen.ToString("F2");
               }
           }

#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.EndSample();
#endif

		}

		protected virtual void Update()
		{
			if(OwnerEntity == Entity.Null)
			{
                return;
			}

            if(Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateCharacterStats();
            }
		}
	}
}

