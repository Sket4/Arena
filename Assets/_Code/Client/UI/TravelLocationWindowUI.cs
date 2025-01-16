using System.Collections;
using System.Collections.Generic;
using Arena.Quests;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Items;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
	public class TravelLocationWindowUI : GameUIBase
	{
		[SerializeField] private UIBase mainWindow;
		[SerializeField] private UIBase waitWindow;
		[SerializeField] private LocationItemUI itemPrefab;
		[SerializeField] private GameObject startButton;
		[SerializeField] private RectTransform container;
		[SerializeField] private Image icon;
		[SerializeField] private TextUI description;
		[SerializeField] private GameObject startMultiplayerButton;

		private LocationItemUI _activeLocation;
		private Dictionary<LocationItemUI, Entity> locationMap = new();

		[SerializeField] private EntityEvent onLocationStarted;
		
		// игрок должен находиться рядом с источником, предоставляющим локации 
		Entity getCurrentLocationHolder()
		{
			Entity targetEntity = Entity.Null;

			var linkeds = GetBuffer<LinkedEntityGroup>();
			var interactor = Entity.Null;
			
			foreach (var linked in linkeds)
			{
				if (HasData<OverlappingEntities>(linked.Value))
				{
					interactor = linked.Value;
					break;
				}
			}
			var overlappings = GetBuffer<OverlappingEntities>(interactor);
            
			foreach (var overlapping in overlappings)
			{
				if (HasData<LocationElement>(overlapping.Entity))
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
			
			Utility.DestroyAllChilds(container);
			locationMap.Clear();
			_activeLocation = null;
			
			startMultiplayerButton.SetActive(GameState.IsOfflineMode == false);

			var currentHirer = getCurrentLocationHolder();

			if (currentHirer == Entity.Null)
			{
				return;
			}

			var tasks = GetBuffer<LocationElement>(currentHirer);

			for (var i = 0; i < tasks.Length; i++)
			{
				var task = tasks[i];
				var newItem = Instantiate(itemPrefab);
				var clientData = GetObjectData<LocationClientData>(task.LocationPrefab);
				newItem.ClientData = clientData;
				if (HasData<ItemIcon>(task.LocationPrefab))
				{
					var iconData = GetData<ItemIcon>(task.LocationPrefab);
					StartCoroutine(ItemIcon.LoadIcon(iconData.Sprite, (result) =>
					{
						newItem.Icon = result;
						
						if (_activeLocation == newItem)
						{
							icon.sprite = result;
						}
					}));
				}
				else
				{
					newItem.Icon = null;
				}

				newItem.Label = clientData.Name.TryGetLocalizedString();
				newItem.transform.SetParent(container);
				newItem.transform.localScale = Vector3.one;
				newItem.OnClicked += NewItemOnOnClicked;
				newItem.Activated = false;
				locationMap.Add(newItem, task.LocationPrefab);

				if (_activeLocation == null)
				{
					_activeLocation = newItem;
				}
			}

			if (_activeLocation != null)
			{
				_activeLocation.Activated = true;
				var selectable = _activeLocation.GetComponent<Selectable>();
				if (selectable != null)
				{
					selectable.Select();
				}

				description.text = _activeLocation.ClientData.Description.TryGetLocalizedString();
				icon.sprite = _activeLocation.Icon;
				startButton.SetActive(true);	
			}
			else
			{
				startButton.SetActive(false);
				icon.sprite = null;
				description.text = "";
			}
		}

		private void NewItemOnOnClicked(LocationItemUI taskItemUi)
		{
			_activeLocation = taskItemUi;

			foreach (var task in locationMap)
			{
				if (task.Key == _activeLocation)
				{
					task.Key.Activated = true;
				}
				else
				{
					task.Key.Activated = false;
				}
			}

			if (_activeLocation != null)
			{
				description.text = _activeLocation.ClientData.Description.TryGetLocalizedString();
				icon.sprite = _activeLocation.Icon;
				startButton.SetActive(true);
			}
		}

		public void OnConfirmMultiplayerClicked()
		{
			StartCoroutine(startTask(true));
		}

		IEnumerator startTask(bool multiplayer)
		{
			if (_activeLocation == null)
			{
				yield break;
			}
			
			waitWindow.SetVisible(true);
			mainWindow.SetVisible(false);
			
			var location = locationMap[_activeLocation];

			using(var query = EntityManager.CreateEntityQuery(typeof(GameInterface)))
			{
				var gameInterface = query.GetSingleton<GameInterface>();

				int spawnPointId = 0;

				if (HasData<SpawnPointIdData>(location))
				{
					spawnPointId = GetData<SpawnPointIdData>(location).ID;
				}

				GameParameter[] parameters;

				if (HasData<GameParameter>(location))
				{
					parameters = GetBuffer<GameParameter>(location).AsNativeArray().ToArray();
				}
				else
				{
					parameters = null;
				}
				
				var locationTask = gameInterface.StartLocation(new QuestGameInfo
				{
					GameSceneID = GetData<QuestData>(location).GameSceneID,
					SpawnPointID = spawnPointId,
					MatchType = "ArenaMatch",
					Multiplayer = multiplayer,
					Parameters = parameters
				});
				
				onLocationStarted.Invoke(location);

				while (locationTask.IsCompleted == false)
				{
					yield return null;
				}
				
				waitWindow.SetVisible(false);
					
				if (locationTask.IsCompleted)
				{
					if (locationTask.Result == false)
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

		public void OnConfirmClicked()
		{
			StartCoroutine(startTask(false));
		}
	}	
}
