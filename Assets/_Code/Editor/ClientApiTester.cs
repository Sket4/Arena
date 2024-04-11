using Arena.Client;
using TzarGames.MatchFramework.Client;
using UnityEditor;
using UnityEngine;
using TzarGames.MatchFramework.Database;
using System.Threading.Tasks;
using Arena.Server;
using TzarGames.MatchFramework;
using DatabaseGameLib;
using static TzarGames.MatchFramework.Client.ClientUtility;
using System.Linq;
using TzarGames.GameCore;

namespace Arena.Editor
{
    public class ClientApiTester : EditorWindow
    {
        [SerializeField]
        string authServerIP = "127.0.0.1";

        [SerializeField]
        int authServerPort = 30600;

        [SerializeField]
        string internalAuthServerIP = "127.0.0.1";

        [SerializeField]
        int internalAuthServerPort = 30700;

        [SerializeField]
        string frontendServerIP = "127.0.0.1";

        [SerializeField]
        int frontendServerPort = 31600;

        [SerializeField]
        string dbServerIp = "127.0.0.1";

        [SerializeField]
        int dbServerPort = 50052;

        int itemCountToModify = 1;

        int characterId;
        string characterName;
        ItemComponent itemType;
        int itemId;

        string authToken;

        Arena.Server.AuthService authService;
        CharacterData[] allCharacters;
        CharacterData selectedCharacterData;

        enum AuthType
        {
            Anonymously,
            EmailAndPassword,
        }

        AuthType authType;
        string email;
        string password;

        Vector2 scrollPos = Vector2.zero;

        const string emailKey = "API_TEST_EMAIL";
        const string passwordKey = "API_TEST_PASSWORD";

        [MenuItem("Arena/API Tester")]
        static void show()
        {
            var window = GetWindow<ClientApiTester>();
            window.email = EditorPrefs.GetString(emailKey, "");
            window.password = EditorPrefs.GetString(passwordKey, "");
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                draw();
            }
            GUILayout.EndScrollView();
        }

