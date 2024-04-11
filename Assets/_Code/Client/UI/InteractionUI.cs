// Copyright 2012-2022 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using Arena;
using Arena.Client;
using Arena.Quests;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class InteractionUI : TzarGames.GameFramework.UI.GameUIBase 
	{
		[SerializeField]
		Button openShopButton = default;

        [SerializeField]
        Button readyButton = default;

        [SerializeField]
		Button openForgeButton = default;
		
		[SerializeField]
		Button openTaskListButton = default;

        [SerializeField]
        ActionDetectorUI detectorUI = default;

		protected override void Start()
		{
			base.Start();
			
			ShowOpenShopButton (false);
			ShowOpenForgeButton (false);
			ShowTaskListButton(false);
            ShowReadyButton(false);
		}

		public void ShowOpenShopButton(bool show)
		{
			openShopButton.gameObject.SetActive (show);
		}
		public void ShowOpenForgeButton(bool show)
		{
			openForgeButton.gameObject.SetActive (show);
		}
		public void ShowTaskListButton(bool show)
		{
			openTaskListButton.gameObject.SetActive (show);
		}
        public void ShowReadyButton(bool show)
        {
            readyButton.gameObject.SetActive(show);
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            detectorUI.SetPlayerOwner(ownerEntity);
        }

        private void Update()
        {
            if(EntityManager.World.IsCreated == false 
                || EntityManager.Exists(OwnerEntity) == false
                || EntityManager.HasComponent<PlayerController>(OwnerEntity) == false)
            {
                return;
            }

            var playerEntity = GetData<PlayerController>(OwnerEntity).Value;


            //if (EntityManager.HasComponent<ArenaPlayerMatchData>(playerEntity) == false)
            //{
            //    return;
            //}

            //var matchState = EntityManager.GetComponentData<ArenaPlayerMatchData>(playerEntity);

            //if(matchState.InFight)
            //{
            //    readyButton.gameObject.SetActive(false);
            //    openShopButton.gameObject.SetActive(false);
            //}
            //else
            {
                //readyButton.gameObject.SetActive(true);

                var interactingObjects = GetBuffer<OverlappingEntities>();
                bool isInteractingWithShop = false;
                bool isInteractingWithForge = false;
                bool isInteractingWithQuestHirer = false;
                
                foreach (var overlapping in interactingObjects)
                {
	                if (HasData<StoreItems>(overlapping.Entity))
	                {
		                isInteractingWithShop = true;
	                }

	                if (HasData<CraftReceipts>(overlapping.Entity))
	                {
		                isInteractingWithForge = true;
	                }

	                if (HasData<QuestElement>(overlapping.Entity))
	                {
		                isInteractingWithQuestHirer = true;
	                }
                }
                openShopButton.gameObject.SetActive(isInteractingWithShop);
                openForgeButton.gameObject.SetActive(isInteractingWithForge);
                openTaskListButton.gameObject.SetActive(isInteractingWithQuestHirer);
            }
        }
    }
}
