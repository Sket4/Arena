using System;
using Unity.Entities;

namespace Arena.Quests
{
    public enum QuestRequestStatus
    {
        InProcess,
        Success
    }
    
    public struct QuestRequest : IComponentData
    {
        public Entity Hirer;                // наниматель
        public Entity Executor;             // исполнитель
        public Guid RequestGuid;            // ИД запроса
        public QuestRequestStatus Status;
    }
}
