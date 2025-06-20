using System.Collections.Generic;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Items;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using LocalizedString = UnityEngine.Localization.LocalizedString;

namespace Arena.Client.UI
{
    public class AbilitiesUI : GameUIBase
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
        LocalizedString currentPointsFormat = default;

        [Header("Skills")]
        [SerializeField]
        UIBase skillWindow = default;

        [SerializeField]
        Button skillsButton = default;

        [SerializeField]
        Transform skillContainer = default;

        [SerializeField]
        SkillUpgradeUI skillUpgradePrefab = default;

        [SerializeField] private LocalizedString requiredLevelText;

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

        public const int MAXIMUM_CHARACTERISTIC_UPGRADE_LEVEL = 10;

        private SkillUpgradeUI createSkillUpgradeUI()
        {
            var instance = Instantiate(skillUpgradePrefab);

            var container = getSkillUiContainer();
            instance.transform.SetParent(container.transform);
            instance.Counter.OnValueChanged += onCounterChanged;
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
                instance.Counter.OnValueChanged -= onCounterChanged;

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

        class AbilityUpgradeInfo
        {
            public SkillUpgradeUI AbilityUI;
            public Entity AbilityPrefab;
            public Entity AbilityEntity;
            public bool LevelMatch;
        }

        List<CharacteristicUpgradeInfo> upgrades = new();
        List<AbilityUpgradeInfo> abilityUpgrades = new();

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

            foreach(var skillUpgrade in abilityUpgrades)
            {
                result += calculateRequiredPointsForAbility(skillUpgrade);
            }

            return result;
        }

        int calculateRequiredPointsForAbility(AbilityUpgradeInfo abilityInfo)
        {
            var currentValue = abilityInfo.AbilityUI.Counter.CurrentValue;

            int result;
            
            if (abilityInfo.AbilityEntity != Entity.Null)
            {
                result = currentValue - GetData<Level>(abilityInfo.AbilityEntity).Value;    
            }
            else
            {
                result = currentValue;
            }
            
            return result;
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
            
            abilityUpgrades.Clear();
            
            
            
            var characterTemplate = Arena.SharedUtility.GetCharacterTemplate(GetData<CharacterClassData>().Value);
            using (var dbQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MainDatabaseTag>(), ComponentType.ReadOnly<IdToEntity>()))
            {
                var db = dbQuery.GetSingletonBuffer<IdToEntity>();
                var abilityPrefabs = new List<Entity>();

                foreach (var availableAbility in characterTemplate.AvailableAbilities)
                {
                    if (IdToEntity.TryGetEntityById(db, availableAbility.AbilityID, out var abilityPrefab) == false)
                    {
                        Debug.LogError($"Failed to find ability prefab with id {availableAbility.AbilityID}");
                        continue;
                    }
                    abilityPrefabs.Add(abilityPrefab);
                }
                abilityPrefabs.Sort((a,b) => GetData<MinimalLevel>(a).Value.CompareTo(GetData<MinimalLevel>(b).Value));

                var currentLevel = GetData<Level>().Value;
                var activeAbilities = GetBuffer<AbilityElement>();
                
                foreach (var abilityPrefab in abilityPrefabs)
                {
                    //var skill = playerCharacter.GetSkillInstanceAtIndex(i); 
                    var skillUI = skillUpgradeUiPool.Get();
                    if(skillUI.gameObject.activeSelf == false)
                    {
                        skillUI.gameObject.SetActive(true);
                    }
                    activeSkillUiList.Add(skillUI);
                    skillUI.Icon = GetSharedComponentManaged<AbilityIcon>(abilityPrefab).Sprite;
                    skillUI.Label = GetSharedComponentManaged<ItemName>(abilityPrefab).ToString();
                    var abilityMinimalLevel = GetData<MinimalLevel>(abilityPrefab).Value;
                    skillUI.RequiredLevel = string.Format(requiredLevelText.GetLocalizedString(), abilityMinimalLevel);
                    
                    // var skillUpgrade = playerCharacter.GetSkillUpgradeBySkillId(skill.Id);
                    // if(skillUpgrade == null)
                    // {
                    //     skillUpgrade = playerCharacter.CreateSkillUpgrade(skill.Id);
                    // }
            
                    skillUI.Counter.MinValue = 0;
                    skillUI.Counter.MaxValue = 10;
                     
                    var newInfo = new AbilityUpgradeInfo();
                    newInfo.AbilityUI = skillUI;
                    newInfo.AbilityPrefab = abilityPrefab;
                    newInfo.AbilityEntity = Entity.Null;

                    foreach (var ability in activeAbilities)
                    {
                        var id = GetData<AbilityID>(ability.AbilityEntity);
                        if (id == GetData<AbilityID>(abilityPrefab))
                        {
                            newInfo.AbilityEntity = ability.AbilityEntity;
                        }
                    }

                    if (newInfo.AbilityEntity != Entity.Null)
                    {
                        skillUI.Counter.CurrentValue = GetData<Level>(newInfo.AbilityEntity).Value;
                    }
                    else
                    {
                        skillUI.Counter.CurrentValue = 0;
                    }
                    
                    //newInfo.Upgrade = skillUpgrade;
                    abilityUpgrades.Add(newInfo);
            
                    skillUI.transform.SetParent(skillContainer, false);
                    skillUI.transform.localScale = Vector3.one;
                    skillUI.transform.localPosition = Vector3.one;
                    skillUI.transform.localRotation = Quaternion.identity;
                }
            }
            
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
            //characteristicsWindow.SetVisible(true);
            skillsButton.interactable = true;
            characteristicsButton.interactable = false;

