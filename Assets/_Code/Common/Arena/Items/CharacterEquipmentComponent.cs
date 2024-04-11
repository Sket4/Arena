using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [System.Serializable]
    public struct CharacterEquipment : IComponentData
    {
        public Entity ArmorSet;
        public Entity RightHandWeapon;
        public Entity LeftHandShield;
        public Entity LeftHandBow;
    }

    public class CharacterEquipmentComponent : ComponentDataBehaviour<CharacterEquipment>
    {
    }
}
