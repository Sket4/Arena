using Unity.Entities;

namespace Arena
{
    public struct CharacterGameProgressFlags : IBufferElementData
    {
        public ushort Value;
    }
    
    public struct CharacterGameProgressKeyValue : IBufferElementData
    {
        public ushort Key;
        public int Value;
    }

    public struct CharacterGameProgress : IComponentData
    {
        public int CurrentStage;
        public int CurrentBaseLocationID;
        public int CurrentBaseLocationSpawnPointID;
    }
}