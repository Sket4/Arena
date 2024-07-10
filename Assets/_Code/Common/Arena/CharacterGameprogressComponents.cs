using Unity.Entities;

namespace Arena
{
    public struct CharacterGameProgressFlags : IBufferElementData
    {
        public ushort Value;
    }

    public struct CharacterGameProgress : IComponentData
    {
        public uint CurrentStage;
        public int CurrentBaseLocationID;
    }
}