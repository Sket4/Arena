// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using System.Collections;
using TzarGames.Common.UI;
using TzarGames.GameFramework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class InventoryItemUI : MonoBehaviour, IPoolable 
	{
		[SerializeField]
		Image iconImage = default;

		[SerializeField]
		RawImage activeIcon = default;

		[SerializeField]
		RawImage selectedImage = default;

		[SerializeField] private TextUI count = default;

		public event System.Action<InventoryItemUI> OnItemClicked;

        private Entity itemInstance;
        Coroutine itemAnimationCoroutine;

        public Entity ItemEntity
		{
			get
            {
                return itemInstance;
            }
            set
            {
                if(itemInstance == value)
                {
                    return;
                }
                itemInstance = value;
                updateSpriteAnimationState();
            }
		}

        void updateSpriteAnimationState()
        {
            //if (Item != null)
            //{
            //    if (itemAnimationCoroutine != null)
            //    {
            //        StopCoroutine(itemAnimationCoroutine);
            //        itemAnimationCoroutine = null;
            //    }

            //    if(gameObject.activeInHierarchy && enabled)
            //    {
            //        var animComponent = Item.GetComponent<AnimatedIconItemComponent>();
            //        if (animComponent != null)
            //        {
            //            itemAnimationCoroutine = StartCoroutine(itemAnimation(Item, animComponent));
            //        }
            //    }
            //}
            //else
            //{
            //    if (itemAnimationCoroutine != null)
            //    {
            //        StopCoroutine(itemAnimationCoroutine);
            //    }
            //}
        }

        //IEnumerator itemAnimation(Item item, AnimatedIconItemComponent animComponent)
        //{
        //    var sprites = animComponent.Sprites;
        //    var frameTime = 1.0f / animComponent.FPS;
        //    int currentFrame = 0;

        //    while (true)
        //    {
        //        iconImage.sprite = sprites[currentFrame];
        //        yield return new WaitForSeconds(frameTime);
        //        currentFrame++;
        //        if (currentFrame >= sprites.Length)
        //        {
        //            currentFrame = 0;
        //        }
        //    }
        //}

        void OnEnable()
        {
            updateSpriteAnimationState();
        }

        void OnDisable()
        {
            if (itemAnimationCoroutine != null)
            {
                StopCoroutine(itemAnimationCoroutine);
                itemAnimationCoroutine = null;
            }
        }

		public void NotifyItemClicked()
		{
			if (OnItemClicked != null) OnItemClicked.Invoke(this);
		}

        public void OnPushedToPool()
        {
            itemInstance = Entity.Null;
            iconImage.sprite = null;
            count.enabled = false;
        }

        public void OnPulledFromPool()
        {
            count.enabled = true;
        }

        public Sprite ItemIcon
		{
			get
			{
				return iconImage.sprite;
			}
			set
			{
				iconImage.sprite = value;
			}
		}

		public bool IsActivated
		{
			get
			{
				return activeIcon.gameObject.activeSelf;
			}
			set
			{
				activeIcon.gameObject.SetActive(value);
			}
		}

		public bool Selected
		{
			get
			{
				return selectedImage.gameObject.activeSelf;
			}
			set
			{
				selectedImage.gameObject.SetActive(value);
			}
		}

		public string Count
		{
			get { return count.text; }
			set { count.text = value; }
		}

		public bool ShowCount
		{
			get { return count.enabled; }
			set { count.enabled = value; }
		}
	}
}