        private void draw()
        {
            authServerIP = EditorGUILayout.TextField("Auth server IP", authServerIP);
            authServerPort = EditorGUILayout.IntField("Authe server port", authServerPort);

            frontendServerIP = EditorGUILayout.TextField("Frontend IP", frontendServerIP);
            frontendServerPort = EditorGUILayout.IntField("Frontend Port", frontendServerPort);

            dbServerIp = EditorGUILayout.TextField("Database IP", dbServerIp);
            dbServerPort = EditorGUILayout.IntField("Database Port", dbServerPort);

            internalAuthServerIP = EditorGUILayout.TextField("Internal auth IP", internalAuthServerIP);
            internalAuthServerPort = EditorGUILayout.IntField("Internal auth Port", internalAuthServerPort);

            GUILayout.Space(20);

            authType = (AuthType)EditorGUILayout.EnumPopup("Тип авторизации", authType);

            switch (authType)
            {
                case AuthType.Anonymously:
                    break;
                case AuthType.EmailAndPassword:
                    {
                        var newEmail = EditorGUILayout.TextField("e-mail", email);
                        if (newEmail != email)
                        {
                            email = newEmail;
                            EditorPrefs.SetString(emailKey, email);
                        }
                        var newPassword = EditorGUILayout.TextField("password", password);
                        if (newPassword != password)
                        {
                            password = newPassword;
                            EditorPrefs.SetString(passwordKey, password);
                        }
                        if (GUILayout.Button("Создать аккаунт с email и паролем"))
                        {
                            CreateUserWithEmailAndPassword();
                        }
                    }
                    break;
            }

            if (GUILayout.Button("Авторизация"))
            {
                Autheticate();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Загрузить данные игрока"))
            {
                loadPlayerData();
            }

            if (GUILayout.Button("Загрузить всех персонажей"))
            {
                loadAllCharacters();
            }

            //if (playerData != null)
            {
                drawPlayerData();
            }

            GUILayout.Space(20);
            characterId = EditorGUILayout.IntField("Целевой персонаж", characterId);
            characterName = EditorGUILayout.TextField("Имя целевой персонажа", characterName);
            itemId = EditorGUILayout.IntField("Целевой ИД предмета", itemId);
            itemType = EditorGUILayout.ObjectField("Целевой тип предмета", itemType, typeof(ItemComponent), false) as ItemComponent;

            GUILayout.Space(20);

            if (GUILayout.Button("Выбрать персонажа"))
            {
                SelectCharacter();
            }

            if (GUILayout.Button("Купить целевой предмет"))
            {
                PurchaseItem();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Активировать целевой предмет"))
            {
                SetItemState(true);
            }
            if (GUILayout.Button("Деактивировать целевой предмет"))
            {
                SetItemState(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            itemCountToModify = EditorGUILayout.IntField("Кол-во предметов для добавления", itemCountToModify);

            if (GUILayout.Button("Добавить целевой предмет"))
            {
                modifyItems(true);
            }
            if (GUILayout.Button("Удалить целевой предмет"))
            {
                modifyItems(false);
            }
            //if (GUILayout.Button("Загрузить лидерборд"))
            //{
            //    loadLeaders();
            //}
            //if (GUILayout.Button("Получить награду за рекламу"))
            //{
            //    getAdReward();
            //}
        }

        void drawPlayerData()
        {
            //GUILayout.Label($"Отображаемое имя: {playerData.DisplayName}");
            //GUILayout.Label($"Выбранный персонаж: {playerData.SelectedCharacter}");
            //GUILayout.Label($"Монеты: {playerData.Money}");
            //GUILayout.Label($"Игровые очки: {playerData.GamePoints}");

            if(allCharacters != null)
            {
                GUILayout.Label("Персонажи:");

                foreach (var character in allCharacters)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Character ID: {character.ID} Name: {character.Name} Class: {character.Class}");
                    if (GUILayout.Button("Show data"))
                    {
                        if(selectedCharacterData == character)
                        {
                            selectedCharacterData = null;
                        }
                        else
                            selectedCharacterData = character;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(15);
            }
            


            //GUILayout.Label("Купленные предметы:");
            //foreach (var item in playerData.PurchasedItems)
            //{
            //    GUILayout.BeginHorizontal();
            //    GUILayout.Label($"Item ID: {item.TypeID} internalID: {item.ID}");
            //    GUILayout.EndHorizontal();
            //}
            //GUILayout.Space(15);

            if (selectedCharacterData != null)
            {
                drawCharacterData(selectedCharacterData);
            }
        }

        void drawCharacterData(CharacterData data)
        {
            GUILayout.Label($"ID: {data.ID}");
            GUILayout.Label($"Имя: {data.Name}");
            GUILayout.Label($"Класс: {data.Class}");

            GUILayout.Space(10);
            GUILayout.Label("Предметы:");
            var bag = data.ItemsData.Bags[0];
            
            foreach (var item in bag.Items)
            {
                GUILayout.Label("----------------------");
                var dataStr = item.Data != null ? item.Data.ToString() : null;
                GUILayout.Label($"Предмет: TypeID: {item.TypeID}, {dataStr}");
            }

            GUILayout.Space(10);
            GUILayout.Label("Расходуемые предметы:");
            foreach (var item in bag.ConsumableItems)
            {
                GUILayout.Label("----------------------");
                var dataStr = item.Data != null ? item.Data.ToString() : null;
                GUILayout.Label($"Предмет: TypeID: {item.TypeID}, кол-во: {item.Count}, {dataStr}");
            }
        }

        class SimpleAuthTokenProvider : IAuthTokenProvider
        {
            public SimpleAuthTokenProvider(string authToken)
            {
                AuthenticationToken = authToken;
            }

            public string AuthenticationToken { get; private set; }
        }

        string getFrontendCertificate()
        {
            var path = "Assets/_ProjectFiles/Data/Client/Certificates/front_public.txt";
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return textAsset.text;
        }

        string getAuthCertificate()
        {
            var path = "Assets/_ProjectFiles/Data/Client/Certificates/auth_public.txt";
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return textAsset.text;
        }

        string getInternalAuthCertificate()
        {
            var path = "Assets/_ProjectFiles/Data/Server/Certificates/auth_internal.txt";
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return textAsset.text;
        }

        string getDbCertificate()
        {
            var path = "Assets/_ProjectFiles/Data/Server/Certificates/db.txt";
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return textAsset.text;
        }

        TzarGames.MatchFramework.ServiceWrapper<ArenaClientService.ArenaClientServiceClient> getGameService()
        {
            return ClientGameUtility.GetGameClient(new SimpleAuthTokenProvider(authToken), frontendServerIP, frontendServerPort, getFrontendCertificate());
        }

        async void loadAllCharacters()
        {
            var gameService = getGameService();

            var request = new GetCharactersRequest
            {
            };
            var result = await gameService.Service.GetCharactersAsync(request);
            allCharacters = result.Characters.ToArray();
            var _ = gameService.ShutdownAsync();
            
            loadPlayerData();
        }

        async void loadPlayerData()
        {
            //playerData = null;
            //selectedCharacterData = null;
            //var gameService = getGameService();

            //var request = new Client.GetPlayerGameDataRequest
            //{
            //};
            //var result = await gameService.Service.GetPlayerGameDataAsync(request);
            //var _ = gameService.ShutdownAsync();
            //playerData = result.PlayerGameData;
            //if (playerData != null)
            //{
            //    Debug.Log("Данные загружены");
            //}
            //else
            //{
            //    Debug.LogError("Не удалось загрузить данные");
            //}
        }

        async Task<AccountId> getAccountId()
        {
            if (authService == null)
            {
                var certificate = getInternalAuthCertificate();
                authService = new Server.AuthService(TzarGames.MatchFramework.Server.ServerUtility.GetAuthClient(internalAuthServerIP, internalAuthServerPort, certificate).Service);
            }

            var authorizeResult = await authService.AuthorizeByUserToken(authToken);
            var accoundId = new AccountId();
            accoundId.Value = authorizeResult.PlayerId.Value;
            Debug.Log($"ID игрока: {accoundId.Value}");
            return accoundId;
        }

        async void modifyItems(bool add)
        {
            var certificate = getDbCertificate();
            var dbClient = DatabaseUtility.CreateDatabaseClient(dbServerIp, dbServerPort, certificate);
            var accountId = await getAccountId();

            var getSelectedResult = await dbClient.Service.GetSelectedCharacterForAccountAsync(new GetCharacterRequest { AccountId = accountId });

            var targetCharacter = getSelectedResult.Character;

            if (targetCharacter == null)
            {
                Debug.LogError("Failed to get selected character");
                return;
            }
            
            var bag = targetCharacter.ItemsData.Bags[0];

            if(add)
            {
                
                if(itemType.TryGetComponent(out ConsumableComponent component))
                {
                    bag.ConsumableItems.Add(new ConsumableItemData
                    {
                        ID = itemId,
                        TypeID = itemType.ID.Id,
                        Count = itemCountToModify
                    });
                }
                else
                {
                    bag.Items.Add(new ItemData
                    {
                        ID = itemId,
                        TypeID = itemType.ID.Id
                    });
                }
            }
            else
            {
                if (itemType.TryGetComponent(out ConsumableComponent component))
                {
                    bag.ConsumableItems.Add(new ConsumableItemData
                    {
                        ID = itemId,
                        TypeID = itemType.ID.Id,
                        Count = itemCountToModify
                    });
                }
                else
                {
                    bag.Items.Add(new ItemData
                    {
                        ID = itemId,
                        TypeID = itemType.ID.Id
                    });
                }
            }

            

            var request = new DbSaveCharactersRequest();
            request.Characters.Add(targetCharacter);

            var result = await dbClient.Service.SaveCharactersAsync(request);
            if (result.Success)
            {
                Debug.Log($"Транзакция для {accountId.Value} прошла успешно");
            }
            else
            {
                Debug.LogError("Failed to save target character");
            }

            loadPlayerData();
        }

        async void CreateUserWithEmailAndPassword()
        {
            await Authentication.FirebaseCreateUserWithEmailAndPassword(email, password);
        }

        async void Autheticate()
        {
            Authentication.AuthServerIp = authServerIP;
            Authentication.AuthServerPort = authServerPort;

            string firebaseToken = null;

            switch (authType)
            {
                case AuthType.Anonymously:
                    {
                        firebaseToken = await Authentication.FirebaseSignInAnonymously();
                    }
                    break;
                case AuthType.EmailAndPassword:
                    {
                        firebaseToken = await Authentication.FirebaseSignInByEmailAndPassword(email, password);
                    }
                    break;
            }

            authToken = await Authentication.AuthenticateUsingFirebaseToken(firebaseToken, getAuthCertificate());

            Debug.Log("Токен: " + authToken);
        }

        async void SelectCharacter()
        {
            var gameService = getGameService();

            var request = new SelectCharacterRequest
            {
                Name = characterName,
            };
            var result = await gameService.Service.SelectCharacterAsync(request);
            var _ = gameService.ShutdownAsync();
            Debug.Log("Выбор персонажа: " + result.Success);
            loadPlayerData();
        }

        //async void PurchaseCharacter()
        //{
        //    var gameService = getGameService();

        //    var request = new PurchaseCharacterRequest
        //    {
        //        CharacterID = characterId,
        //    };
        //    var result = await gameService.Service.PurchaseCharacterAsync(request);
        //    var _ = gameService.ShutdownAsync();
        //    Debug.Log("Выбор персонажа: " + result.Status);

        //    if (result.Status == PurchaseStatus.Success)
        //    {
        //        loadPlayerData();
        //    }
        //}

        async void PurchaseItem()
        {
            //var gameService = getGameService();

            //var request = new PurchaseItemRequest
            //{
            //    ItemID = itemId,
            //};
            //var result = await gameService.Service.PurchaseItemAsync(request);
            //var _ = gameService.ShutdownAsync();
            //Debug.Log("Покупка предмета: персонажа: " + result.Status);

            //if (result.Status == PurchaseStatus.Success)
            //{
            //    loadPlayerData();
            //}
        }

        async void SetItemState(bool state)
        {
            //var gameService = getGameService();

            //var request = new Client.SetItemStateRequest
            //{
            //    CharacterID = characterId,
            //    Activated = state,
            //    ItemID = itemId,
            //    GroupID = itemGroupId,
            //};
            //var result = await gameService.Service.SetCharacterItemStateAsync(request);
            //var _ = gameService.ShutdownAsync();
            //Debug.Log($"Состояние предмета: " + result.Success);

            //if (result.Success)
            //{
            //    loadPlayerData();
            //}
        }

        //async void loadLeaders()
        //{
            //var gameService = getGameService();

            //var request = new LeaderboardRequest
            //{
            //};

            //var result = await gameService.Service.GetLeaderboardDataAsync(request);
            //var _ = gameService.ShutdownAsync();

            //Debug.Log("Данные о лидерах загружены:");
            //Debug.Log("Топ игроки:");
            //foreach (var item in result.TopLeaders)
            //{
            //    Debug.Log($"{item.Rank}. Имя: {item.PlayerName}, очки: {item.Rating}");
            //}
            //Debug.Log("Ближайшие игроки:");
            //foreach (var item in result.NearPlayers)
            //{
            //    Debug.Log($"{item.Rank}. Имя: {item.PlayerName}, очки: {item.Rating}");
            //}
        //}

        //async void getAdReward()
        //{
        //    var gameService = getGameService();

        //    var request = new AdRewardRequest();

        //    var result = await gameService.Service.GetAdRewardAsync(request);
        //    var _ = gameService.ShutdownAsync();

        //    Debug.Log("Результат получения награды за рекламу: " + result.Success);
        //    Debug.Log($"Получено золота: {result.Money} всего: {result.TotalMoney}");
        //    Debug.Log($"Предметы: {result.Rewards.Count}");
            
        //    foreach (var item in result.Rewards)
        //    {
        //        Debug.Log($"{item.ItemID}. Кол-во: {item.Amount}");
        //    }
        //}
    }
}
