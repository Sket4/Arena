// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections;
using TzarGames.Common;
using TzarGames.Common.Events;
using TzarGames.Common.UI;
using TzarGames.GameFramework.UI;
using UnityEngine;

namespace Arena.Client.UI
{
    public class RubyShopUI : GameUIBase
	{
		[SerializeField] private TextUI rubyCountText = default;
		[SerializeField] private LocalizedStringAsset rubyCountFormat = default;

        [SerializeField] private LocalizedStringAsset veryBigPackText = default;
        [SerializeField] private LocalizedStringAsset smallPackText = default;
		[SerializeField] private LocalizedStringAsset mediumPackText = default;
		[SerializeField] private LocalizedStringAsset bigPackText = default;
		
		[SerializeField] private LocalizedStringAsset status_DuplicateTransaction = default;
		[SerializeField] private LocalizedStringAsset status_ExistingPurchasePending = default;
		[SerializeField] private LocalizedStringAsset status_PaymentDeclined = default;
		[SerializeField] private LocalizedStringAsset status_ProductUnavailable = default;
		[SerializeField] private LocalizedStringAsset status_PurchasingUnavailable = default;
		[SerializeField] private LocalizedStringAsset status_SignatureInvalid = default;
		[SerializeField] private LocalizedStringAsset status_UserCancelled = default;
		[SerializeField] private LocalizedStringAsset status_Unknown = default;
        
        [SerializeField] private TextUI smallPackTextField = default;
		[SerializeField] private TextUI mediumPackTextField = default;
		[SerializeField] private TextUI bigPackTextField = default;
        [SerializeField] private TextUI veryBigPackTextField = default;


        [SerializeField] private UIBase puchaseWindow = default;
		[SerializeField] private UIBase waitWindow = default;
		[SerializeField] private GameObject cancelWaitButton = default;
		[SerializeField] private float cancelWaitTime = 15;
		[SerializeField] private UIBase successWindow = default;
		[SerializeField] private UIBase failWindow = default;
		[SerializeField] private TextUI failureReasonText = default;
		[SerializeField] private float successShowTime = 3;

        [SerializeField] private UnityEngine.Events.UnityEvent onPurchaseSuccess = default;
        [SerializeField] private StringEvent onPurchaseFailed = default;

        private float minWaitTime = 1;
		private float lastWaitStartTime = 0;

		protected override void OnVisible()
		{
			base.OnVisible();
			updateRubyCount();
            ResetToDefaultState();
		}

		void updateRubyCount()
		{
			if (GameState.Instance != null)
			{
				rubyCountText.text = "0";//string.Format(rubyCountFormat, EndlessGameState.Instance.SelectedCharacter.Ruby);	
			}
		}

		class ShopStateBase : State
		{
			public RubyShopUI Shop
			{
				get
				{
					return Owner as RubyShopUI;	
				}
			}
		}

		[DefaultState]
		class Purchase : ShopStateBase
		{
			public override void OnStateBegin(State prevState)
			{
				base.OnStateBegin(prevState);
				Shop.puchaseWindow.SetVisible(true);
				
                Shop.waitWindow.SetVisible(false);
				Shop.successWindow.SetVisible(false);
				Shop.failWindow.SetVisible(false);

				Shop.updateRubyCount();

				var localizedPriceText = "";// EndlessInAppManager.GetLocalizedPriceForSmallPack();
				Shop.smallPackTextField.text =
					string.Format(Shop.smallPackText, localizedPriceText ?? "???");

                localizedPriceText = "";// EndlessInAppManager.GetLocalizedPriceForVeryBigPack();
				Shop.veryBigPackTextField.text =
                    string.Format(Shop.veryBigPackText, localizedPriceText ?? "???");
				
				localizedPriceText = "";// EndlessInAppManager.GetLocalizedPriceForMediumPack();
				Shop.mediumPackTextField.text =
					string.Format(Shop.mediumPackText, localizedPriceText ?? "???");
				
				localizedPriceText = "";// EndlessInAppManager.GetLocalizedPriceForBigPack();
				Shop.bigPackTextField.text =
					string.Format(Shop.bigPackText, localizedPriceText ?? "???");
			}

			public override void OnStateEnd(State nextState)
			{
				base.OnStateBegin(nextState);
				Shop.puchaseWindow.SetVisible(false);
			}
		}

		class Wait : ShopStateBase
		{
			public override void OnStateBegin(State prevState)
			{
				base.OnStateBegin(prevState);
				Shop.waitWindow.SetVisible(true);

				Shop.lastWaitStartTime = Time.time;
				Shop.cancelWaitButton.SetActive(false);
				Shop.StopCoroutine(cancelWaitButtonActivation());
				Shop.StartCoroutine(cancelWaitButtonActivation());
			}

