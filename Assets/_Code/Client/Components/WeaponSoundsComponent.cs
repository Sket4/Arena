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
    }

    [UseDefaultInspector]
    public class WeaponSoundsComponent : ComponentDataBehaviour<WeaponSounds>
    {
        [SerializeField]
        SoundGroupComponent swordSwings;

        protected override void Bake<K>(ref WeaponSounds serializedData, K baker)
        {
            serializedData.SwordSwingsGroup = swordSwings ? baker.GetEntity(swordSwings) : default;
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