            //blockChanceUpgradePointsUI.gameObject.SetActive(playerCharacter.HasBlockChanceCharacteristic());
        }

        public void ShowSkills()
        {
            //characteristicsWindow.SetVisible(false);
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
            var points = GetData<AbilityPoints>().Count;
            //
            currentPoints.text = string.Format(currentPointsFormat.GetLocalizedString(), points - required);
            
            bool hasPoints = required < points;
            
            // foreach(var upgrade in upgrades)
            // {
            //     upgrade.Counter.EnableIncrement(hasPoints);    
            //     upgrade.Counter.MinValue = upgrade.Upgrade.UpgradeLevel;
            //     var bonus = upgrade.Upgrade.GetBonusValueForLevel(upgrade.UI.Counter.CurrentValue);
            //     var bonusStr = (bonus > 0 ? "+" : "") + bonus;
            //     //bonus = Mathf.Round(bonus);
            //     upgrade.UI.Label = string.Format(upgrade.UI.TemplateText, bonusStr);
            // }
            
            foreach (var upgrade in abilityUpgrades)
            {
                var ui = upgrade.AbilityUI;
                
                var abilityMinimalLevel = GetData<MinimalLevel>(upgrade.AbilityPrefab).Value;
                var currentLevel= GetData<Level>().Value;
                upgrade.LevelMatch = currentLevel >= abilityMinimalLevel;

                if (upgrade.LevelMatch)
                {
                    ui.Counter.EnableIncrement(hasPoints);
                    ui.Counter.EnableDecrement(true);
                    int minValue = 0;
                    if (upgrade.AbilityEntity != Entity.Null)
                    {
                        minValue = GetData<Level>(upgrade.AbilityEntity).Value;
                    }
                    ui.Counter.MinValue = minValue;    
                }
                else
                {
                    ui.Counter.EnableIncrement(false);
                    ui.Counter.EnableDecrement(false);
                    ui.Counter.MinValue = 0;
                }
                
                var isActiveSkill = true;

                if (isActiveSkill)
                {
                    var playerAbilities = GetData<PlayerAbilities>();
                        
                    ui.ActivateButton.gameObject.SetActive(true);
                    ui.ActivateButton.interactable = upgrade.LevelMatch 
                                                     && upgrade.AbilityEntity != Entity.Null 
                                                     && playerAbilities.Contains(upgrade.AbilityEntity) == false;
                        
                    ui.PassiveAbilityLabel.SetActive(false);
                }
                else
                {
                    ui.PassiveAbilityLabel.SetActive(true);
                    ui.ActivateButton.gameObject.SetActive(false);
                }
            
                //var skill = playerCharacter.GetSkillInstanceById(upgrade.AbilityEntity);
                // var commonBonus = skillUpgrade.Upgrade.GetCommonUpgradeBonusForLevel(skillUpgrade.SkillUI.CommonCounter.CurrentValue);
                // var label = (skill as Skills.ICommonSkillUpgrade).GetUpgradeLabelWithBonus(commonBonus);
                // skillUI.Description = label;
            }

            confirmButton.gameObject.SetActive(required > 0);
        }
    }
}
