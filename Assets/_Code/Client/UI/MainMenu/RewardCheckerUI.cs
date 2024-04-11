using UnityEngine;

namespace Arena.Client.UI.MainMenu
{
    public class RewardCheckerUI : MonoBehaviour
    {
        [SerializeField]
        UnityEngine.Events.UnityEvent onAnyRewardAvailable = default;

        void Start()
        {
            Debug.LogError("Not implemented");
            // var handlers = GetComponentsInChildren<IRewardHandler>();
            // bool canGetAnyReward = false;
            //
            // foreach(var h in handlers)
            // {
            //     if(h == null)
            //     {
            //         Debug.LogError("Null reward handler");
            //         continue;
            //     }
            //
            //     if(Rewarder.Instance != null && Rewarder.Instance.CanGetReward(h.Reward))
            //     {
            //         canGetAnyReward = true;
            //         break;
            //     }
            // }
            //
            // if(canGetAnyReward)
            // {
            //     onAnyRewardAvailable.Invoke();
            // }
        }
    }
}
