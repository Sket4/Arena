// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Threading.Tasks;
using TzarGames.Common;
using TzarGames.Common.UI;
using TzarGames.GameCore.Client;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Arena.Client.UI.MainMenu
{
    public class CreateCharacterUI : TzarGames.Common.UI.UIBase
    {
        [SerializeField]
        TextUI currentClassLabel = default;

        [SerializeField]
        Color selectedClassColor = Color.blue;

        [SerializeField]
        Color unselectedClassColor = Color.white;

        [SerializeField] private TextUI statusText;
        [SerializeField] private GameObject statusButton;

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

        [SerializeField] private PhysicsCategoryTags selectableCharactersLayer;

        [SerializeField] private UnityEvent onStartWaitCreate;
        [SerializeField] private UnityEvent onStopWaitCreate;

        public event Action OnGoToNextScene;

        public bool AutoSelectCharacter { get; set; }
        public CharacterData LastCreatedCharacter { get; private set; }
        
        CharacterClass currentClass;
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

            input.CharacterLimit = GameState.Instance.MaxLengthOfCharacterName;
        }

        public void SetPlayerClass(CharacterClass classType)
        {
            classIsChosen = true;
            currentClass = classType;
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
            
            currentClassLabel.text = localizedClassName;
            currentClassLabel.gameObject.SetActive(false);
        }

        public void SetKnightClass()
        {
            SetPlayerClass(CharacterClass.Knight);
        }

        public void SetArcherClass()
        {
            SetPlayerClass(CharacterClass.Archer);
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
            if (classIsChosen == false)
            {
                return;
            }

            var startWaitTime = Time.realtimeSinceStartup;
            statusText.text = pleaseWaitText;
            statusButton.SetActive(false);
            onStartWaitCreate.Invoke();
            var result = await GameState.Instance.CreateCharacter(input.Text, currentClass);

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
            GetUtilitySystem((utilSystem) =>
            {
                utilSystem.SendMessage(MainUI.EnableCharactersMessage); 
            });
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            GetUtilitySystem((utilSystem) =>
            {
                utilSystem.SendMessage(MainUI.DisableCharactersMessage);
            });
        }

        void GetUtilitySystem(Action<UtilitySystem> callback)
        {
            var mainUI = FindObjectOfType<MainUI>();
            StartCoroutine(mainUI.WaitForSceneGameLoop((gameLoop) =>
            {
                callback?.Invoke(gameLoop.World.GetExistingSystemManaged<UtilitySystem>());
            }));
        }
        
        public void OnPointerDown(BaseEventData eventData)
        {
            GetUtilitySystem((utilSystem) =>
            {
                var camera = Camera.main;
                var ray = camera.ScreenPointToRay((eventData as PointerEventData).position);
            
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 15);
            
                if (utilSystem.Raycast(ray.origin, ray.origin + ray.direction * 10, new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = selectableCharactersLayer.Value,
                        GroupIndex = 0
                    }, out RaycastHit hit))
                {
                    if (utilSystem.EntityManager.HasComponent<CharacterClassData>(hit.Entity) == false)
                    {
                        return;
                    }

                    var charClass = utilSystem.EntityManager.GetComponentData<CharacterClassData>(hit.Entity);
                
                    Debug.Log($"Selected {hit.Entity.Index} {charClass.Value}");

                    switch (charClass.Value)
                    {
                        case CharacterClass.Knight:
                            SetKnightClass();
                            utilSystem.SendMessage("enable_knight");
                            utilSystem.SendMessage("disable_archer");
                            break;
                        case CharacterClass.Archer:
                            SetArcherClass();
                            utilSystem.SendMessage("enable_archer");
                            utilSystem.SendMessage("disable_knight");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }
    }
}
