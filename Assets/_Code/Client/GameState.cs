// Copyright 2012-2024 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using Google.Protobuf;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Arena.Quests;
using Grpc.Core;
using TzarGames.GameCore;
using TzarGames.GameFramework;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Client;
using UniRx;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using static TzarGames.MatchFramework.Client.ClientUtility;

namespace Arena.Client
{
    public class QuestGameInfo
    {
        public string MatchType;
        public PrefabID GameSceneID;
        public bool Multiplayer;
        public int SpawnPointID;

        public GameParameter[] Parameters;
    }
    
    public interface IGameInterface
    {
        public Task<bool> StartLocation(QuestGameInfo questGameInfo);
        public void ExitFromMatch();
    }

    public sealed class GameInterface : IGameInterface, IComponentData
    {
        private IGameInterface iface;
        
        public GameInterface(IGameInterface iface)
        {
            this.iface = iface;
        }

        public GameInterface()
        {
        }

        public Task<bool> StartLocation(QuestGameInfo questGameInfo)
        {
            return iface.StartLocation(questGameInfo);
        }

        public void ExitFromMatch()
        {
            iface.ExitFromMatch();
        }
    }
    
    public class GameState : StateMachine, IAuthTokenProvider, IGameInterface
    {
        [SerializeField]
        float minLoadingTime = 3;

        [SerializeField]
        string tutorialSceneName = default;

        [SerializeField]
        string mainMenuSceneName = default;

        [SerializeField]
        string clientGameSceneName = default;

        [SerializeField]
        string localGameSceneName = default;

        [SerializeField]
        GameObject connectingUiPrefab = default;
        
        [SerializeField] ServerAddress authServer = new ServerAddress("127.0.0.1", 30600);
        [SerializeField] ServerAddress frontendServer = new ServerAddress("127.0.0.1", 31600);
        
        [SerializeField] ServerAddress localAuthServer = new ServerAddress("127.0.0.1", 30600);
        [SerializeField] ServerAddress localFrontendServer = new ServerAddress("127.0.0.1", 31600);
        public string AuthServerCertificate => UseLocalServer ? localAuthServer.Certificate.text : authServer.Certificate.text;


        [SerializeField]
        float refreshTokenMinutesInterval = 3;

        GameObject connectingUi;


        public ServerAddress FrontendServer => UseLocalServer ? localFrontendServer : frontendServer;
        public ServerAddress AuthServer => UseLocalServer ? localAuthServer : authServer;


        public string AuthenticationToken
        {
            get
            {
                return Connecting.AuthToken;
            }
        }

        public static bool IsOfflineMode { get; set; } = true;
        public static bool UseLocalServer { get; set; }

        public GameData PlayerData
        {
            get; private set;
        }

        public CharacterData SelectedCharacter
        {
            get
            {
                if (PlayerData == null || PlayerData.Characters.Count == 0)
                {
                    return null;
                }

                foreach (var character in PlayerData.Characters)
                {
                    if (string.IsNullOrEmpty(character.Name))
                    {
                        continue;
                    }

                    if (character.Name.Equals(PlayerData.SelectedCharacterName))
                    {
                        return character;
                    }
                }
                return null;
            }
        }

	    [SerializeField, HideInInspector] private string bundleVersion;

		static GameState _instance;

	    public bool IsLoadingScene { get; private set; }
        public bool IsConnectingToGameServer { get; private set; }
	    public event Action OnLoadingStarted;

        // вызывается при окончании загрузки следующей сцены, до ее активации
        public event Action OnLoadingSceneReady;
        public event Action OnLoadingFinished;
		public event Action OnInitializationFinished;
		public event Action OnGameContinued;
		public event Action OnMainMenuLoaded;
        
		private object lastAttemptGameStateData = null;
		private Type lastAttemptGameStateType = null;

        private ServiceWrapper<ArenaClientService.ArenaClientServiceClient> gameService;

        private bool paused = false;
		public bool Paused
		{
			get
			{
				return paused;
			}
			set
			{
#if UNITY_EDITOR
				if (paused != value)
				{
					if (value)
					{
						Debug.Log("Game paused");
					}
					else
					{
						Debug.Log("Game resumed");
					}	
				}
#endif
				paused = value;
			}
		}

	    public static GameState Instance
        {
            get
            {
                return _instance;
            }
        }

        public static string MultiplayerVersion
        {
            get
            {
                return "0";
            }
        }

        public string Version
	    {
	        get
	        {
                var appVersion = Application.version;
#if UNITY_ANDROID || UNITY_IOS
                appVersion += string.Format(" ({0})", bundleVersion);
#endif
                return appVersion;
            }
	    }
	    
#if UNITY_EDITOR
	    [UnityEditor.Callbacks.PostProcessScene]
	    static void OnPostprocessScene()
	    {
		    if (UnityEditor.EditorApplication.isPlaying)
		    {
			    return;
		    }
		    
		    var gameState = FindObjectOfType<GameState>();
		    if (gameState != null)
		    {
			    gameState.bundleVersion = Editor_GetBundleVersion();
			    Debug.Log("Запись bundle version для билда: " + gameState.bundleVersion);    
		    }
	    }

