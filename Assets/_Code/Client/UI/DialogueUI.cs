// Copyright 2012-2024 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System.Collections.Generic;
using Arena.Dialogue;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public struct DialogueAnswerData
    {
        public string Text;
        public Address CommandAddress;
    }
    
    public class DialogueUI : TzarGames.GameFramework.UI.GameUIBase
    {
        public TMPro.TextMeshProUGUI MessageText;
        public GameObject AnswerPrefab;
        public Transform AnswerContainer;

        protected override void Awake()
        {
            base.Awake();
            AnswerPrefab.SetActive(false);
        }

        public void ShowDialogue(Entity dialogueEntity, string message, IEnumerable<DialogueAnswerData> answers)
        {
            MessageText.text = message;

            foreach (Transform child in AnswerContainer)
            {
                if (child.gameObject == AnswerPrefab)
                {
                    continue;
                }
                Destroy(child.gameObject);
            }

            foreach (var answer in answers)
            {
                var answerUI = Instantiate(AnswerPrefab);
                answerUI.SetActive(true);
                answerUI.transform.SetParent(AnswerContainer);

                var text = Utility.FindChild(answerUI.transform, "answer text").GetComponent<TMPro.TextMeshProUGUI>();
                text.enabled = true;
                text.text = answer.Text;
                
                var button = answerUI.GetComponent<Button>();
                
                button.onClick.AddListener(() =>
                {
                    var ui = FindObjectOfType<GameUI>();
                    ui.ShowDialogueWindow(false);

                    var signalEntity = EntityManager.CreateEntity(typeof(DialogueAnswerSignal));
                    
                    EntityManager.SetComponentData(signalEntity, new DialogueAnswerSignal
                    {
                        ScriptVizEntity = dialogueEntity,
                        CommandAddress = answer.CommandAddress
                    });
                });
            }
        }
    }
}
