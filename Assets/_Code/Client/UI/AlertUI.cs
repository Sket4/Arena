// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System.Collections.Generic;
using TzarGames.Common;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class AlertUI : StateMachine
    {
        [SerializeField]
        TextUI text = default;

        [SerializeField]
        Animation _animation = default;

        [SerializeField] private RawImage image;
        
        List<string> messages = new List<string>();


        class AlertBaseState : State
        {
            public AlertUI UI
            {
                get
                {
                    return Owner as AlertUI;
                }
            }
        }
        
        [DefaultState]
        class Disabled : AlertBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.messages.Clear();
                UI.text.enabled = false;

                if (UI.image != null)
                {
                    UI.image.enabled = false;    
                }
                
                UI._animation.Stop();
            }
        }

        class Showing : AlertBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                if (UI.image != null)
                {
                    UI.image.enabled = true;    
                }
                UI.text.enabled = true;
            }

            void show(string msg)
            {
                UI.text.text = msg;
                UI._animation.Play();
            }

            public override void Update()
            {
                base.Update();
                if (UI._animation.isPlaying == false)
                {
                    if (UI.messages.Count > 0)
                    {
                        show(UI.messages[0]);
                        UI.messages.RemoveAt(0);
                    }
                    else
                    {
                        UI.GotoState<Disabled>();
                    }
                }
            }
        }

        [ConsoleCommand]
        public void Show(string message)
        {
            messages.Add(message);
            GotoState<Showing>();
        }

        public void Disable()
        {
            GotoState<Disabled>();
        }

        private void Update()
        {
            CurrentState.Update();
        }
    }
}
