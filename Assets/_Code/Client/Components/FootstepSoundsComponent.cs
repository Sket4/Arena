using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct FootstepSoundGroupElement : IBufferElementData
    {
        public byte PhysicsMaterialTags;
        public Entity SoundGroupEntity;
    }

    public struct FootstepSoundsTag : IComponentData {}

    [UseDefaultInspector]
    public class FootstepSoundsComponent : DynamicBufferBehaviour<FootstepSoundGroupElement>
    {
        public bool SetAsSingleton = false;

        [System.Serializable]
        class FootstepSoundGroup
        {
            public CustomPhysicsMaterialTags PhysicsMaterialTags;
            public SoundGroupComponent SoundGroup;
        }

        [SerializeField]
        FootstepSoundGroup[] Groups;

        protected override void Bake<K>(ref DynamicBuffer<FootstepSoundGroupElement> serializedData, K baker)
        {
            foreach(var group in Groups)
            {
                serializedData.Add(new FootstepSoundGroupElement
                {
                    PhysicsMaterialTags = group.PhysicsMaterialTags.Value,
                    SoundGroupEntity = baker.GetEntity(group.SoundGroup)
                }); 
            }

            if(SetAsSingleton)
            {
                baker.AddComponent(new FootstepSoundsTag());
            }
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
