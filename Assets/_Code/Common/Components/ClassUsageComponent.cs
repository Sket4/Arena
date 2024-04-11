using Unity.Entities;
using TzarGames.GameCore;

namespace Arena
{
    [System.Serializable]
    public struct ClassUsage : IComponentData
    {
        [MaskEnum]
        public CharacterClass Classes;

        public bool HasFlag(CharacterClass classValue)
        {
            return (Classes & classValue) != 0;
        }
    }
    
    public class ClassUsageComponent : ComponentDataBehaviour<ClassUsage>
    {
    }
}
