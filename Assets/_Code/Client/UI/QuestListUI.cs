using System;
using System.Collections;
using System.Collections.Generic;
using Arena.Quests;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
	public class QuestListUI : GameUIBase
	{
		[SerializeField] private UIBase mainWindow;
		[SerializeField] private UIBase waitWindow;
		[SerializeField] private QuestItemUI itemPrefab;
		[SerializeField] private GameObject startButton;
		[SerializeField] private RectTransform container;
		[SerializeField] private TextUI description;
		[SerializeField] private TextUI noTasksText;
		[SerializeField] private GameObject startMultiplayerButton;

		private QuestItemUI activeQuest;
		private Dictionary<QuestItemUI, Entity> questMap = new Dictionary<QuestItemUI, Entity>();

		[SerializeField] private EntityEvent onQuestStarted;
		
		Entity getCurrentHirer()
		{
			Entity targetEntity = Entity.Null;
			var overlappings = GetBuffer<OverlappingEntities>();
            
			foreach (var overlapping in overlappings)
			{
				if (HasData<QuestElement>(overlapping.Entity))
				{
					targetEntity = overlapping.Entity;
					break;
				}
			}
			return targetEntity;
		}

		protected override void OnVisible()
		{
			base.OnVisible();
			
			waitWindow.SetVisible(false);
			mainWindow.SetVisible(true);
			
			TzarGames.GameCore.Utility.DestroyAllChilds(container);
			questMap.Clear();
			activeQuest = null;
			
			startMultiplayerButton.SetActive(GameState.IsOfflineMode == false);

			var currentHirer = getCurrentHirer();

			if (currentHirer == Entity.Null)
			{
				return;
			}

			var tasks = GetBuffer<QuestElement>(currentHirer);

			for (var i = 0; i < tasks.Length; i++)
			{
				var task = tasks[i];
				var newItem = Instantiate(itemPrefab);
				var clientData = GetObjectData<QuestClientData>(task.QuestPrefab);
				newItem.ClientData = clientData;
				newItem.Label = clientData.Name;
				newItem.transform.SetParent(container);
				newItem.transform.localScale = Vector3.one;
				newItem.OnClicked += NewItemOnOnClicked;
				newItem.Activated = false;
				questMap.Add(newItem, task.QuestPrefab);

				if (activeQuest == null)
				{
					activeQuest = newItem;
				}
			}

			if (activeQuest != null)
			{
				activeQuest.Activated = true;
				var selectable = activeQuest.GetComponent<Selectable>();
				if (selectable != null)
				{
					selectable.Select();
				}

				description.text = activeQuest.ClientData.Description;
				startButton.SetActive(true);	
			}
			else
			{
				startButton.SetActive(false);
				description.text = "";
			}

			if (questMap.Count > 0)
			{
				noTasksText.gameObject.SetActive(false);
			}
			else
			{
				noTasksText.gameObject.SetActive(true);
			}
		}

		private void NewItemOnOnClicked(QuestItemUI taskItemUi)
		{
			activeQuest = taskItemUi;

			foreach (var task in questMap)
			{
				if (task.Key == activeQuest)
				{
					task.Key.Activated = true;
				}
				else
				{
					task.Key.Activated = false;
				}
			}

			if (activeQuest != null)
			{
				description.text = activeQuest.ClientData.Description;
				startButton.SetActive(true);
			}
		}

		public void OnStartMultiplayerTaskClicked()
		{
			StartCoroutine(startTask(true));
		}

		IEnumerator startTask(bool multiplayer)
		{
			if (activeQuest == null)
			{
				yield break;
			}
			
			waitWindow.SetVisible(true);
			mainWindow.SetVisible(false);
			
			var quest = questMap[activeQuest];

			using(var query = EntityManager.CreateEntityQuery(typeof(GameInterface)))
			{
				var gameInterface = query.GetSingleton<GameInterface>();

				int spawnPointId = 0;

				if (HasData<SpawnPointIdData>(quest))
				{
					spawnPointId = GetData<SpawnPointIdData>(quest).ID;
				}
				
				var questTask = gameInterface.StartQuest(new QuestGameInfo
				{
					GameSceneID = GetData<QuestData>(quest).GameSceneID,
					SpawnPointID = spawnPointId,
					MatchType = "ArenaMatch",
					Multiplayer = multiplayer
				});
				
				onQuestStarted.Invoke(quest);

				while (questTask.IsCompleted == false)
				{
					yield return null;
				}
				
				waitWindow.SetVisible(false);
					
				if (questTask.IsCompleted)
				{
					if (questTask.Result == false)
					{
						mainWindow.SetVisible(true);	
					}
				}
				else
				{
					mainWindow.SetVisible(true);
				}
			}
		}

		public void OnStartTaskClicked()
		{
			StartCoroutine(startTask(false));
		}
	}	
}