		public static string Editor_GetBundleVersion()
		{
			#if UNITY_ANDROID
			return UnityEditor.PlayerSettings.Android.bundleVersionCode.ToString();
			#elif UNITY_IOS
			return UnityEditor.PlayerSettings.iOS.buildNumber;
			#else
			return "";
			#endif
		}

#endif
        
        public async Task SelectCharacter(string characterName)
        {
            Debug.Log($"Запрос на выбор персонажа... {characterName}");

            if(IsOfflineMode)
            {
                for (var index = 0; index < PlayerData.Characters.Count; index++)
                {
                    var character = PlayerData.Characters[index];
                    if (character.Name.Equals(characterName))
                    {
                        PlayerData.SelectedCharacterName = characterName;
                        SaveLocalGame();
                        break;
                    }
                }
            }
            else
            {
                var selectRequest = new SelectCharacterRequest()
                {
                    Name = characterName
                };

                var selectResult = await gameService.Service.SelectCharacterAsync(selectRequest);

                if(selectResult.Success)
                {
                    for (var index = 0; index < PlayerData.Characters.Count; index++)
                    {
                        var character = PlayerData.Characters[index];
                        if (character.Name.Equals(characterName))
                        {
                            PlayerData.SelectedCharacterName = characterName;
                            break;
                        }
                    }
                }

                Debug.Log($"Выбор персонажа {characterName}, результат: {selectResult.Success}");
            }
        }

        

        async Task LoadPlayerGameData()
        {
            Debug.Log("Loading player game data...");
            var playerDataResult = await gameService.Service.GetGameDataAsync(new GetGameDataRequest()).ResponseAsync;
            if (playerDataResult.Data != null)
            {
                PlayerData = playerDataResult.Data;
                SaveLocalGame();
            }
            else
            {
                Debug.LogError("Failed to load player game data");
            }
        }

        public int MaxLengthOfCharacterName
		{
			get { return 12; }
		}

        public async Task<CreateCharacterResult> CreateCharacter(string name, CharacterClass classType, Genders gender, int headID, int hairstyleID, int hairColorID, int skinColorID, int eyeColorID, int armorColor)
        {
            if (IsOfflineMode)
            {
                var hairColor = Identifiers.HairColors[hairColorID].rgba;
                var skinColor = Identifiers.SkinColors[skinColorID].rgba;
                var eyeColor = Identifiers.EyeColors[eyeColorID].rgba;
                
                var localCharacter = Arena.SharedUtility.CreateDefaultCharacterData(classType, name, gender, headID, hairstyleID, skinColor, hairColor, eyeColor, armorColor);
                PlayerData.Characters.Add(localCharacter);
                SaveLocalGame();
                return new CreateCharacterResult { Character = localCharacter, Success = true };
            }
            
            var result = await gameService.Service.CreateCharacterAsync(new CreateCharacterRequest
            {
                Class = (int)classType,
                Name = name,
                SkinColor = skinColorID,
                HairColor = hairColorID,
                EyeColor = eyeColorID,
                Gender = gender,
                HairstyleID = hairstyleID,
                HeadID = headID,
                
            }).ResponseAsync;
            
            if(result.Character != null)
            {
                Debug.LogFormat("Created character {0} xp {1}", result.Character.Class, result.Character.XP);
                PlayerData.Characters.Add(result.Character);
            }
            
            return result;
        }

        public async Task<bool> DeleteCharacter(string name)
        {
            foreach (var character in PlayerData.Characters)
            {
                if (character.Name == name)
                {
                    if (IsOfflineMode)
                    {
                        PlayerData.Characters.Remove(character);
                        SaveLocalGame();
                        return true;
                    }
                    else
                    {
                        var result = await gameService.Service.DeleteCharacterAsync(new DeleteCharacterRequest
                        {
                            Name = name
                        }).ResponseAsync;
                        
                        if (result.Success)
                        {
                            PlayerData.Characters.Remove(character);
                        }
                        return result.Success;
                    }
                }
            }
            return false;
        }

        public void SaveLocalGame()
        {
            Observable.Start(() => {}).ObserveOnMainThread().Subscribe((xs) =>
            {
                if(PlayerData == null)
                {
                    return;
                }
                var bytes = PlayerData.ToByteArray();
                
                ArenaPlayerDataLocalStoreSystem.Save(bytes, ArenaPlayerDataLocalStoreSystem.GetDefaultSavePath());    
            });
        }

		public void DeleteAllCharacters()
		{
            throw new System.NotImplementedException();
   //         for (int i = 0; i < currentSaveGame.CharacterCount; i++) 
			//{
			//	currentSaveGame.RemoveCharacterAt (i);
			//}
            //currentSaveGame.SelectedCharacterNumber = -1;
		}

        public bool IsTutorialCompleted
        {
            get
            {
                throw new System.NotImplementedException();
                //if (currentSaveGame.SelectedCharacter == null)
                //{
                //    return false;
                //}
                //return currentSaveGame.SelectedCharacter.IsTutorialCompleted;
            }
        }

        public bool IsInGameState()
        {
            return CurrentState is Game;
        }

        public bool IsInOnlineGameState()
        {
            return CurrentState is OnlineGame;
        }

        public bool IsInOfflineGameState()
        {
            return CurrentState is OfflineGame;
        }
		
		public bool IsInitializing()
		{
			return CurrentState is Initial;
		}