			public override void OnStateEnd(State nextState)
			{
				base.OnStateBegin(nextState);
				Shop.waitWindow.SetVisible(false);
				Shop.StopCoroutine(cancelWaitButtonActivation());
			}

			IEnumerator cancelWaitButtonActivation()
			{
				yield return new WaitForSeconds(Shop.cancelWaitTime);
				Shop.cancelWaitButton.SetActive(true);
			}
		}

		class Success : ShopStateBase
		{
			public override void OnStateBegin(State prevState)
			{
				base.OnStateBegin(prevState);
				Shop.successWindow.SetVisible(true);
			}

			public override void OnStateEnd(State nextState)
			{
				base.OnStateBegin(nextState);
				Shop.successWindow.SetVisible(false);
                Shop.onPurchaseSuccess.Invoke();
			}
		}

		class Failure : ShopStateBase
		{
			public override void OnStateBegin(State prevState)
			{
				base.OnStateBegin(prevState);
				Shop.failWindow.SetVisible(true);
			}

			public override void OnStateEnd(State nextState)
			{
				base.OnStateBegin(nextState);
				Shop.failWindow.SetVisible(false);
			}
		}
		
		//[SerializeField] private Button[] purchaseButtons = default;

//		protected override void Awake()
//		{
//			base.Awake();
//			TzarGames.EventSystem.Event<IStoreListener>.AddHandler(this);
//		}
//
//		private void OnDestroy()
//		{
//			TzarGames.EventSystem.Event<IStoreListener>.RemoveHandler(this);
//		}

		public void ShowPurchaseWindow()
		{
			GotoState<Purchase>();
		}

		private void setFailureReasonText(PurchaseFailureReason reason)
		{
			string failureText = status_Unknown;
			switch(reason)
			{
			case PurchaseFailureReason.DuplicateTransaction:
				failureText = status_DuplicateTransaction;
				break;
			case PurchaseFailureReason.ExistingPurchasePending:
				failureText = status_ExistingPurchasePending;
				break;
			case PurchaseFailureReason.PaymentDeclined:
				failureText = status_PaymentDeclined;
				break;
			case PurchaseFailureReason.ProductUnavailable:
				failureText = status_ProductUnavailable;
				break;
			case PurchaseFailureReason.PurchasingUnavailable:
				failureText = status_PurchasingUnavailable;
				break;
			case PurchaseFailureReason.SignatureInvalid:
				failureText = status_SignatureInvalid;
				break;
			case PurchaseFailureReason.UserCancelled:
				failureText = status_UserCancelled;
				break;
			case PurchaseFailureReason.Unknown:
				failureText = status_Unknown;
				break;
			}
			failureReasonText.text = failureText;
		}

		private IEnumerator showResult(IPurchaseResult result)
		{
			//Debug.Log("Show result " + result.Success);
			if(Time.time - lastWaitStartTime < minWaitTime)
			{
				yield return new WaitForSeconds(minWaitTime - (Time.time - lastWaitStartTime));
			}

			if(result.Success)
			{
				StartCoroutine(showSuccessAndReturn());
			}
			else
			{
				setFailureReasonText(result.FailureReason);
				GotoState<Failure>();
				try
				{
					onPurchaseFailed.Invoke(result.FailureReason.ToString());
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		IEnumerator showSuccessAndReturn()
		{
			GotoState<Success>();
			yield return new WaitForSeconds(successShowTime);
			GotoState<Purchase>();
		}

		public void OnCancelPress()
		{
			
		}

		public void OnCancelWaitPress()
		{
			ShowPurchaseWindow();
		}

        public void OnBuyVeryBigPackPress()
        {
            GotoState<Wait>();

			throw new System.NotImplementedException();
			//EndlessInAppManager.BuyVeryBigRubyPack((x) =>
   //         {
   //             StartCoroutine(showResult(x));
   //         });
        }

        public void OnBuySmallPackPress()
		{
			GotoState<Wait>();

			throw new System.NotImplementedException();
			//EndlessInAppManager.BuySmallRubyPack((x) =>
			//{
			//	StartCoroutine(showResult(x));
			//});
		}
		
		public void OnBuyMediumPackPress()
		{
			GotoState<Wait>();

			throw new System.NotImplementedException();
			//EndlessInAppManager.BuyMediumRubyPack((x) =>
			//{
			//	StartCoroutine(showResult(x));
			//});
		}
		
		public void OnBuyBigPackPress()
		{
			GotoState<Wait>();

			throw new System.NotImplementedException();
			//EndlessInAppManager.BuyBigRubyPack((x) =>
			//{
			//	StartCoroutine(showResult(x));
			//});
		}
	}	
}
