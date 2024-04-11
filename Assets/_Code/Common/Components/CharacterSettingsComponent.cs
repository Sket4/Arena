using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct SkipDisableColliderOnDeath : IComponentData
    {
    }

    public class CharacterSettingsComponent : ComponentDataBehaviourBase
    {
        public bool SkipDisableColliderOnDeath = false;

        protected override void PreBake<T>(T baker)
        {
            if (ShouldBeConverted(baker) == false)
            {
                return;
            }

            if (SkipDisableColliderOnDeath)
            {
                baker.AddComponent(new SkipDisableColliderOnDeath());
            }
        }
    }
}
