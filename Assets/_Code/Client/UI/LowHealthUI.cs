using System;
using System.Collections;
using TzarGames.Common;
using TzarGames.GameFramework;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using TzarGames.GameCore;

namespace Arena.Client.UI
{
	public class LowHealthUI : GameUIBase
	{
		[SerializeField] private CanvasRenderer[] canvasRenderers = default;
		[SerializeField] private float activationPercent = 0.3f;
		private int renderCount;
		

		protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
		{
			base.OnSetup(ownerEntity, uiEntity, manager);
			renderCount = canvasRenderers.Length;
		}

		private void OnDisable()
		{
			for (int i = 0; i < renderCount; i++)
			{
				var r = canvasRenderers[i];
				r.GetComponent<Graphic>().color = Color.clear;
			}
		}

		private void OnEnable()
		{
			StartCoroutine(enableColor());
		}

		IEnumerator enableColor()
		{
			yield return null;
			for (int i = 0; i < renderCount; i++)
			{
				var r = canvasRenderers[i];
				r.GetComponent<Graphic>().color = Color.white;
			}
		}

		void Update () 
		{
			if (OwnerEntity == Entity.Null || HasData<Health>() == false)
			{
				return;
			}

			var hp = GetData<Health>();

			var percentHP = activationPercent * hp.ModifiedHP;

			if (percentHP <= 0)
			{
				for (int i = 0; i < renderCount; i++)
				{
					var r = canvasRenderers[i];
					r.SetAlpha(1);
				}
				return;
			}
			

			var normalized = 1.0f - (hp.ActualHP / percentHP);
			var currentAlpha = canvasRenderers[0].GetAlpha();

			if (Math.Abs(currentAlpha - normalized) < FMath.KINDA_SMALL_NUMBER)
			{
				return;
			}
			
			if (hp.ActualHP <= percentHP)
			{
				for (int i = 0; i < renderCount; i++)
				{
					var r = canvasRenderers[i];
					r.SetAlpha(normalized);
				}
			}
			else
			{
				for (int i = 0; i < renderCount; i++)
				{
					var r = canvasRenderers[i];
					r.SetAlpha(0);
				}
			}
		}
	}
}
