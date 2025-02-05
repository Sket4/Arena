// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Threading.Tasks;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions.ColorPicker;
using Random = UnityEngine.Random;

namespace Arena.Client.UI.MainMenu
{
    public class CreateCharacterUI : TzarGames.Common.UI.UIBase
    {
        [SerializeField]
        Color selectedClassColor = Color.blue;

        [SerializeField]
        Color unselectedClassColor = Color.white;

        [SerializeField] private TextUI statusText;
        [SerializeField] private GameObject statusButton;

        [SerializeField] private GameObject mainSettingsContainer;
        [SerializeField] private SwitcherUI genderSwitcher;
        [SerializeField] private UnityEngine.Localization.LocalizedString maleGenderText;
        [SerializeField] private UnityEngine.Localization.LocalizedString femaleGenderText;
        [SerializeField] private SwitcherUI skinColorSwitcher;
        [SerializeField] private SwitcherUI headSwitcher;
        [SerializeField] private SwitcherUI hairColorSwitcher;
        [SerializeField] private SwitcherUI hairstyleSwitcher;
        [SerializeField] private SwitcherUI eyeColorSwitcher;
        [SerializeField] private ColorPickerControl colorPicker;

        [SerializeField]
        InputFieldUI input = default;

        [SerializeField]
        UnityEngine.UI.Button createButton = default;

        [SerializeField]
        UnityEngine.UI.Button cancelButton = default;

        [SerializeField]
        UIBase nextMenu = null;

        [SerializeField] private LocalizedStringAsset pleaseWaitText;
        [SerializeField] private LocalizedStringAsset unknownErrorText;
        [SerializeField] private LocalizedStringAsset alreadyTakenNameText;
        [SerializeField] private LocalizedStringAsset knightClassName;
        [SerializeField] private LocalizedStringAsset archerClassName;

        [SerializeField] private LayerMask selectableCharactersLayer;

        [SerializeField] private Color[] armorColorVariants;

        [SerializeField] private UnityEvent onStartWaitCreate;
        [SerializeField] private UnityEvent onStopWaitCreate;

        enum ColorPickerMode
        {
            Armor
        }

        private ColorPickerMode currentColorPickerMode = ColorPickerMode.Armor;

        public event Action OnGoToNextScene;

        public bool AutoSelectCharacter { get; set; }
        public CharacterData LastCreatedCharacter { get; private set; }
        
        CharacterClass selectedClass;
        private Genders selectedGender = Genders.Male;
        private int selectedHead = 0;
        private int selectedSkinColor = 0;
        private int selectedHairColor = 0;
        private int selectedHairstyle = 0;
        private Color selectedArmorColor = Color.white;
        private int selectedEyeColor = 0;
        bool classIsChosen = false;

