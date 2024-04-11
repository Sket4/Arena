using Arena.Client;
using UnityEngine;

namespace Arena.Client.UI.MainMenu
{
    public class RateAppButtonUI : MonoBehaviour
    {
        [SerializeField]
        UnityEngine.Events.UnityEvent onNotYetPressed = default;

        [SerializeField]
        UnityEngine.Events.UnityEvent onRateAndroid = default;

        void Start()
        {
            var game = GameState.Instance;

            if(game == null)
            {
                return;
            }

            Debug.Log("Not implemented");
            // if(game.CommonSaveGameData.HasInt(RateAppUI.RateAppPressedKey) == false)
            // {
            //     onNotYetPressed.Invoke();
            // }
        }

        public void Rate()
        {
            var game = GameState.Instance;

            if (game == null)
            {
                return;
            }

#if UNITY_ANDROID
            onRateAndroid.Invoke();
#elif UNITY_IOS

            throw new System.NotImplementedException();
            
            //IOSReviewRequest.RequestReview();

            //if (game.CommonSaveGameData.HasInt(RateAppUI.RateAppPressedKey) == false)
            //{
            //    game.CommonSaveGameData.SetInt(RateAppUI.RateAppPressedKey, 1);
            //    if(game.IsItSafeStateToSaveGame())
            //    {
            //        game.SaveGame();
            //    }
            //}
#endif
        }
    }
}