		public bool IsInMainMenu()
		{
			return CurrentState is MainMenu;
		}

        public bool IsInTutorial()
        {
            return CurrentState is Tutorial;
        }
        
        public static OnlineGameInfo GetOnlineGameInfo() => onlineGameInfo;
        private static OfflineGameInfo offlineGameInfo;
        public static OfflineGameInfo GetOfflineGameInfo() => offlineGameInfo; 
        public static OnlineGameInfo SetDebugOnlineGameInfo(OnlineGameInfo onlineGameInfo) => GameState.onlineGameInfo = onlineGameInfo;
        static OnlineGameInfo onlineGameInfo = null;

        public class OnlineGameInfo
        {
            public string MatchType { get; private set; }
            public MetaData MetaData { get; private set; }
            public string ServerHost { get; private set; }
            public int ServerPort { get; private set; }
            
            public SymmetricEncryptionKey EncryptionKey
            {
                get; private set;
            }

            public OnlineGameInfo(string matchType, string serverHost, int serverPort, SymmetricEncryptionKey encryptionKey, MetaData metaData)
            {
                MatchType = matchType;
                MetaData = metaData;
                ServerHost = serverHost;
                ServerPort = serverPort;
                EncryptionKey = encryptionKey;
            }
        }
        
        public class OfflineGameInfo
        {
            public string MatchType { get; private set; }
            public int GameSceneID { get; private set; }
            public int SpawnPointID { get; private set; }
            public GameParameter[] Parameters { get; private set; }

            public OfflineGameInfo(string matchType, int gameSceneId, int spawnPointID, GameParameter[] parameters)
            {
                MatchType = matchType;
                GameSceneID = gameSceneId;
                SpawnPointID = spawnPointID;
                Parameters = parameters;
            }
        }

        public void SetTutorialCompleted()
        {
            throw new System.NotImplementedException();
            //currentSaveGame.SelectedCharacter.IsTutorialCompleted = true;
        }
		public bool IsItSafeStateToSaveGame()
		{
			return IsInMainMenu();
		}

        class TransitionState : State
        {

        }

        public void ExitFromGame()
        {
            if(CurrentState is Game)
            {
                GotoState<MainMenu>();
            }
        }

        // повторная загрузка данных игрока и последующая загрузка главного меню
//#if UNITY_EDITOR
//        [TzarGames.Common.ConsoleCommand]
//#endif
        public void ReloadGameDataAndLoadMainMenu()
		{
			GotoState<Reloading>();
		}

        public void Reconnect()
        {
            if(CurrentState is FailedConnection == false)
            {
                Debug.LogError($"Нельзя переподключаться из состояния {CurrentState.GetType().Name}");
                return;
            }
            GotoState<Connecting>();
        }

        public void ExitToMainMenu()
        {
            GotoState<MainMenu>();
        }
		
        protected override void Awake()
		{
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Debug.LogError("Не допускается использование больше одного " + GetType().Name);
                Destroy(gameObject);
                return;
            }

            base.Awake();

			DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += (scene, mode) => 
            {
                (CurrentState as GameStateBase).OnSceneLoaded(scene, mode);
            };
			
			#if UNITY_EDITOR
			bundleVersion = Editor_GetBundleVersion();
			#endif
		}

        void OnDestroy()
		{
			if(_instance == this)
			{
				_instance = null;
                offlineGameInfo = null;
                onlineGameInfo = null;
                Debug.Log("Shutting down client game service");

                if (gameService != null)
                {
                    gameService.ShutdownAsync();    
                }
            }
            (CurrentState as GameStateBase).OnDestroy();
		}

        public void LoadSceneAsync(string sceneName)
        {
            (CurrentState as GameStateBase).LoadSceneAsync(sceneName);
        }

        public class GameStateBase : State
        {
            public GameState GameState
            {
                get
                {
                    return Owner as GameState;
                }
            }

            public virtual void LoadSceneAsync(string sceneName, bool unloadUnusedAssets = true, Action loadedCallback = null)
            {
                GameState.StartCoroutine(loadSceneRoutine(sceneName, unloadUnusedAssets, loadedCallback));
            }

