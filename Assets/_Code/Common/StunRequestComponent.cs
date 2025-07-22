using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [System.Serializable]
    public struct StunRequest : IComponentData
    {
        public float Duration;
    }

    [System.Serializable]
    public struct StunDuration : IComponentData
    {
        public float Value;
    }

    public struct Stunned : IComponentData
    {
        public bool PendingFinish;
        public bool PendingStart;
        public double StartTime;
        public Entity ModificatorOwner;
    }

    public class StunRequestComponent : ComponentDataBehaviour<StunRequest>
    {
    }
}
