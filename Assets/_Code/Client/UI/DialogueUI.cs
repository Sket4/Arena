﻿// Copyright 2012-2024 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System.Collections.Generic;
using Arena.Dialogue;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
        public Texture2D DefaultImage;
        public RawImage Image;
        public TMPro.TextMeshProUGUI MessageText;
        public GameObject AnswerPrefab;
        public Transform AnswerContainer;
        public UnityEvent OnAnswerChosen;

        protected override void Awake()
        {
            base.Awake();
            AnswerPrefab.SetActive(false);
        }

        string replace(string original, string playerName)
        {
            return original.Replace("{playername}", playerName);
        }

        public void ShowDialogue(Entity playerEntity, Entity dialogueEntity, string message, Texture2D image, IEnumerable<DialogueAnswerData> answers)
        {
            var playerName = "Dinar";

            if (HasData<Name30>())
            {
                playerName = GetData<Name30>().Value.ToString();
            }
            
            Debug.Log($"Show dialogue from entity {dialogueEntity}");

            if (image)
            {
                Image.texture = image;
            }
            else
            {
                Image.texture = DefaultImage;
            }

            message = message.Trim();
            
            MessageText.text = replace(message, playerName);

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
                answerUI.transform.localScale = Vector3.one;

                var text = answerUI.GetComponent<TMPro.TextMeshProUGUI>();
                text.enabled = true;
                var anwserText = replace(answer.Text, playerName);
                anwserText = anwserText.Trim();
                text.text = anwserText; 
                
                var button = answerUI.GetComponent<Button>();
                
                button.onClick.AddListener(() =>
                {
                    var ui = FindObjectOfType<GameUI>();
                    ui.ShowDialogueWindow(false);

                    var signalEntity = EntityManager.CreateEntity(typeof(DialogueAnswerSignal));
                    
                    EntityManager.SetComponentData(signalEntity, new DialogueAnswerSignal
                    {
                        Player = playerEntity,
                        DialogueEntity = dialogueEntity,
                        CommandAddress = answer.CommandAddress
                    });
                    
                    OnAnswerChosen.Invoke();
                });
            }
        }
    }
}
