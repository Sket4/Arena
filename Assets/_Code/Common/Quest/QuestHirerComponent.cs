using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.Quests
{
    [System.Serializable]
    public struct QuestHirer : IComponentData
    {
    }

    [System.Serializable]
    public struct QuestElement : IBufferElementData
    {
        public Entity QuestPrefab;
    }

    [UseDefaultInspector]
    public class QuestHirerComponent : ComponentDataBehaviour<QuestHirer>
    {
        [System.Serializable]
        public class QuestAuthoring
        {
            public string Name;
            public QuestKey Key;    
        }

        public QuestAuthoring[] Quests;
        
        protected override void Bake<K>(ref QuestHirer serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            var quests = baker.AddBuffer<QuestElement>();

            foreach (var quest in Quests)
            {
                quests.Add(new QuestElement { QuestPrefab = baker.ConvertObjectKey(quest.Key) });
            }
        }
    }
}
