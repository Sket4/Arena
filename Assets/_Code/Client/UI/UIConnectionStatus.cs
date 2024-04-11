using UnityEngine;

namespace FallGame.Client.UI
{
    public class UIConnectionStatus : MonoBehaviour
    {
        [SerializeField] GameObject wifiIcon;
        [SerializeField] GameObject cellularIcon;
        [SerializeField] GameObject noNetworkIcon;

        void Update()
        {
            var netStatus = Application.internetReachability;
            noNetworkIcon.SetActive(netStatus == NetworkReachability.NotReachable);
            cellularIcon.SetActive(netStatus == NetworkReachability.ReachableViaCarrierDataNetwork);
            wifiIcon.SetActive(netStatus == NetworkReachability.ReachableViaLocalAreaNetwork);            
        }
    }
}
