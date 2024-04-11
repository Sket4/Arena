// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using Arena.Client;

namespace Arena.Client.UI
{
    public class DisconnectedMenuUI : TzarGames.GameFramework.UI.GameUIBase
    {
        public void ExitToMainMenu()
        {
            GameState.Instance.ExitToMainMenu();
        }
    }
}