            IEnumerator loadSceneRoutine(string sceneName, bool unloadUnusedAssets, Action loadedCallback)
            {
                var asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                if(GameState.IsLoadingScene == false)
                {
                    GameState.IsLoadingScene = true;

                    try
                    {
                        Debug.Log("Loading started");
                        GameState.OnLoadingStarted?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                
                asyncOperation.allowSceneActivation = false;

                float loadingStartTime = Time.realtimeSinceStartup;
                
                while (asyncOperation.progress < 0.9f)
                {
                    yield return null;
                }
                
                while (Time.realtimeSinceStartup - loadingStartTime < GameState.minLoadingTime)
                {
                    yield return null;
                }

                try
                {
                    Debug.Log("Cleaning up poolable objects");
                    Instantiator.CleanupPoolableObjects();

                    GameState.OnLoadingSceneReady?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                asyncOperation.allowSceneActivation = true;

                yield return asyncOperation;
                if (unloadUnusedAssets)
                {
                    Resources.UnloadUnusedAssets();
                }
                GC.Collect();

                var sceneLauncher = FindObjectOfType<BaseClientGameLauncher>();
                if (sceneLauncher != null)
                {
                    Debug.Log("Waiting for launcher scene loading...");
                    while (sceneLauncher.IsLoadingScenes())
                    {
                        yield return null;
                    }
                }

                GameState.IsLoadingScene = false;
	            GameState.Paused = false;

	            try
	            {
		            if (loadedCallback != null)
		            {
			            loadedCallback();    
		            }
	            }
	            catch (System.Exception e)
	            {
		            Debug.LogException(e);
	            }

				try
				{
                    Debug.Log("Loading finished");
                    GameState.OnLoadingFinished?.Invoke();
				}
				catch (System.Exception e)
	            {
		            Debug.LogException(e);
	            }
            }

            public virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                
            }

            public virtual void OnDestroy()
            {
            }
        }

		public void NotifyGameAdsWatched()
		{
			//gameAdsWatched = true;
		}
		
		public void TryContinueGame()
		{
			if (lastAttemptGameStateType == null)
			{
				Debug.LogError("No game type ");
				return;
			}
			
			GotoState(lastAttemptGameStateType, lastAttemptGameStateData);
		}

		public void CancelGameContinue()
		{
			lastAttemptGameStateType = null;
			lastAttemptGameStateData = null;
		}

        [DefaultState]
        [UnityEngine.Scripting.Preserve]
        class Initial : GameStateBase
        {
            private AsyncOperationHandle<LocalizationSettings> localizationInitHandle;
            
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                localizationInitHandle = LocalizationSettings.InitializationOperation;
                GameState.StartCoroutine(update());
            }

            IEnumerator update()
            {
                while (localizationInitHandle.IsDone == false)
                {
                    yield return null;
                }
                
                Debug.Log($"Localization system init is done, default table: {LocalizationSettings.StringDatabase?.DefaultTable}");
                    
                if (IsOfflineMode)
                {
                    Debug.Log("Инициализация локальной игры...");
                    ForceGotoState<LocalLoading>();
                }
                else
                {
                    Debug.Log("Инициализация сетевой игры...");
                    ForceGotoState<Connecting>();
                }
            }

            public override void OnStateEnd(State nextState)
	        {
		        base.OnStateEnd(nextState);
		        try
		        {
			        GameState.OnInitializationFinished?.Invoke();
		        }
		        catch (Exception e)
		        {
			        Debug.LogException(e);
		        }		        
	        }
        }

        [UnityEngine.Scripting.Preserve]
        class Reloading : GameStateBase
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);

