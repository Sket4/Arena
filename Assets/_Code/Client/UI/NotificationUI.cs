using System.Collections.Generic;
using TzarGames.Common;
using UnityEngine;

namespace Arena.Client.UI
{
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField] private RectTransform container = default;
        [SerializeField] private NotificationEntryUI entryPrefab = default;

        private Pool<EntryInfo> pool = null;
        private List<EntryInfo> tempEntries = new List<EntryInfo>();
        private List<EntryInfo> constantEntryUis = new List<EntryInfo>();

        class EntryInfo
        {
            public NotificationEntryUI EntryUi;
            public float StartTime;
            public float Time;
            public bool IsConstant;
        }

        private EntryInfo createObjectCallback()
        {
            var entryInfo = new EntryInfo();
            entryInfo.EntryUi = Instantiate(entryPrefab);
            entryInfo.EntryUi.gameObject.SetActive(false);
            return entryInfo;
        }
        
        private void Awake()
        {
            if (pool == null)
            {
                initPool();    
            }
        }

        void initPool()
        {
            pool = new Pool<EntryInfo>(createObjectCallback, 10);
            pool.CreateObjects(10);
        }

#if UNITY_EDITOR
        [ConsoleCommand]
#endif
        public void AddTempNotification(string message, float time)
        {
            AddTempNotificationWithIcon(message, null, time);
        }

        public void AddTempNotificationWithIcon(string message, Sprite icon, float time)
        {
            if (pool == null)
            {
                initPool();
            }
            var entryInfo = pool.Get();
            entryInfo.EntryUi.gameObject.SetActive(true);
            entryInfo.EntryUi.transform.localScale = Vector3.one;
            entryInfo.StartTime = Time.time;
            entryInfo.EntryUi.Message = message;
            entryInfo.EntryUi.Icon = icon;
            entryInfo.Time = time;
            entryInfo.IsConstant = false;
            entryInfo.EntryUi.transform.SetParent(container, false);
            tempEntries.Add(entryInfo);
        }

#if UNITY_EDITOR
        [ConsoleCommand]
#endif
        public NotificationEntryUI AddConstantNotification(string message, string id = null)
        {
            if (pool == null)
            {
                initPool();    
            }

            if (string.IsNullOrEmpty(id) == false)
            {
                RemoveConstantNotificationById(id);    
            }
            
            var entry = pool.Get();
            entry.EntryUi.ID = id;
            entry.EntryUi.Message = message;
            entry.EntryUi.gameObject.SetActive(true);
            entry.IsConstant = true;
            entry.EntryUi.transform.localScale = Vector3.one;
            entry.EntryUi.Icon = null;
            entry.EntryUi.transform.SetParent(container, false);
            if (constantEntryUis.Count == 0)
            {
                entry.EntryUi.transform.SetAsFirstSibling();
            }
            else
            {
                var last = constantEntryUis[constantEntryUis.Count - 1];
                entry.EntryUi.transform.SetSiblingIndex(last.EntryUi.transform.GetSiblingIndex()+1);
            }
            constantEntryUis.Add(entry);
            return entry.EntryUi;
        }

        public void RemoveConstantNotification(NotificationEntryUI entryUi)
        {
            for (int i = constantEntryUis.Count-1; i >= 0; i--)
            {
                var e = constantEntryUis[i];

                if (e.EntryUi == entryUi)
                {
                    removeEntry(entryUi);
                    constantEntryUis.Remove(e);
                    pool.Set(e);
                }
            }
        }

        public void RemoveConstantNotificationById(string id)
        {
            for (int i = constantEntryUis.Count-1; i >= 0; i--)
            {
                var e = constantEntryUis[i];

                if (e.EntryUi.ID == id)
                {
                    RemoveConstantNotification(e.EntryUi);
                    break;
                }
            }
        }

        void removeEntry(NotificationEntryUI entryUi)
        {
            entryUi.transform.SetParent(null);
            entryUi.gameObject.SetActive(false);
        }

        private void Update()
        {
            int tempCount = tempEntries.Count;
            var time = Time.time;
            
            for (int i = tempCount - 1; i >= 0; i--)
            {
                var entry = tempEntries[i];
                if (time - entry.StartTime >= entry.Time)
                {
                    tempEntries.RemoveAt(i);   
                    removeEntry(entry.EntryUi);
                    if (pool.Set(entry) == false)
                    {
                        Destroy(entry.EntryUi.gameObject);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            foreach(var entry in tempEntries)
            {
                if (entry.EntryUi == null || !entry.EntryUi)
                {
                    continue;
                }
                Destroy(entry.EntryUi.gameObject);
            }

            foreach(var entry in pool)
            {
                if(entry.EntryUi == null || !entry.EntryUi)
                {
                    continue;
                }
                Destroy(entry.EntryUi.gameObject);
            }
        }
    }
}
