using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [System.Serializable]
    public struct Slot : IComponentData
    {
        public byte Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class SlotComponent : ComponentDataBehaviour<Slot>
    {
    }
}
