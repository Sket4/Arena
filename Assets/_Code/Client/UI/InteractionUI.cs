// Copyright 2012-2024 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System.Collections;
using Arena.Quests;
using Arena.ScriptViz;
using TzarGames.GameCore;
using TzarGames.GameCore.Items;
using TzarGames.GameCore.ScriptViz;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class InteractionUI : TzarGames.GameFramework.UI.GameUIBase 
	{
		[SerializeField]
		Button interactButton = default;

		[SerializeField] private Image interactIcon;

		private Entity currentInteractingEntity;
		private Entity interactorEntity;

		protected override void Start()
		{
			base.Start();
			interactButton.gameObject.SetActive(false);
		}

		protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
		{
			base.OnSetup(ownerEntity, uiEntity, manager);
			var linkeds = GetBuffer<LinkedEntityGroup>();
			foreach (var linked in linkeds)
			{
				if (HasData<OverlappingEntities>(linked.Value))
				{
					interactorEntity = linked.Value;
					break;
				}
			}
		}

        IEnumerator loadSprite(Image image, WeakObjectReference<Sprite> sprite)
        {
	        if (sprite.LoadingStatus == ObjectLoadingStatus.None)
	        {
		        sprite.LoadAsync();
	        }

	        while (sprite.LoadingStatus != ObjectLoadingStatus.Completed && sprite.LoadingStatus != ObjectLoadingStatus.Error)
	        {
		        yield return null;
	        }

	        if (sprite.LoadingStatus == ObjectLoadingStatus.Completed)
	        {
		        image.sprite = sprite.Result;
	        }
	        
	        interactButton.gameObject.SetActive(true);
        }

        public void OnInteractButtonPressed()
        {
	        if (currentInteractingEntity != Entity.Null)
	        {
		        if (HasData<StoreItems>(currentInteractingEntity))
		        {
			        FindObjectOfType<GameUI>().ShowItemShop();
		        }
		        else if (HasData<CraftReceipts>(currentInteractingEntity))
		        {
			        Debug.LogError("not implemented");
		        }
		        else if (HasData<LocationElement>(currentInteractingEntity))
		        {
			        FindObjectOfType<GameUI>().ShowLocationSelectWindow();
		        }
		        else if(EntityManager.HasComponent<InteractionEventCommand>(currentInteractingEntity))
		        {
			        var eventCommands = GetBuffer<InteractionEventCommand>(currentInteractingEntity);
			        var aspect = EntityManager.GetAspect<ScriptVizAspect>(currentInteractingEntity);
			        var ecb = new EntityCommandBuffer(Allocator.Temp);
			        var commands = ecb.AsParallelWriter();
			        var handle = new ContextDisposeHandle(ref aspect, ref commands, 0, Time.deltaTime);
		        
			        foreach (var eventCommand in eventCommands)
			        {
				        if (eventCommand.CommandAddress.IsInvalid)
				        {
					        continue;
				        }
				        if (eventCommand.InteractorEntityOutputAddress.IsValid)
				        {
					        handle.Context.WriteToTemp(OwnerEntity, eventCommand.InteractorEntityOutputAddress);	       
				        }
				        handle.Execute(eventCommand.CommandAddress);
			        }
			        ecb.Playback(EntityManager);    
		        }
		        else
		        {
			        Debug.LogError("failed to interact");
		        }
	        }
        }

        private void Update()
        {
            if(EntityManager.World.IsCreated == false 
                || EntityManager.Exists(OwnerEntity) == false
                || EntityManager.HasComponent<PlayerController>(OwnerEntity) == false)
            {
                return;
            }
            {
                var interactingObjects = GetBuffer<OverlappingEntities>(interactorEntity);
                bool isInteracting = false;
                ItemIcon icon = default;
                
                foreach (var overlapping in interactingObjects)
                {
	                if (HasData<InteractiveObject>(overlapping.Entity) && IsEnabled<InteractiveObject>(overlapping.Entity))
	                {
		                isInteracting = true;

		                if (HasData<ItemIcon>(overlapping.Entity))
		                {
			                currentInteractingEntity = overlapping.Entity;
			                icon = GetData<ItemIcon>(overlapping.Entity);
		                }
	                }
                }

                if (isInteracting && icon.Sprite.IsReferenceValid)
                {
	                StartCoroutine(loadSprite(interactIcon, icon.Sprite));
                }
                else
                {
	                interactButton.gameObject.SetActive(false);
                }
            }
        }
    }
}
