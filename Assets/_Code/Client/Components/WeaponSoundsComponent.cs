using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct WeaponSounds : IComponentData
    {
        public Entity SwordSwingsGroup;
        public Entity CommonSwingsGroup;
    }

    [UseDefaultInspector]
    public class WeaponSoundsComponent : ComponentDataBehaviour<WeaponSounds>
    {
        [SerializeField]
        SoundGroupComponent swordSwings;
        
        [SerializeField]
        SoundGroupComponent commonSwings;

        protected override void Bake<K>(ref WeaponSounds serializedData, K baker)
        {
            serializedData.SwordSwingsGroup = baker.GetEntity(swordSwings);
            serializedData.CommonSwingsGroup = baker.GetEntity(commonSwings);
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
