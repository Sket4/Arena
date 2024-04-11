// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using Arena.Client;
using TzarGames.Common.UI;
using UnityEngine;

namespace Arena.Client.UI.MainMenu
{
    public class RateAppUI : UIBase
    {
        public const string RateAppPressedKey = "RATE_APP_BUTTON_PRESSED";

        [SerializeField]
        string androidLink = "market://details?id=com.tzargamestudio.endless";

        public void Rate()
        {
#if UNITY_ANDROID
            Application.OpenURL(androidLink);
#endif
            var game = GameState.Instance;

            if (game == null)
            {
                return;
            }

            Debug.Log("Not implemented");
            // if (game.CommonSaveGameData.HasInt(RateAppPressedKey) == false)
            // {
            //     game.CommonSaveGameData.SetInt(RateAppPressedKey, 1);
            //     if (game.IsItSafeStateToSaveGame())
            //     {
            //         game.SaveGame();
            //     }
            // }
        }
    }
}
