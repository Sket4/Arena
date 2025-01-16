using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.Quests
{
    [System.Serializable]
    public struct LocationSource : IComponentData
    {
    }

    [System.Serializable]
    public struct LocationElement : IBufferElementData
    {
        public Entity LocationPrefab;
    }

    [UseDefaultInspector]
    public class LocationSourceComponent : ComponentDataBehaviour<LocationSource>
    {
        [System.Serializable]
        public class LocationAuthoring
        {
            public string Name;
            public QuestKey Key;    
        }

        public LocationAuthoring[] Locations;
        
        protected override void Bake<K>(ref LocationSource serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            var locations = baker.AddBuffer<LocationElement>();

            foreach (var location in Locations)
            {
                locations.Add(new LocationElement { LocationPrefab = baker.ConvertObjectKey(location.Key) });
            }
        }
    }
}
