using UnityEngine;

namespace Arena.Client.UI
{
    public class GlobalUI : MonoBehaviour
    {
        [SerializeField]
        private AlertUI alert = default;
        
        public static GlobalUI Instance { get; private set; }
        
        public AlertUI Alert
        {
            get { return alert; }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("More than one GlobalUI is not allowed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
