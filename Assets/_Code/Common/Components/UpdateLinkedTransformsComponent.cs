using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    ///
    /// для обновления позиций всех привязанных entity в LinkedEntityGroup
    ///
    public struct UpdateLinkedTransforms : IComponentData
    {
    }

    public class UpdateLinkedTransformsComponent : ComponentDataBehaviour<UpdateLinkedTransforms>
    {
    }
}
