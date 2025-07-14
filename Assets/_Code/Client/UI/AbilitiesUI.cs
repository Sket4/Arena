using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Items;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using Unity.Mathematics;
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
        [SerializeField] private LocalizedString damageMultiplierText;

        [Header("Activate ability panel")] 
        [SerializeField] private UIBase activateAbilityWindow;
        [SerializeField] private Button slot1_button;
        [SerializeField] private Image slot1_icon;
        [SerializeField] private Button slot2_button;
        [SerializeField] private Image slot2_icon;
        [SerializeField] private Button slot3_button;
        [SerializeField] private Image slot3_icon;
        [SerializeField] private Sprite emptySlotIcon;

        Pool<SkillUpgradeUI> skillUpgradeUiPool;
        List<SkillUpgradeUI> activeSkillUiList = new List<SkillUpgradeUI>();
        List<SkillUpgradeUI> createdSkillUiList = new List<SkillUpgradeUI>();

        private Entity currentAbilityToActivate = Entity.Null;

        public interface ICharacteristicUpgrade
        {
            bool CanUpgradeCharacteristicByLevel(int upgradeLevel);
            int GetRequiredPointsForUpgrade(int upgradeLevel);
            bool AddUpgrade(int level);
            int UpgradeLevel { get; }
            float GetBonusValueForLevel(int level);
        }

        //public const int MAXIMUM_CHARACTERISTIC_UPGRADE_LEVEL = 10;

        private SkillUpgradeUI createSkillUpgradeUI()
        {
            var instance = Instantiate(skillUpgradePrefab);

            var container = getSkillUiContainer();
            instance.transform.SetParent(container.transform);
            instance.Counter.OnValueChanged += onCounterChanged;
            createdSkillUiList.Add(instance);
            return instance;
        }

        protected override void Start()
        {
            base.Start();
            slot1_button.onClick.AddListener(() => activateCurrentAbility(1));
            slot2_button.onClick.AddListener(() => activateCurrentAbility(2));
            slot3_button.onClick.AddListener(() => activateCurrentAbility(3));
        }

        async void activateCurrentAbility(byte slot)
        {
            activateAbilityWindow.SetVisible(false);
            
            var request = EntityManager.CreateEntity();
            EntityManager.AddComponentData(request, new ActivateAbilityRequest
            {
                AbilityID = GetData<AbilityID>(currentAbilityToActivate),
                Slot = slot
            });
            EntityManager.AddComponentData(request, new Target(OwnerEntity));

            await Task.Delay(500);
            
            UpdateUI();
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

            private Entity abilityEntity;

            public Entity AbilityEntity
            {
                get => abilityEntity;
                set => abilityEntity = value;
            }
            public bool LevelMatch;
        }

        List<CharacteristicUpgradeInfo> upgrades = new();
        List<AbilityUpgradeInfo> abilityUpgrades = new();

        // void registerUpgrade(CharacteristicUpgradeUI ui, ICharacteristicUpgrade upgrade)
        // {
        //     if (upgrade == null)
        //     {
        //         return;
        //     }
        //     var info = new CharacteristicUpgradeInfo();
        //     ui.Counter.MinValue = 0;
        //     ui.Counter.MaxValue = MAXIMUM_CHARACTERISTIC_UPGRADE_LEVEL;
        //     info.UI = ui;
        //     info.Counter = ui.Counter;
        //     info.Upgrade = upgrade;
        //     upgrades.Add(info);
        // }

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

        public async void Confirm()
        {
            var requestEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(requestEntity, new Target(OwnerEntity));
            
            var requests = EntityManager.AddBuffer<LearnAbilityRequest>(requestEntity);

            foreach (var upgrade in abilityUpgrades)
            {
                var points = (byte)(upgrade.AbilityUI.Counter.CurrentValue - upgrade.AbilityUI.Counter.MinValue);

                if (points <= 0)
                {
                    continue;
                }
                
                requests.Add(new LearnAbilityRequest
                {
                    AbilityID = GetData<AbilityID>(upgrade.AbilityPrefab),
                    Points = points
                });
            }

            confirmButton.interactable = false;

            await Task.Delay(300);

            confirmButton.interactable = true;
            UpdateUI();

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

        void updateItemDescription(SkillUpgradeUI skillUI, Entity abilityPrefab, Entity abilityEntity, int currentLevel)
        {
            if (currentLevel == 0)
            {
                currentLevel = 1;
            }
            
            if (HasData<Description>(abilityPrefab))
            {
                skillUI.Description = GetSharedComponentManaged<Description>(abilityPrefab).ToString();    
            }
            else
            {
                skillUI.Description = "";
            }

            float baseDamageMultiplier = 1;
            float additionalDamageMultiplier = 1;

            if (HasData<CopyOwnerDamageToAbility>(abilityPrefab))
            {
                var copy = GetData<CopyOwnerDamageToAbility>(abilityPrefab);
                baseDamageMultiplier = copy.Multiplier;
            }
            
            if (HasData<ModifyDamageByLevelAbility>(abilityPrefab))
            {
                var modify = GetData<ModifyDamageByLevelAbility>(abilityPrefab);
                additionalDamageMultiplier = modify.Calculate((ushort)currentLevel);
            }

            var combinedMultiplier = baseDamageMultiplier * additionalDamageMultiplier;
            
            if (math.abs(combinedMultiplier - 1) > math.EPSILON)
            {
                if (string.IsNullOrEmpty(skillUI.Description) == false)
                {
                    skillUI.Description += Environment.NewLine;
                }

                string xStr = "<color=#888888>X</color>";
                string multStr = $" {xStr}<color=yellow>{combinedMultiplier:0.0}</color>";

                if (math.abs(additionalDamageMultiplier - 1) > math.EPSILON)
                {
                    multStr += $"  ( <color=yellow>{baseDamageMultiplier:0.0}</color> {xStr} <color=green>{additionalDamageMultiplier:0.0}</color> )";
                }

                var formatStr = damageMultiplierText.TryGetLocalizedString();
                var result = formatStr.Replace("{multiplier}", multStr);
                result = $"<color=#aaaaaa>{result}</color>";
                skillUI.Description += result;
            }
        }

        protected override void OnVisible()
        {
            base.OnVisible();

            if(isInitialized == false)
            {
                // registerUpgrade(damageUpgradePointsUI, null);//playerCharacter.DamageUpgrade);
                // registerUpgrade(defenceUpgradePointsUI, null);//playerCharacter.DefenceUpgrade);
                // registerUpgrade(speedUpgradePointsUI, null);//playerCharacter.SpeedUpgrade);
                // registerUpgrade(attackSpeedUpgradePointsUI, null);//playerCharacter.AttackSpeedUpgrade);
                // registerUpgrade(hpUpgradePointsUI, null);//playerCharacter.HitPointsUpgrade);
                // registerUpgrade(hpRegenUpgradePointsUI, null);//playerCharacter.HitPointsRegenUpgrade);
                // registerUpgrade(critChanceUpgradePointsUI, null);//playerCharacter.CritChanceUpgrade);
                // registerUpgrade(critMultiplierUpgradePointsUI, null);//playerCharacter.CritMultiplierUpgrade);
                // registerUpgrade(blockChanceUpgradePointsUI, null);//playerCharacter.BlockChanceUpgrade);
            
                isInitialized = true;    
            }
            
            activateAbilityWindow.SetVisible(false);
            
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
                var activeAbilities = GetBuffer<AbilityArray>();
                
                foreach (var abilityPrefab in abilityPrefabs)
                {
                    //var skill = playerCharacter.GetSkillInstanceAtIndex(i); 
                    var skillUI = skillUpgradeUiPool.Get();
                    if(skillUI.gameObject.activeSelf == false)
                    {
                        skillUI.gameObject.SetActive(true);
                    }
                    activeSkillUiList.Add(skillUI);
                    
                    var newInfo = new AbilityUpgradeInfo();
                    newInfo.AbilityUI = skillUI;
                    newInfo.AbilityPrefab = abilityPrefab;
                    
                    foreach (var ability in activeAbilities)
                    {
                        var id = GetData<AbilityID>(ability.AbilityEntity);
                        if (id == GetData<AbilityID>(newInfo.AbilityPrefab))
                        {
                            newInfo.AbilityEntity = ability.AbilityEntity;
                            break;
                        }
                    }
                    
                    var abilityMinimalLevel = GetData<MinimalLevel>(abilityPrefab).Value;
                    skillUI.RequiredLevel = string.Format(requiredLevelText.GetLocalizedString(), abilityMinimalLevel);
                    
                    // var skillUpgrade = playerCharacter.GetSkillUpgradeBySkillId(skill.Id);
                    // if(skillUpgrade == null)
                    // {
                    //     skillUpgrade = playerCharacter.CreateSkillUpgrade(skill.Id);
                    // }
            
                    skillUI.Counter.MinValue = 0;
                    skillUI.Counter.MaxValue = GetData<MaximumLevel>(abilityPrefab).Value;
                    
                    if (newInfo.AbilityEntity != Entity.Null)
                    {
                        newInfo.AbilityUI.Counter.CurrentValue = GetData<Level>(newInfo.AbilityEntity).Value;
                    }
                    else
                    {
                        newInfo.AbilityUI.Counter.CurrentValue = 0;
                    }
                    
                    skillUI.Icon = GetSharedComponentManaged<AbilityIcon>(abilityPrefab).Sprite;
                    skillUI.Label = GetSharedComponentManaged<ItemName>(abilityPrefab).ToString();
                    
                    //newInfo.Upgrade = skillUpgrade;
                    abilityUpgrades.Add(newInfo);
            
                    skillUI.transform.SetParent(skillContainer, false);
                    skillUI.transform.localScale = Vector3.one;
                    skillUI.transform.localPosition = Vector3.one;
                    skillUI.transform.localRotation = Quaternion.identity;
                    
                    skillUI.ActivateButton.onClick.RemoveAllListeners();
                    skillUI.ActivateButton.onClick.AddListener(() =>
                    {
                        skillUI.ActivateButton.interactable = false;
                        
                        currentAbilityToActivate = newInfo.AbilityEntity;
                        activateAbilityWindow.SetVisible(true);

                        var playerAbilities = GetData<PlayerAbilities>();
                        if (playerAbilities.Ability1.Ability != Entity.Null)
                        {
                            slot1_icon.sprite = GetSharedComponentManaged<AbilityIcon>(playerAbilities.Ability1.Ability).Sprite;
                        }
                        else
                        {
                            slot1_icon.sprite = emptySlotIcon;
                        }
                        if (playerAbilities.Ability2.Ability != Entity.Null)
                        {
                            slot2_icon.sprite = GetSharedComponentManaged<AbilityIcon>(playerAbilities.Ability2.Ability).Sprite;
                        }
                        else
                        {
                            slot2_icon.sprite = emptySlotIcon;
                        }
                        if (playerAbilities.Ability3.Ability != Entity.Null)
                        {
                            slot3_icon.sprite = GetSharedComponentManaged<AbilityIcon>(playerAbilities.Ability3.Ability).Sprite;
                        }
                        else
                        {
                            slot3_icon.sprite = emptySlotIcon;
                        }
                    });
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
            
            var activeAbilities = GetBuffer<AbilityArray>();

            foreach (var upgrade in abilityUpgrades)
            {
                if (upgrade.AbilityEntity == Entity.Null)
                {
                    foreach (var ability in activeAbilities)
                    {
                        var id = GetData<AbilityID>(ability.AbilityEntity);
                        if (id == GetData<AbilityID>(upgrade.AbilityPrefab))
                        {
                            upgrade.AbilityEntity = ability.AbilityEntity;
                            upgrade.AbilityUI.Counter.CurrentValue = GetData<Level>(upgrade.AbilityEntity).Value;
                            break;
                        }
                    }    
                }
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
                var currentCharacterLevel= GetData<Level>().Value;
                upgrade.LevelMatch = currentCharacterLevel >= abilityMinimalLevel;
                
                updateItemDescription(ui, upgrade.AbilityPrefab, upgrade.AbilityEntity, upgrade.AbilityUI.Counter.CurrentValue);

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