                if (IsOfflineMode)
                {
                    ForceGotoState<LocalLoading>();
                }
                else
                {
                    gotoMainMenu(prevState);
                }
            }

            async void gotoMainMenu(State prevState)
            {
                try
                {
                    if (GameState.IsLoadingScene == false)
                    {
                        Debug.Log("Loading started");
                        GameState.IsLoadingScene = true;
                        GameState.OnLoadingStarted?.Invoke();
                    }

                    if (prevState is OfflineGame == false)
                    {
                        await GameState.LoadPlayerGameData();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                ForceGotoState<MainMenu>();
            }
        }

        [UnityEngine.Scripting.Preserve]
        class FailedConnection : GameStateBase
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                Debug.LogError($"Не удалось подключиться, ошибка {Connecting.LastConnectionError}");
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
            }
        }

        [UnityEngine.Scripting.Preserve]
        class LocalLoading : GameStateBase
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);

                load();
            }

            async void load()
            {
                try
                {
                    bool needsSave = false;

                    GameData playerGameData;
                    var savePath = ArenaPlayerDataLocalStoreSystem.GetDefaultSavePath();

                    var readBytes = await ArenaPlayerDataLocalStoreSystem.Read(savePath);
                    if (readBytes == null || readBytes.Length == 0)
                    {
                        playerGameData = Arena.SharedUtility.CreateDefaultGameData();
                        needsSave = true;
                    }
                    else
                    {
                        playerGameData = GameData.Parser.ParseFrom(readBytes);
                    }
                    GameState.PlayerData = playerGameData;
                    
                    if(needsSave)
                    {
                        GameState.SaveLocalGame();
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    GameState.PlayerData = Arena.SharedUtility.CreateDefaultGameData();
                }

                ForceGotoState<MainMenu>();
            }
        }

        [UnityEngine.Scripting.Preserve]
        public class Connecting : GameStateBase
        {
            public enum ConnectionError
            {
                None,
                FailedToAuthenticate,
                FailedToGetData
            }

            public enum ConnectingState
            {
                WaitingForCredentials,
                Authorizing,
                LoadingPlayerData,
                Failed,
                Finished
            }
            public static ConnectionError LastConnectionError
            {
                get; private set;
            }

            public static double LastAuthAttemptTime { get; private set; }
            
            public static ConnectingState State
            {
                get => _state; 
                private set
                {
                    _state = value;
                    Debug.Log($"Connection state changed: {_state}");
                    try
                    {
                        OnConnectionStateChanged?.Invoke(value);
                    }
                    catch(System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
            private static ConnectingState _state;

            static string email;
            static string password;
            bool wasFailed = false;

            public static event Action<ConnectionError> OnConnectionFailed;
            public static event Action<ConnectingState> OnConnectionStateChanged;

            public static string AuthToken { get; private set; }
            public static void ContinueWithCredentials(string email, string password)
            {
                Connecting.email = email;
                Connecting.password = password;
                State = ConnectingState.Authorizing;
            }

            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                LastConnectionError = ConnectionError.None;

                if(GameState.connectingUi == null)
                {
                    GameState.connectingUi = Instantiate(GameState.connectingUiPrefab);
                }
                GameState.connectingUi.SetActive(true);
                wasFailed = prevState is FailedConnection;
                connecting();
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);

                if(nextState is FailedConnection == false)
                {
                    GameState.connectingUi.SetActive(false);
                }

                if(LastConnectionError != ConnectionError.None)
                {
                    email = null;
                    password = null;
                    try
                    {
                        OnConnectionFailed?.Invoke(LastConnectionError);
                    }
                    catch(Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            async void connecting()
            {
                try
                {
                    //var result = await Authenticate(wasFailed);
                    var result = await Authenticate(true);

                    if (result == false)
                    {
                        LastConnectionError = ConnectionError.FailedToAuthenticate;
                        State = ConnectingState.Failed;
                        ForceGotoState<FailedConnection>();
                        return;
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    LastConnectionError = ConnectionError.FailedToAuthenticate;
                    State = ConnectingState.Failed;
                    ForceGotoState<FailedConnection>();
                    return;
                }
                
                Debug.Log("Загрузка данных игрока...");
                State = ConnectingState.LoadingPlayerData;

                if(GameState.gameService == null)
                {
                    GameState.gameService = ClientGameUtility.GetGameClient(GameState, GameState.FrontendServer.Address, GameState.FrontendServer.Port, GameState.FrontendServer.Certificate.text);
                }

                var getDataRequest = new GetGameDataRequest();

                try
                {
                    var playerDataRequestResult = await GameState.gameService.Service.GetGameDataAsync(getDataRequest);

                    if (playerDataRequestResult == null || playerDataRequestResult.Data == null)
                    {
                        Debug.LogError($"Не удалось получить данные игрока");
                        LastConnectionError = ConnectionError.FailedToGetData;
                        State = ConnectingState.Failed;
                        ForceGotoState<FailedConnection>();
                        return;
                    }

                    Debug.Log("Данные игрока успешно получены");
                    GameState.PlayerData = playerDataRequestResult.Data;

                    // сохраняем данные для локальной игры
                    GameState.SaveLocalGame();

                    // имя пользователя как идентификатор при валидации просмотров рекламы
                    Debug.LogError("SET SERVER ADS USER ID");
                    //TzarGames.Common.Ads.UnityLevelPlayService.ServerUserId = GameState.PlayerData.UserId;

                    // var shopDataRequestResult = await GameState.gameService.Service.GetShopDataAsync(new GetShopDataRequest());
                    // if(shopDataRequestResult.Shop == null)
                    // {
                    //     Debug.LogError($"Не удалось получить данные магазина");
                    //     LastConnectionError = ConnectionError.FailedToGetData;
                    //     State = ConnectingState.Failed;
                    //     ForceGotoState<FailedConnection>();
                    //     return;
                    // }
                    // Debug.Log("Данные магазина успешно получены");
                    // GameState.ShopData = shopDataRequestResult.Shop;
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    LastConnectionError = ConnectionError.FailedToGetData;
                    State = ConnectingState.Failed;
                    ForceGotoState<FailedConnection>();
                    return;
                }

                State = ConnectingState.Finished;
                ForceGotoState<MainMenu>();
            }

            static async Task<string> AuthenticateUsingEmailAndPassword()
            {
                if (string.IsNullOrEmpty(email))
                {
                    State = ConnectingState.WaitingForCredentials;
                }
                else
                {
                    State = ConnectingState.Authorizing;
                }

                while (State == ConnectingState.WaitingForCredentials)
                {
                    await Task.Yield();
                }

                var firebaseToken =
                    await Authentication.FirebaseSignInByEmailAndPassword(email, password);

                return firebaseToken;
            }

#if UNITY_ANDROID
            static bool isGooglePlayGamesActivated = false;
#endif

            static async Task<string> AuthenticateUsingSocialAccount(bool reAuth)
            {
                bool result = false;
                string error = null;
                bool waiting = true;

                Debug.Log("Authenticating using social network");

                State = ConnectingState.Authorizing;

#if UNITY_ANDROID
                GooglePlayGames.PlayGamesPlatform.Instance.Authenticate((status) => Debug.Log($"GPG auth status: {status}"));

                if (isGooglePlayGamesActivated == false)
                {
                    GooglePlayGames.PlayGamesPlatform.DebugLogEnabled = true;
                    isGooglePlayGamesActivated = true;
                }
#endif

                if(Social.localUser.authenticated == false || reAuth)
                {
                    Social.localUser.Authenticate((bool success, string errorMessage) =>
                    {
                        waiting = false;
                        result = success;
                        error = errorMessage;
                    });

                    while (waiting)
                    {
                        await Task.Yield();
                    }

                    if (result == false)
                    {
                        Debug.LogError("Failed to authenticate");
                        return null;
                    }
                }
                
                Debug.Log($"Social Auth finished, success: {result}, error: {error}");

                if (result)
                {
#if UNITY_ANDROID
                    string firebaseToken = null;
                    string authCode;

                    authCode = await getGooglePlayAuthCode(reAuth);

                    if (string.IsNullOrEmpty(authCode))
                    {
                        Debug.LogError("Failed to get goole play auth code");
                        return null;
                    }
                    
                    Debug.Log("Signing into firebase using social auth code");
                    try
                    {
                        firebaseToken = await Authentication.FirebaseSignInByGooglePlayGames(authCode);
                    }
                    catch(AggregateException aggrEx)
                    {
                        foreach(var ex in aggrEx.InnerExceptions)
                        {
                            if(ex is Firebase.FirebaseException fex)
                            {
                                Debug.LogException(fex);
                                Debug.LogError($"firebae auth failed, errorCode {fex.ErrorCode}");
                            }
                            else
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                    catch (Firebase.FirebaseException ex)
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"auth failed, errorCode {ex.ErrorCode}");
                        return null;
                    }
                    catch(Exception ex)
                    {
                        Debug.LogException(ex);

                        Debug.LogError($"auth failed");
                        return null;
                    }

                    return firebaseToken;
#elif UNITY_IOS
                    Debug.Log("Signing into firebase using game center");
                    var firebaseToken = await Authentication.FirebaseSignInByGameCenter();
                    return firebaseToken;
#else
                    throw new System.NotImplementedException();
#endif
                }
                else
                {
                    Debug.LogError(error);
                    return null;
                }
            }

            

#if UNITY_ANDROID
            private static async Task<string> getGooglePlayAuthCode(bool refreshToken)
            {
                string authCode = null;
                bool isWaiting = true;

                GooglePlayGames.PlayGamesPlatform.Instance.RequestServerSideAccess(refreshToken, (result) =>
                {
                    isWaiting = false;
                    authCode = result;
                });

                while(isWaiting)
                {
                    await Task.Yield();
                }

                if (string.IsNullOrEmpty(authCode))
                {
                    Debug.Log("Google Play auth code is null");
                    return null;
                }
                return authCode;
            }
#endif

            public static async Task<bool> Authenticate(bool useEmailAuthIfPossible = false)
            {
                bool isReauth = false;

                try
                {
                    var result = await AuthenticateInternal(false, useEmailAuthIfPossible);

                    if (result)
                    {
                        return true;
                    }
                    isReauth = true;
                    return await AuthenticateInternal(true, useEmailAuthIfPossible);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);

                    if(isReauth == false)
                    {
                        return await AuthenticateInternal(true, useEmailAuthIfPossible);
                    }
                    return false;
                }
            }


            static async Task<bool> AuthenticateInternal(bool reAuth, bool useEmailAuthIfPossible = false)
            {
                LastAuthAttemptTime = Time.realtimeSinceStartupAsDouble;

                Authentication.AuthServerIp = Instance.AuthServer.Address;
                Authentication.AuthServerPort = Instance.AuthServer.Port;
                
                Debug.Log($"Аутентификация на сервере {Authentication.AuthServerIp}:{Authentication.AuthServerPort}");

                string firebaseToken;
#if UNITY_EDITOR
                firebaseToken = await AuthenticateUsingEmailAndPassword();
#elif UNITY_ANDROID || UNITY_IOS
                if(useEmailAuthIfPossible)
                {
                    firebaseToken = await AuthenticateUsingEmailAndPassword();
                }
                else
                {
                    firebaseToken = await AuthenticateUsingSocialAccount(reAuth);
                }
#else
                firebaseToken = await AuthenticateUsingEmailAndPassword();
#endif

                if (firebaseToken == null)
                {
                    Debug.LogError("Failed to get firebase token");
                    return false;
                }

                AuthToken = await Authentication.AuthenticateUsingFirebaseToken(firebaseToken, Instance.AuthServer.Certificate.text);

                if (string.IsNullOrEmpty(AuthToken))
                {
                    Debug.LogError("Failed to auth using firebase token");
                    return false;
                }
                Debug.Log("Auth success " + AuthToken);
                return true;
            }
        }

        [UnityEngine.Scripting.Preserve]
        class Tutorial : GameStateBase
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                Debug.Log("Loading tutorial scene...");
                //GameState.CreateCharacter("Player", PlayerClass.Knight, true, false);
                LoadSceneAsync(GameState.tutorialSceneName, false);
			}
        }

        [UnityEngine.Scripting.Preserve]
        class MainMenu : GameStateBase
        {
            bool isTokenRefreshLaunched = false;

            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);

                

                if (SceneManager.GetActiveScene().name == GameState.mainMenuSceneName)
                {
                    Debug.Log("Main menu scene already loaded");
                    LoadedCallback();
                }
                else
                {
                    Debug.Log("Loading main menu scene...");
                    LoadSceneAsync(GameState.mainMenuSceneName, true, LoadedCallback);
                }

                if(isTokenRefreshLaunched == false)
                {
                    isTokenRefreshLaunched = true;
                    
                    GameState.StartCoroutine(refreshTokenLoop());
                }
			}

            IEnumerator refreshTokenLoop()
            {
                var interval = (int)(GameState.refreshTokenMinutesInterval * 60);
                bool hasError = false;

                var wait_after_error = new WaitForSeconds(3);
                var wait_interval = new WaitForSeconds(0.5f);

                while (true)
                {
                    if(hasError)
                    {
                        yield return wait_after_error;
                    }
                    else
                    {
                        while ((Time.realtimeSinceStartupAsDouble - Connecting.LastAuthAttemptTime) < interval)
                        {
                            yield return wait_interval;
                        }
                    }

                    if(GameState == null || !GameState)
                    {
                        Debug.Log("Stop refreshing tokens");
                        break;
                    }

                    while(GameState.CurrentState is MainMenu == false)
                    {
                        yield return wait_interval;
                    }

                    bool authResult = false;

                    Debug.Log("Refreshing token");

                    yield return Connecting.Authenticate();

                    Debug.Log($"Re-auth result {authResult}");
                }
            }

	        private void LoadedCallback()
	        {
		        try
		        {
                    GameState.OnMainMenuLoaded?.Invoke();
		        }
		        catch (Exception e)
		        {
			        Debug.LogException(e);
		        }
	        }
        }

        [UnityEngine.Scripting.Preserve]
        class Game : GameStateBase
	    {
		    public override void OnStateEnd(State nextState)
		    {
			    base.OnStateEnd(nextState);
			    GameState.lastAttemptGameStateType = null;
			    GameState.lastAttemptGameStateData = null;
		    }
            	    
		    public virtual void Continue()
		    {
			    GameState.lastAttemptGameStateType = null;
			    GameState.lastAttemptGameStateData = null;
			    
			    try
			    {
                    GameState.OnGameContinued?.Invoke();
			    }
			    catch (Exception e)
			    {
				    Debug.LogException(e);
			    }
		    }

		    public override bool OnStateValidate(State prevState)
		    {
			    GameState.lastAttemptGameStateType = this.GetType();
			    GameState.lastAttemptGameStateData = Parameters;
			    
			    var baseResult = base.OnStateValidate(prevState);
			    if (baseResult == false)
			    {
				    return false;
			    }
			    
			    if (GameState.CanContinueGame() == false)
			    {
				    //try
				    //{
					   // if (OnNeedPurchaseFullGame != null) OnNeedPurchaseFullGame();
				    //}
				    //catch (Exception e)
				    //{
					   // Debug.LogException(e);
				    //}
				    return false;
			    }
			    //GameState.gameAdsWatched = false;
			    return true;
		    }
        }


        public void StartGame()
        {
            if (IsOfflineMode)
            {
                GotoState<OfflineGame>();    
            }
            else
            {
                GotoState<OnlineGame>();
            }
        }

        [UnityEngine.Scripting.Preserve]
        class OnlineGame : Game
        {
            private CancellationTokenSource cts;
            
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);

                cts = new CancellationTokenSource();
                Continue();
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                clear();
            }

            void clear()
            {
                cts.Dispose();
                cts = null;
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
                clear();
            }

            public override void Continue()
            {
                base.Continue();

                GoToBaseLocation();
            }

            async Task<bool> roomConnection(bool safeArea, bool multiplayer, int gameSceneID, int spawnPointId, string matchType, CancellationToken cancellationToken)
            {
                AsyncDuplexStreamingCall<ClientRoomMessage, ServerRoomMessage> roomCall;

                if (safeArea)
                {
                    roomCall = GameState.gameService.Service.SafeAreaRoomConnection(cancellationToken: cancellationToken);
                }
                else
                {
                    roomCall = GameState.gameService.Service.GameRoomConnection(cancellationToken: cancellationToken);
                }
                var roomConnection = new RoomConnection(roomCall.RequestStream, roomCall.ResponseStream);

                Debug.Log($"Request for game... (safeArea: {safeArea})");
                var gameRequestMessage = RoomMessages.CreateGameRequestMessage(matchType);
                gameRequestMessage.MetaData.IntKeyValues.Add(MetaDataKeys.SceneId, gameSceneID);
                gameRequestMessage.MetaData.IntKeyValues.Add(MetaDataKeys.SpawnPointId, spawnPointId);

                if (multiplayer)
                {
                    gameRequestMessage.MetaData.BoolKeyValues.Add(MetaDataKeys.MultiplayerGame, true);
                }
                
                var roomTask = roomConnection.HandleRoomMessages((message) =>
                {
                    Debug.Log($"Received message from room: {message} (safeArea: {safeArea})");
                    return true;
                    
                }, cancellationToken); 
                
                _ = roomConnection.WriteRequest(gameRequestMessage);

                ServerRoomMessage endMessage = null;
                try
                {
                    endMessage = await roomTask;
                }
                finally
                {
                    roomConnection.Close();

                    if (endMessage != null)
                    {
                        Debug.Log($"Room connection (safeArea: {safeArea}) finished with result {endMessage.IsSuccess()}");    
                    }
                    else
                    {
                        Debug.Log($"Room connection (safeArea: {safeArea}) finished with unknown result");
                    }
                }
                
                if (endMessage.IsSuccess() == false)
                {
                    var activeSceneName = SceneManager.GetActiveScene().name;

                    if (activeSceneName == GameState.mainMenuSceneName)
                    {
                        Debug.Log("Failed to start game, reloading the main menu state");
                        ForceGotoState<MainMenu>();
                    }
                    else if(activeSceneName == GameState.clientGameSceneName)
                    {
                        if(safeArea)
                        {
                            Debug.Log("Failed to go to the safe zone, exiting to main menu");
                            ForceGotoState<MainMenu>();
                        }
                    }

                    return false;
                }
                
                var keyData = endMessage.GetEncryptionKey();
                var key = SymmetricEncryptionKey.CreateFromBytes(keyData);

                onlineGameInfo = new OnlineGameInfo(matchType, endMessage.GetGameServerHost(),
                    endMessage.GetGameServerPort(), key, null);
                
                Debug.Log($"loading client scene... (safeArea: {safeArea}) ");
                LoadSceneAsync(GameState.clientGameSceneName);
                return true;
            }

            async Task<bool> roomConnectionWrapper(bool safeArea, bool multiplayer, int gameSceneID, int spawnPointId, string matchType, CancellationToken cancellationToken)
            {
                try
                {
                    Debug.Log("start connecting to game server");
                    GameState.IsConnectingToGameServer = true;

                    var result = await roomConnection(safeArea, multiplayer, gameSceneID, spawnPointId, matchType, cancellationToken);

                    return result;
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
                finally
                {
                    GameState.IsConnectingToGameServer = false;
                    Debug.Log("finished connection to game server");
                }
            }


            public Task<bool> StartQuest(QuestGameInfo questGameInfo)
            {
                return roomConnectionWrapper(false, questGameInfo.Multiplayer, questGameInfo.GameSceneID.Value, questGameInfo.SpawnPointID,
                        questGameInfo.MatchType, cts.Token);
            }

            
            public Task<bool> GoToBaseLocation()
            {
                // TODO тип матча должен проверяться на стороне сервера
                // TODO тип спаун поинта должен проверяться на стороне сервера
                var character = GameState.SelectedCharacter;
                return roomConnectionWrapper(true, false, character.Progress.CurrentBaseLocation, character.Progress.CurrentBaseLocationSpawnPoint, "Town_1", cts.Token);
            }
        }

        [UnityEngine.Scripting.Preserve]
        class OfflineGame : Game
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                Continue();
            }

            public override void Continue()
            {
                base.Continue();
                GotoBaseLocation();
            }

            public Task<bool> StartQuest(QuestGameInfo questGameInfo)
            {
                offlineGameInfo = new OfflineGameInfo(questGameInfo.MatchType, questGameInfo.GameSceneID.Value, questGameInfo.SpawnPointID, questGameInfo.Parameters);
                LoadSceneAsync(GameState.localGameSceneName);
                return Task<bool>.FromResult(true);
            }
            
            public Task<bool> GotoBaseLocation()
            {
                string matchType;
                
                if (SharedUtility.IsSafeZoneLocation(GameState.SelectedCharacter.Progress.CurrentBaseLocation))
                {
                    matchType = "Town_1";
                }
                else
                {
                    matchType = "ArenaMatch";
                }

                var character = GameState.SelectedCharacter;
                offlineGameInfo = new OfflineGameInfo(matchType, character.Progress.CurrentBaseLocation, character.Progress.CurrentBaseLocationSpawnPoint, null);
                LoadSceneAsync(GameState.localGameSceneName);
                return Task<bool>.FromResult(true);
            }
        }

        public bool CanContinueGame()
		{
            return true;

            //if(SelectedCharacter.MaxStage < 6)
            //{
            //	return true;
            //}

            //return currentSaveGame.IsGamePurchased || gameAdsWatched;
        }

        public Task<bool> StartLocation(QuestGameInfo questGameInfo)
        {
            if (CurrentState is OnlineGame onlineGame)
            {
                return onlineGame.StartQuest(questGameInfo);
            }
            else if(CurrentState is OfflineGame offlineGame)
            {
                return offlineGame.StartQuest(questGameInfo);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void ExitFromMatch()
        {
            GoToBaseLocation();
        }

        public void GoToBaseLocation()
        {
            if (CurrentState is OnlineGame onlineGame)
            {
                onlineGame.GoToBaseLocation();
            }
            else if(CurrentState is OfflineGame offlineGame)
            {
                offlineGame.GotoBaseLocation();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        
#if UNITY_EDITOR
        static void loadScene(string sceneName, bool testPath = false)
        {
            string path;

            if(testPath)
            {
                path = "Assets/Test/Scenes/";
            }
            else
            {
                path = "Assets/Scenes/";
            }

            if(UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path + sceneName + ".unity");
            }
        }

        [UnityEditor.MenuItem("Arena/Загрузить сцену - Инициализация")]
        static void loadInitialization()
        {
            loadScene("Logic/Initial loading");
        }

        [UnityEditor.MenuItem("Arena/Загрузить сцену - Главное меню")]
        static void loadMenu()
        {
            loadScene("Logic/Main menu");
        }
        
        [UnityEditor.MenuItem("Arena/Загрузить сцену - Город")]
        static void loadMainArea()
        {
            loadScene("MainArea");
        }

       /* [UnityEditor.MenuItem("Arena/Загрузить сцену - Обучение")]
        static void loadTutorial()
        {
            loadScene("Tutorial");
        }*/

        [UnityEditor.MenuItem("Arena/Загрузить сцену - Тест-Арена")]
        static void loadTestArena()
        {
            loadScene("Test Playground", true);
        }

        [UnityEditor.MenuItem("Arena/Загрузить сцену/Главный город")]
        static void loadWorldMainCity()
        {
            loadScene("Main Area");
        }
#endif
	}
}