using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.Client
{
    [System.Serializable]
    public struct OtherCategoryStoreTag : IComponentData
    {
    }
    
    public class OtherCategoryStoreTagComponent : ComponentDataBehaviour<OtherCategoryStoreTag>
    {
    }
}