        protected override void Start()
        {
            base.Start();
            AutoSelectCharacter = true;

            createButton.interactable = classIsChosen;

            if(GameState.Instance == null)
            {
                return;
            }
            updateCreateButtonState();
            
            SetPlayerClass(CharacterClass.Knight);
            input.CharacterLimit = GameState.Instance.MaxLengthOfCharacterName;

            input.OnValueChanged += _ =>
            {
                updateCreateButtonState();
            };

            // gender switcher
            selectedGender = Genders.Male;
            updateGenderSwitcher(false);
            
            genderSwitcher.OnNext.AddListener(() =>
            {
                updateGenderSwitcher(true);
                updateCharacterInstance();
            });
            genderSwitcher.OnPrev.AddListener(() =>
            {
                updateGenderSwitcher(true);
                updateCharacterInstance();
            });
            
            // skin color switcher
            selectedSkinColor = 0;
            updateSkinColorSwitcher();
            
            skinColorSwitcher.OnPrev.AddListener(() =>
            {
                selectedSkinColor--;
                updateSkinColorSwitcher();
                updateCharacterInstance();
            });
            
            skinColorSwitcher.OnNext.AddListener(() =>
            {
                selectedSkinColor++;
                updateSkinColorSwitcher();
                updateCharacterInstance();
            });
            
            // hair color
            selectedHairColor = 0;
            updateHairColorSwitcher();
            
            hairColorSwitcher.OnPrev.AddListener(() =>
            {
                selectedHairColor--;
                updateHairColorSwitcher();
                updateCharacterInstance();
            });
            
            hairColorSwitcher.OnNext.AddListener(() =>
            {
                selectedHairColor++;
                updateHairColorSwitcher();
                updateCharacterInstance();
            });
            
            // eye color
            selectedEyeColor = 0;
            updateEyeColorSwitcher();
            
            eyeColorSwitcher.OnPrev.AddListener(() =>
            {
                selectedEyeColor--;
                updateEyeColorSwitcher();
                updateCharacterInstance();
            });
            
            eyeColorSwitcher.OnNext.AddListener(() =>
            {
                selectedEyeColor++;
                updateEyeColorSwitcher();
                updateCharacterInstance();
            });
            
            // head
            selectedHead = 0;
            updateHeadSwitcher();
            
            headSwitcher.OnPrev.AddListener(() =>
            {
                selectedHead--;
                updateHeadSwitcher();
                updateCharacterInstance();
            });
            
            headSwitcher.OnNext.AddListener(() =>
            {
                selectedHead++;
                updateHeadSwitcher();
                updateCharacterInstance();
            });
            
            // hairstyle
            selectedHairstyle = 0;
            updateHairstyleSwitcher();
            
            hairstyleSwitcher.OnPrev.AddListener(() =>
            {
                selectedHairstyle--;
                updateHairstyleSwitcher();
                updateCharacterInstance();
            });
            
            hairstyleSwitcher.OnNext.AddListener(() =>
            {
                selectedHairstyle++;
                updateHairstyleSwitcher();
                updateCharacterInstance();
            });

            if (armorColorVariants.Length > 0)
            {
                selectedArmorColor = armorColorVariants[Random.Range(0, armorColorVariants.Length)];
            }

            colorPicker.S = 1;
            colorPicker.V = 1;
            
            colorPicker.onValueChanged.AddListener((color) =>
            {
                storePickedColor(color);
                updateCharacterInstance();
            });
        }

