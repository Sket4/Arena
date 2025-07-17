 // Copyright 2012-2025 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

 using TzarGames.Common.UI;
 using UnityEngine;
 using UnityEngine.UI;

 namespace Arena.Client.UI
{
    public class CharacterUI : TzarGames.GameFramework.UI.GameUIBase
    {
        [System.Serializable]
        class TabInfo
        {
            public Button TabButton;
            public UIBase UI;
        }
	    public InventoryUI Inventory;
        public AbilitiesUI Abilities = default;
        public GameObject AbilityNotification;

        [SerializeField]
        private TabInfo[] Tabs;

        class BaseState : State
        {
            protected CharacterUI UI => this.Owner as CharacterUI; 
        }

        [DefaultState]
        class Empty : BaseState
        {
        }

        public void UpdateAbilityNotification()
        {
            var abilityPoints = GetData<AbilityPoints>();
            AbilityNotification.SetActive(abilityPoints.Count > 0);
        }

        public override void SetVisible(bool visible)
        {
            base.SetVisible(visible);
            if (visible == false)
            {
                GotoState<Empty>();
            }
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            UpdateAbilityNotification();
        }

        void activateTab(UIBase ui)
        {
            foreach (var tabInfo in Tabs)
            {
                if (tabInfo.UI == ui)
                {
                    tabInfo.TabButton.interactable = false;
                }
                else
                {
                    tabInfo.TabButton.interactable = true;
                }
            }
        }

        class InventoryState : BaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.Inventory.SetVisible(true);
                UI.Inventory.RefreshItems();
                UI.activateTab(UI.Inventory);
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.Inventory.SetVisible(false);
            }
        }
        
        class AbilityState : BaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.Abilities.SetVisible(true);
                UI.activateTab(UI.Abilities);
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.Abilities.SetVisible(false);
            }
        }

        public void ShowInventory()
        {
            GotoState<InventoryState>();
        }
        
        public void ShowAbilities()
        {
            GotoState<AbilityState>();
        }
    }
}
