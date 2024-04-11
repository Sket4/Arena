using System.Collections.Generic;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class XpPointManagementUI : GameUIBase
    {
        [Header("Characteristics")]
        [SerializeField]
        UIBase characteristicsWindow = default;

        [SerializeField]
        Button characteristicsButton = default;

        [SerializeField]
        UnityEngine.UI.Button confirmButton = default;

        [SerializeField]
        TextUI currentPoints = default;

        [SerializeField]
        CharacteristicUpgradeUI damageUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI defenceUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI speedUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI attackSpeedUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI hpUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI hpRegenUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI critChanceUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI critMultiplierUpgradePointsUI = default;

        [SerializeField]
        CharacteristicUpgradeUI blockChanceUpgradePointsUI = default;

        [SerializeField]
        LocalizedStringAsset currentPointsFormat = default;

        [Header("Skills")]
        [SerializeField]
        UIBase skillWindow = default;

        [SerializeField]
        Button skillsButton = default;

        [SerializeField]
        Transform skillContainer = default;

        [SerializeField]
        SkillUpgradeUI skillUpgradePrefab = default;

        [SerializeField]
        LocalizedStringAsset damageSkillUpgradeLabel = default;

        [SerializeField]
        LocalizedStringAsset cooldownUpgradeLabel = default;

        Pool<SkillUpgradeUI> skillUpgradeUiPool;
        List<SkillUpgradeUI> activeSkillUiList = new List<SkillUpgradeUI>();
        List<SkillUpgradeUI> createdSkillUiList = new List<SkillUpgradeUI>();

        public interface ICharacteristicUpgrade
        {
            bool CanUpgradeCharacteristicByLevel(int upgradeLevel);
            int GetRequiredPointsForUpgrade(int upgradeLevel);
            bool AddUpgrade(int level);
            int UpgradeLevel { get; }
            float GetBonusValueForLevel(int level);
        }

        public interface ISkillUpgrade
        {
            int SkillId { get; }
            //CharacteristicModificator DamageModificator { get; }
            //CharacteristicModificator CooldownModificator { get; }
            int CooldownUpgradeLevel { get; }
            int CommonUpgradeLevel { get; }
            void AddCooldownUpgrade(int levelAddition);
            void AddCommonUpgrade(int levelAddition);
            bool CanUpgradeCommonByLevel(int levelAddition);
            bool CanUpgradeCooldownByLevel(int levelAddition);
            int GetRequiredPointsToUpgdateCommon(int levelAddition);
            int GetRequiredPointsToUpgdateCooldown(int levelAddition);
            float GetCommonUpgradeBonusForLevel(int level);
            float GetCooldownUpgradeBonusForLevel(int level);
        }

        public const int MAXIMUM_CHARACTERISTIC_UPGRADE_LEVEL = 10;

        private SkillUpgradeUI createSkillUpgradeUI()
        {
            var instance = Instantiate(skillUpgradePrefab);

            var container = getSkillUiContainer();
            instance.transform.SetParent(container.transform);
            instance.CooldownCounter.OnValueChanged += onCounterChanged;
            instance.CommonCounter.OnValueChanged += onCounterChanged;
            createdSkillUiList.Add(instance);
            return instance;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < createdSkillUiList.Count; i++)
            {
                var instance = createdSkillUiList[i];
                if (instance == null)
                {
                    continue;
                }
                instance.CooldownCounter.OnValueChanged -= onCounterChanged;
                instance.CommonCounter.OnValueChanged -= onCounterChanged;

                Destroy(instance.gameObject);
            }
            createdSkillUiList.Clear();
        }

        void onCounterChanged(string val)
        {
            UpdateUI();
        }

        static Transform getSkillUiContainer()
        {
            const string containerName = "SkillUpgradeUI_Container";
            var container = GameObject.Find(containerName);
            if (container == null)
            {
                container = new GameObject(containerName);
            }
            return container.transform;
        }

        bool isInitialized = false;

        class CharacteristicUpgradeInfo
        {
            public CharacteristicUpgradeUI UI;
            public IntCounterUI Counter;
            public ICharacteristicUpgrade Upgrade;
        }

        class SkillUpgradeInfo
        {
            public SkillUpgradeUI SkillUI;
            public ISkillUpgrade Upgrade;
        }

        List<CharacteristicUpgradeInfo> upgrades = new List<CharacteristicUpgradeInfo>();
        List<SkillUpgradeInfo> skillUpgrades = new List<SkillUpgradeInfo>();

        void registerUpgrade(CharacteristicUpgradeUI ui, ICharacteristicUpgrade upgrade)
        {
            if (upgrade == null)
            {
                return;
            }
            var info = new CharacteristicUpgradeInfo();
            ui.Counter.MinValue = 0;
            ui.Counter.MaxValue = MAXIMUM_CHARACTERISTIC_UPGRADE_LEVEL;
            info.UI = ui;
            info.Counter = ui.Counter;
            info.Upgrade = upgrade;
            upgrades.Add(info);
        }

        private int getRequiredPoints()
        {
            int result = 0;

            foreach(var upgrade in upgrades)
            {
                result += calculateRequiredPoints(upgrade.Counter.CurrentValue, upgrade.Upgrade);    
            }

            foreach(var skillUpgrade in skillUpgrades)
            {
                result += calculateRequiredPointsForSkill(skillUpgrade);
            }

            return result;
        }

        int calculateRequiredPointsForSkill(SkillUpgradeInfo skillInfo)
        {
            var upgrade = skillInfo.Upgrade;
            var currentCommonValue = skillInfo.SkillUI.CommonCounter.CurrentValue;
            var currentCooldownValue = skillInfo.SkillUI.CooldownCounter.CurrentValue;

            var lvlCooldown = currentCooldownValue - upgrade.CooldownUpgradeLevel;
            lvlCooldown = upgrade.GetRequiredPointsToUpgdateCooldown(lvlCooldown);

            var lvlCommon = currentCommonValue - upgrade.CommonUpgradeLevel;
            lvlCommon = upgrade.GetRequiredPointsToUpgdateCommon(lvlCommon);

            return lvlCommon + lvlCooldown;
        }

        int calculateRequiredPoints(int currentValue, ICharacteristicUpgrade upgrade)
        {
            var lvl = currentValue - upgrade.UpgradeLevel;
            if (lvl > 0)
            {
                return upgrade.GetRequiredPointsForUpgrade(lvl);
            }
            return 0;
        }

        public void Confirm()
        {
            // var required = getRequiredPoints();
            // if(required > playerCharacter.AvailableUpgradePoints)
            // {
            //     Debug.Log("Not enough upgrade points");
            //     return;
            // }
            //
            // bool needSave = false;
            //
            // foreach(var upgrade in upgrades)
            // {
            //     var level = upgrade.Counter.CurrentValue - upgrade.Upgrade.UpgradeLevel;
            //     if (level > 0
            //         && upgrade.Upgrade.CanUpgradeCharacteristicByLevel(level) == false)
            //     {
            //         Debug.Log("Failed to upgrade " + upgrade.Counter.name);
            //         return;
            //     }    
            // }
            //
            // foreach (var skillUpgrade in skillUpgrades)
            // {
            //     var commonLevel = skillUpgrade.SkillUI.CommonCounter.CurrentValue - skillUpgrade.Upgrade.CommonUpgradeLevel;
            //     if (commonLevel > 0 && skillUpgrade.Upgrade.CanUpgradeCommonByLevel(commonLevel) == false)
            //     {
            //         Debug.LogError("Failed to upgrade common for skill " + skillUpgrade.Upgrade.SkillId);
            //         return;
            //     }
            //
            //     var cooldownLevel = skillUpgrade.SkillUI.CooldownCounter.CurrentValue - skillUpgrade.Upgrade.CooldownUpgradeLevel;
            //     if (cooldownLevel > 0 && skillUpgrade.Upgrade.CanUpgradeCooldownByLevel(cooldownLevel) == false)
            //     {
            //         Debug.LogError("Failed to upgrade cooldown for skill " + skillUpgrade.Upgrade.SkillId);
            //         return;
            //     }
            // }
            //
            // foreach (var upgrade in upgrades)
            // {
            //     var level = upgrade.Counter.CurrentValue - upgrade.Upgrade.UpgradeLevel;
            //
            //     if (level > 0)
            //     {
            //         if(upgrade.Upgrade.AddUpgrade(level))
            //         {
            //             needSave = true;
            //         }
            //         else
            //         {
            //             Debug.LogError("Failed to upgrade " + upgrade.UI);
            //         }
            //     }
            // }
            //
            // foreach(var skillUpgrade in skillUpgrades)
            // {
            //     var commonLevel = skillUpgrade.SkillUI.CommonCounter.CurrentValue - skillUpgrade.Upgrade.CommonUpgradeLevel;
            //     if(commonLevel > 0)
            //     {
            //         skillUpgrade.Upgrade.AddCommonUpgrade(commonLevel);
            //         needSave = true;
            //     }
            //
            //     var cooldownLevel = skillUpgrade.SkillUI.CooldownCounter.CurrentValue - skillUpgrade.Upgrade.CooldownUpgradeLevel;
            //     if (cooldownLevel > 0)
            //     {
            //         skillUpgrade.Upgrade.AddCooldownUpgrade(cooldownLevel);
            //         needSave = true;
            //     }
            // }
            //
            // if(needSave)
            // {
            //     Debug.LogError("Not implemented");
            //     //(EndlessLobbyGameManager.Instance as EndlessLobbyGameManager).SaveGameWithPlayerData();    
            //     UpdateUI();
            // }
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            UpdateUI();
        }

        protected override void Awake()
        {
            base.Awake();
            skillUpgradeUiPool = new Pool<SkillUpgradeUI>(createSkillUpgradeUI, int.MaxValue);
            skillUpgradeUiPool.CreateObjects(10);
        }

        protected override void OnVisible()
        {
            base.OnVisible();

            if(isInitialized == false)
            {
                registerUpgrade(damageUpgradePointsUI, null);//playerCharacter.DamageUpgrade);
                registerUpgrade(defenceUpgradePointsUI, null);//playerCharacter.DefenceUpgrade);
                registerUpgrade(speedUpgradePointsUI, null);//playerCharacter.SpeedUpgrade);
                registerUpgrade(attackSpeedUpgradePointsUI, null);//playerCharacter.AttackSpeedUpgrade);
                registerUpgrade(hpUpgradePointsUI, null);//playerCharacter.HitPointsUpgrade);
                registerUpgrade(hpRegenUpgradePointsUI, null);//playerCharacter.HitPointsRegenUpgrade);
                registerUpgrade(critChanceUpgradePointsUI, null);//playerCharacter.CritChanceUpgrade);
                registerUpgrade(critMultiplierUpgradePointsUI, null);//playerCharacter.CritMultiplierUpgrade);
                registerUpgrade(blockChanceUpgradePointsUI, null);//playerCharacter.BlockChanceUpgrade);
            
                isInitialized = true;    
            }
            
            foreach(var upgrade in upgrades)
            {
                upgrade.Counter.CurrentValue = upgrade.Upgrade.UpgradeLevel;    
            }
            
            ShowCharacteristics();
            
            var inactiveSkillContainer = getSkillUiContainer();
            foreach(var activeSkill in activeSkillUiList)
            {
                activeSkill.transform.SetParent(inactiveSkillContainer);
                skillUpgradeUiPool.Set(activeSkill);
            }
            activeSkillUiList.Clear();
            
            // if(playerCharacter != null)
            // {
            //     skillUpgrades.Clear();
            //     var skillCount = playerCharacter.SkillInstanceCount;
            //     for (int i = 0; i < skillCount; i++)
            //     {
            //         var skill = playerCharacter.GetSkillInstanceAtIndex(i); 
            //         var skillUI = skillUpgradeUiPool.Get();
            //         if(skillUI.gameObject.activeSelf == false)
            //         {
            //             skillUI.gameObject.SetActive(true);
            //         }
            //         activeSkillUiList.Add(skillUI);
            //         skillUI.Icon = skill.Icon;
            //
            //         var skillUpgrade = playerCharacter.GetSkillUpgradeBySkillId(skill.Id);
            //         if(skillUpgrade == null)
            //         {
            //             skillUpgrade = playerCharacter.CreateSkillUpgrade(skill.Id);
            //         }
            //
            //         skillUI.CommonCounter.MinValue = 0;
            //         skillUI.CommonCounter.MaxValue = EndlessPlayerCharacterTemplate.MAX_SKILL_UPGRADE_LEVEL;
            //         skillUI.CommonCounter.CurrentValue = skillUpgrade.CommonUpgradeLevel;
            //
            //         skillUI.CooldownCounter.MinValue = 0;
            //         skillUI.CooldownCounter.MaxValue = EndlessPlayerCharacterTemplate.MAX_SKILL_UPGRADE_LEVEL;
            //         skillUI.CooldownCounter.CurrentValue = skillUpgrade.CooldownUpgradeLevel;
            //         
            //         var newInfo = new SkillUpgradeInfo();
            //         newInfo.SkillUI = skillUI;
            //         newInfo.Upgrade = skillUpgrade;
            //         skillUpgrades.Add(newInfo);
            //
            //         skillUI.transform.SetParent(skillContainer, false);
            //         skillUI.transform.localScale = Vector3.one;
            //         skillUI.transform.localPosition = Vector3.one;
            //         skillUI.transform.localRotation = Quaternion.identity;
            //     }
            //}

            UpdateUI();
        }

        protected override void OnHidden()
        {
            base.OnHidden();

            for (int i = 0; i < createdSkillUiList.Count; i++)
            {
                SkillUpgradeUI instance = createdSkillUiList[i];
                if (instance.gameObject.activeSelf)
                {
                    instance.gameObject.SetActive(false);
                }
            }
        }

        public void ShowCharacteristics()
        {
            characteristicsWindow.SetVisible(true);
            skillWindow.SetVisible(false);
            skillsButton.interactable = true;
            characteristicsButton.interactable = false;

            //blockChanceUpgradePointsUI.gameObject.SetActive(playerCharacter.HasBlockChanceCharacteristic());
        }

        public void ShowSkills()
        {
            characteristicsWindow.SetVisible(false);
            skillWindow.SetVisible(true);
            skillsButton.interactable = false;
            characteristicsButton.interactable = true;
        }

        public void UpdateUI()
        {
            if(OwnerEntity == Entity.Null)
            {
                return;
            }

            var required = getRequiredPoints();
            // var points = playerCharacter.AvailableUpgradePoints;
            //
            // currentPoints.text = string.Format(currentPointsFormat, points - required);
            //
            // bool canIncrement = required < points;
            //
            // foreach(var upgrade in upgrades)
            // {
            //     upgrade.Counter.EnableIncrement(canIncrement);    
            //     upgrade.Counter.MinValue = upgrade.Upgrade.UpgradeLevel;
            //     var bonus = upgrade.Upgrade.GetBonusValueForLevel(upgrade.UI.Counter.CurrentValue);
            //     var bonusStr = (bonus > 0 ? "+" : "") + bonus;
            //     //bonus = Mathf.Round(bonus);
            //     upgrade.UI.Label = string.Format(upgrade.UI.TemplateText, bonusStr);
            // }
            //
            // foreach (var skillUpgrade in skillUpgrades)
            // {
            //     var skillUI = skillUpgrade.SkillUI;
            //
            //     skillUI.CommonCounter.EnableIncrement(canIncrement);
            //     skillUI.CooldownCounter.EnableIncrement(canIncrement);
            //     skillUI.CommonCounter.MinValue = skillUpgrade.Upgrade.CommonUpgradeLevel;
            //     skillUI.CooldownCounter.MinValue = skillUpgrade.Upgrade.CooldownUpgradeLevel;
            //
            //     var skill = playerCharacter.GetSkillInstanceById(skillUpgrade.Upgrade.SkillId);
            //
            //     var commonBonus = skillUpgrade.Upgrade.GetCommonUpgradeBonusForLevel(skillUpgrade.SkillUI.CommonCounter.CurrentValue);
            //     if (skill is IDamageSkill)
            //     {
            //         var damagePercent = Utility.FloatToPercent(commonBonus);
            //         var damageString = (damagePercent > 0 ? "+" : "") + damagePercent;
            //         skillUI.CommonCounterLabel = string.Format(damageSkillUpgradeLabel, damageString);
            //         
            //     }
            //     else if (skill is Skills.ICommonSkillUpgrade)
            //     {
            //         var label = (skill as Skills.ICommonSkillUpgrade).GetUpgradeLabelWithBonus(commonBonus);
            //         skillUI.CommonCounterLabel = label;
            //     }
            //
            //     var cooldownBonus = skillUpgrade.Upgrade.GetCooldownUpgradeBonusForLevel(skillUpgrade.SkillUI.CooldownCounter.CurrentValue);
            //     var cooldownPercent = Utility.FloatToPercent(cooldownBonus);
            //     var cooldownString = (cooldownPercent > 0 ? "+" : "") + cooldownPercent;
            //
            //     skillUI.CooldownCounterLabel = string.Format(cooldownUpgradeLabel, cooldownString);
            // }

            confirmButton.interactable = required > 0;
        }
    }
}