        void storePickedColor(Color color)
        {
            switch (currentColorPickerMode)
            {
                case ColorPickerMode.Armor:
                    selectedArmorColor = color;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void updateCharacterInstance()
        {
            GetUtilitySystem((utilSystem) =>
            {
                var characterInstance = Entity.Null;
                var needCreateInstance = false;
                var em = utilSystem.EntityManager;
                
                if (utilSystem.HasSingleton<PlayerController>())
                {
                    characterInstance = utilSystem.GetSingletonEntity<PlayerController>();

                    if (em.GetComponentData<Gender>(characterInstance).Value != selectedGender
                        || em.GetComponentData<CharacterClassData>(characterInstance).Value != selectedClass)
                    {
                        needCreateInstance = true;
                    }
                }
                else
                {
                    needCreateInstance = true;
                }

                if (needCreateInstance)
                {
                    if (characterInstance != Entity.Null)
                    {
                        em.DestroyEntity(characterInstance);
                    }
                    var characterData = createCharacterData();
                    characterInstance = MainUI.CreateCharacter(utilSystem, characterData);
                }
                
                int[] headIds, hairstyleIds;

                switch (selectedGender)
                {
                    case Genders.Male:
                        headIds = Identifiers.MaleHeadIDs;
                        hairstyleIds = Identifiers.MaleHairStyles;
                        break;
                    case Genders.Female:
                        headIds = Identifiers.FemaleHeadIDs;
                        hairstyleIds = Identifiers.FemaleHairStyles;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                em.SetComponentData(characterInstance, new CharacterHead { ModelID = new PrefabID(headIds[selectedHead])});
                em.SetComponentData(characterInstance, new CharacterHairstyle { ID = new PrefabID(hairstyleIds[selectedHairstyle]) });
                em.SetComponentData(characterInstance, new CharacterSkinColor { Value = Identifiers.SkinColors[selectedSkinColor] });
                em.SetComponentData(characterInstance, new CharacterHairColor { Value = Identifiers.HairColors[selectedHairColor] });
                em.SetComponentData(characterInstance, new CharacterEyeColor { Value = Identifiers.EyeColors[selectedEyeColor] });

                var characterEquipment = em.GetComponentData<CharacterEquipment>(characterInstance);
                if (characterEquipment.ArmorSet != Entity.Null)
                {
                    var armorColor = (Color32)selectedArmorColor;
                    em.SetComponentData(characterEquipment.ArmorSet, new SyncedColor { Value = new PackedColor(armorColor.r, armorColor.g, armorColor.b, armorColor.a)});
                }
            });
        }

        CharacterData createCharacterData()
        {
            var headIds = selectedGender == Genders.Female
                ? Identifiers.FemaleHeadIDs
                : Identifiers.MaleHeadIDs;

            var hairstyles = selectedGender == Genders.Female
                ? Identifiers.FemaleHairStyles
                : Identifiers.MaleHairStyles;

            var armorColor = (Color32)selectedArmorColor;
            
            var characterData = SharedUtility.CreateDefaultCharacterData(
                selectedClass,
                input.Text,
                selectedGender,
                headIds[selectedHead],
                hairstyles[selectedHairstyle],
                Identifiers.SkinColors[selectedSkinColor].rgba,
                Identifiers.HairColors[selectedHairColor].rgba,
                Identifiers.EyeColors[selectedEyeColor].rgba,
                new PackedColor(armorColor.r, armorColor.g, armorColor.b, armorColor.a).rgba
            );

            return characterData;
        }
        
        void updateHeadSwitcher()
        {
            var headIds = selectedGender == Genders.Female ? Identifiers.FemaleHeadIDs : Identifiers.MaleHeadIDs;
            
            if (selectedHead >= headIds.Length)
            {
                selectedHead = 0;
            }

            if (selectedHead < 0)
            {
                selectedHead = headIds.Length - 1;
            }
            headSwitcher.Text = numberSwitcherText(selectedHead);
        }
        void updateHairstyleSwitcher()
        {
            var ids = selectedGender == Genders.Female ? Identifiers.FemaleHairStyles : Identifiers.MaleHairStyles;
            
            if (selectedHairstyle >= ids.Length)
            {
                selectedHairstyle = 0;
            }

            if (selectedHairstyle < 0)
            {
                selectedHairstyle = ids.Length - 1;
            }
            hairstyleSwitcher.Text = numberSwitcherText(selectedHairstyle);
        }
        
        void updateEyeColorSwitcher()
        {
            if (selectedEyeColor >= Identifiers.EyeColors.Length)
            {
                selectedEyeColor = 0;
            }

            if (selectedEyeColor < 0)
            {
                selectedEyeColor = Identifiers.EyeColors.Length - 1;
            }

            var color = Identifiers.EyeColors[selectedEyeColor];
            
            eyeColorSwitcher.Text = numberSwitcherText(selectedEyeColor);
            eyeColorSwitcher.Color = (Color)new Color32(color.r, color.g, color.b, color.a);
        }

        void updateHairColorSwitcher()
        {
            if (selectedHairColor >= Identifiers.HairColors.Length)
            {
                selectedHairColor = 0;
            }

            if (selectedHairColor < 0)
            {
                selectedHairColor = Identifiers.HairColors.Length - 1;
            }

            var color = Identifiers.HairColors[selectedHairColor];
            
            hairColorSwitcher.Text = numberSwitcherText(selectedHairColor);
            hairColorSwitcher.Color = (Color)new Color32(color.r, color.g, color.b, color.a);
        }

        void updateSkinColorSwitcher()
        {
            if (selectedSkinColor >= Identifiers.SkinColors.Length)
            {
                selectedSkinColor = 0;
            }

            if (selectedSkinColor < 0)
            {
                selectedSkinColor = Identifiers.SkinColors.Length - 1;
            }

            var color = Identifiers.SkinColors[selectedSkinColor];
            
            skinColorSwitcher.Text = numberSwitcherText(selectedSkinColor);
            skinColorSwitcher.Color = (Color)new Color32(color.r, color.g, color.b, color.a);
        }

        string numberSwitcherText(int number)
        {
            return $"№ {number + 1}";
        }

        void updateGenderSwitcher(bool switchGender)
        {
            if (switchGender)
            {
                if (selectedGender == Genders.Female)
                    selectedGender = Genders.Male;
                else
                    selectedGender = Genders.Female;    
            }
            
            string text;

            switch (selectedGender)
            {
                case Genders.Female:
                    text = femaleGenderText.GetLocalizedString();
                    break;
                case Genders.Male:
                    text = maleGenderText.GetLocalizedString();
                    break;
                default:
                    text = "???";
                    break;
            }
            genderSwitcher.Text = text;
            
            updateHeadSwitcher();
            updateHairstyleSwitcher();
        }

        public void SetPlayerClass(CharacterClass classType)
        {
            classIsChosen = true;
            selectedClass = classType;
            createButton.interactable = true;

            string localizedClassName = classType.ToString();

            switch (classType)
            {
                case CharacterClass.Knight:
                    localizedClassName = knightClassName;
                    break;
                case CharacterClass.Archer:
                    localizedClassName = archerClassName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(classType), classType, null);
            }
        }

        public void SetNextMenu(TzarGames.Common.UI.UIBase menu)
        {
            nextMenu = menu;
        }

        public void SetCancelState(bool enable)
        {
            cancelButton.gameObject.SetActive(enable);
        }

        public async void Create()
        {
            try
            {
                await createProcess();
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        

        async Task createProcess()
        {
            if (canCreateCharacter() == false)
            {
                return;
            }

            var startWaitTime = Time.realtimeSinceStartup;
            statusText.text = pleaseWaitText;
            statusButton.SetActive(false);
            onStartWaitCreate.Invoke();

            if (selectedHead < 0)
            {
                Debug.LogError("invalid head ID");
                
                selectedHead = selectedGender == Genders.Female
                    ? Identifiers.DefaultFemaleHeadID
                    : Identifiers.DefaultMaleHeadID;
            }

            if (selectedHairstyle < 0)
            {
                Debug.LogError("invalid hairstyle id");
                selectedHairstyle = 0;
            }

            if (selectedSkinColor < 0)
                selectedSkinColor = 0;
            if (selectedEyeColor < 0)
                selectedEyeColor = 0;
            if (selectedHairColor < 0)
                selectedHairColor = 0;

            int[] headIds, hairstyleIds;
            
            switch (selectedGender)
            {
                case Genders.Male:
                    headIds = Identifiers.MaleHeadIDs;
                    hairstyleIds = Identifiers.MaleHairStyles;
                    break;
                case Genders.Female:
                    headIds = Identifiers.FemaleHeadIDs;
                    hairstyleIds = Identifiers.FemaleHairStyles;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var armorColor = (Color32)selectedArmorColor;
            var packedArmorColor = new PackedColor(armorColor.r, armorColor.g, armorColor.b, armorColor.a).rgba;
            
            var result = await GameState.Instance.CreateCharacter(
                input.Text, 
                selectedClass, 
                selectedGender, 
                headIds[selectedHead], 
                hairstyleIds[selectedHairstyle], 
                selectedHairColor, 
                selectedSkinColor, 
                selectedEyeColor, 
                packedArmorColor
                );

            var elapsedTime = Time.realtimeSinceStartup - startWaitTime;

            if (elapsedTime < 1.0f)
            {
                await Task.Delay(Mathf.RoundToInt((1.0f - elapsedTime) * 1000));    
            }

            if (result.Success == false)
            {
                statusButton.SetActive(true);
                switch (result.ErrorMessage)
                {
                    case "AlreadyCreated":
                        TextUI.ReplaceLocalizedText(statusText, alreadyTakenNameText);
                        break;
                    default:
                        TextUI.ReplaceLocalizedText(statusText, unknownErrorText);
                        break;
                }
                Debug.Log($"failed to create character: {result.ErrorMessage}");
                return;
            }
            
            onStopWaitCreate.Invoke();
            
            LastCreatedCharacter = result.Character;
            
            if (LastCreatedCharacter == null)
            {
                return;
            }

            if (AutoSelectCharacter)
            {
                await GameState.Instance.SelectCharacter(LastCreatedCharacter.Name);
            }

            GoToNextMenu();
        }

        public void GoToNextMenu()
        {
            if(nextMenu != null)
            {
                nextMenu.SetVisible(true);
            }
            
            SetVisible(false);

            if(OnGoToNextScene != null)
            {
                OnGoToNextScene();
            }
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            LastCreatedCharacter = null;
            
            mainSettingsContainer.SetActive(true);
            colorPicker.gameObject.SetActive(false);
            
            updateGenderSwitcher(false);
            updateHairstyleSwitcher();
            updateHeadSwitcher();
            updateEyeColorSwitcher();
            updateSkinColorSwitcher();
            updateHairColorSwitcher();
            
            updateCharacterInstance();
        }

        void GetUtilitySystem(Action<UtilitySystem> callback)
        {
            var mainUI = FindObjectOfType<MainUI>();
            StartCoroutine(mainUI.WaitForSceneGameLoop((gameLoop) =>
            {
                callback?.Invoke(gameLoop.World.GetExistingSystemManaged<UtilitySystem>());
            }));
        }
        
        bool canCreateCharacter()
        {
            if (classIsChosen == false)
            {
                return false;
            }

            if (string.IsNullOrEmpty(input.Text) || input.Text.Length < 3)
            {
                return false;
            }
            return true;
        }

        void updateCreateButtonState()
        {
            createButton.interactable = canCreateCharacter();
        }

        public void OnPickArmorColorPressed()
        {
            mainSettingsContainer.SetActive(false);
            colorPicker.gameObject.SetActive(true);

            currentColorPickerMode = ColorPickerMode.Armor;
        }

        public void OnColorPicked()
        {
            switch (currentColorPickerMode)
            {
                case ColorPickerMode.Armor:
                    selectedArmorColor = colorPicker.CurrentColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            mainSettingsContainer.SetActive(true);
            colorPicker.gameObject.SetActive(false);
        }

        public void OnCancelColorPicked()
        {
            mainSettingsContainer.SetActive(true);
            colorPicker.gameObject.SetActive(false);
        }
        
        public void OnPointerDown(BaseEventData eventData)
        {
            // GetUtilitySystem((utilSystem) =>
            // {
            //     var camera = Camera.main;
            //     var ray = camera.ScreenPointToRay((eventData as PointerEventData).position);
            //
            //     Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 15);
            //
            //     if (utilSystem.Raycast(ray.origin, ray.origin + ray.direction * 10, new CollisionFilter
            //         {
            //             BelongsTo = ~0u,
            //             CollidesWith = Utility.LayerMaskToCollidesWithMask(selectableCharactersLayer),
            //             GroupIndex = 0
            //         }, out RaycastHit hit))
            //     {
            //         if (utilSystem.EntityManager.HasComponent<CharacterClassData>(hit.Entity) == false)
            //         {
            //             return;
            //         }
            //
            //         var charClass = utilSystem.EntityManager.GetComponentData<CharacterClassData>(hit.Entity);
            //     
            //         Debug.Log($"Selected {hit.Entity.Index} {charClass.Value}");
            //
            //         switch (charClass.Value)
            //         {
            //             case CharacterClass.Knight:
            //                 SetKnightClass();
            //                 utilSystem.SendMessage("enable_knight");
            //                 utilSystem.SendMessage("disable_archer");
            //                 break;
            //             case CharacterClass.Archer:
            //                 SetArcherClass();
            //                 utilSystem.SendMessage("enable_archer");
            //                 utilSystem.SendMessage("disable_knight");
            //                 break;
            //             default:
            //                 throw new ArgumentOutOfRangeException();
            //         }
            //     }
            // });
        }
    }
}
