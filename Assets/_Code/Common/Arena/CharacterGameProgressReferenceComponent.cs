using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct CharacterGameProgressReference : IComponentData
    {
        public Entity Value;
    }
    public class CharacterGameProgressReferenceComponent : ComponentDataBehaviour<CharacterGameProgressReference>
    {
    }
}
