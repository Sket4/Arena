using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public sealed class CharacterGameProgress : IComponentData
    {
        public GameProgress Value;
    }
    
    public class CharacterGameProgressComponent : ComponentDataClassBehaviour<CharacterGameProgress>
    {
    }
}
