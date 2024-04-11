using UnityEngine;

namespace Arena
{
    [CreateAssetMenu(fileName = "ZoneSharedData", menuName = "Arena/Zone shared data")]
    public class ZoneSharedData : ScriptableObject
    {
        [System.Serializable]
        class AreaData
        {
            public GameLocationType LocationType = default;
            public string SceneName = default;
        }

        [SerializeField]
        AreaData[] areas = default;


        public string GetAreaSceneName(GameLocationType targetArea)
        {
            if(areas == null || areas.Length == 0)
            {
                return null;
            }

            foreach(var area in areas)
            {
                if(area.LocationType != targetArea)
                {
                    continue;
                }

                return area.SceneName;
            }

            return null;
        }
    }
}
