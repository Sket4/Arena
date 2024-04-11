using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;

namespace Arena.Client
{
    [System.Serializable]
    public struct AnimationPlayEvent : IComponentData
    {
        public int AnimationID;
        public bool AutoDestroy;
    }

    [UseDefaultInspector]
    public class AnimationPlayEventComponent : ComponentDataBehaviour<AnimationPlayEvent>
    {
        public AnimationID AnimationID = default;
        public bool AutoDestroy = true;

        protected override void Bake<K>(ref AnimationPlayEvent serializedData, K baker)
        {
            if(AnimationID != null)
            {
                serializedData.AnimationID = AnimationID.Id;
            }
            else
            {
                serializedData.AnimationID = AnimationID.Invalid;
            }
            serializedData.AutoDestroy = AutoDestroy;
        }
    }
}
