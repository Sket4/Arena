// Copyright 2012-2018 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Arena
{
	public class NotificationHandler : MonoBehaviour
	{
		private const string subjectKey = "SUBJECT";
		private const string rubyRewardKey = "DAILYRUBY";
        private const string hammerRewardKey = "DAILYHAMMER";

        const string commonTopicKey = "FCM_COMMON_TOPIC";

        [SerializeField]
		bool debugMode = false;
	
		[SerializeField]
		string appId;
	
		[SerializeField]
		string googleProjectNumber;

		[SerializeField] private UnityEvent OnDailyRuby;
        [SerializeField] private UnityEvent OnDailyHammer;

//        #if ENABLE_FIREBASE
//        IEnumerator Start()
//        {
//            while(GoogleFirebase.GoogleFirebaseApp.IsReady == false)
//            {
//                yield return null;
//            }


//            Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = Common.Privacy.CanCollectData;
//            Common.Privacy.OnDataCollectionPermissionChanged += Privacy_OnDataCollectionPermissionChanged;

//            Debug.Log("Push notification handler init OK");
//            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
//            Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;

//            if(Common.Privacy.CanCollectData)
//            {
//                Firebase.Messaging.FirebaseMessaging.SubscribeAsync(commonTopicKey);
//            }
//        }

//        private void Privacy_OnDataCollectionPermissionChanged(bool canCollectData)
//        {
//            Debug.Log("Changing data collection settings for Firebase Messaging to " + canCollectData);
//            Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = canCollectData;

//            if (canCollectData)
//            {
//                Firebase.Messaging.FirebaseMessaging.SubscribeAsync(commonTopicKey);
//            }
//            else
//            {
//                Firebase.Messaging.FirebaseMessaging.UnsubscribeAsync(commonTopicKey);
//            }
//        }

//        public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
//        {
//            Debug.Log("Received Registration Token: " + token.Token);
//        }

//        public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
//        {
//            Debug.Log("Received a new message from: " + e.Message.From);

//            var message = e.Message;
//            var data = message.Data;

//            if(data == null)
//            {
//                return;
//            }

//            foreach(var dataEntry in data)
//            {
//                handleNotificationData(dataEntry.Key, dataEntry.Value);
//            }
//        }

//        void handleNotificationData(string key, string value)
//		{
//			if (key == subjectKey)
//			{
//				if (value == rubyRewardKey)
//				{
//					OnDailyRuby.Invoke();
//				}
//                else if(value == hammerRewardKey)
//                {
//                    OnDailyHammer.Invoke();
//                }
//			}
//		}

//#if UNITY_EDITOR
//		[ContextMenu("Test daily ruby")]
//        void testRubyNotification()
//        {
//            testNotification(rubyRewardKey);
//        }
//        [ContextMenu("Test daily hammer")]
//        void testHammerNotification()
//        {
//            testNotification(hammerRewardKey);
//        }

//        void testNotification(string key)
//		{
//			//var result = new OSNotificationOpenedResult();
//			//result.notification = new OSNotification();
//			//result.notification.payload = new OSNotificationPayload();
//			//result.notification.payload.title = "Test push title";
//			//result.notification.payload.body = "Test push body";
//			//result.notification.payload.additionalData = new Dictionary<string, object>();
//   //         result.notification.payload.additionalData.Add(subjectKey, key);

//			//HandleNotification(result);
//		}
//#endif
//#endif
	}	
}
