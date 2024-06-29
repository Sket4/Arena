using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct ArmorSetAppearance : IComponentData
    {
        public Entity RightHandWeaponSocket;
        public Entity LeftHandBowSocket;
        public Entity LeftFoot;
        public Entity RightFoot;
    }

    public class ArmorSetAppearanceComponent : ComponentDataBehaviourBase
    {
        public Transform RightHandWeaponSocket;
        public Transform LeftHandBowSocket;
        public Transform LeftFoot;
        public Transform RightFoot;

        protected override void PreBake<T>(T baker)
        {
            var data = new ArmorSetAppearance
            {
                RightHandWeaponSocket = RightHandWeaponSocket != null ? baker.GetEntity(RightHandWeaponSocket) : Entity.Null,
                LeftHandBowSocket = LeftHandBowSocket != null ? baker.GetEntity(LeftHandBowSocket) : Entity.Null,
                RightFoot = RightFoot != null ? baker.GetEntity(RightFoot) : Entity.Null,
                LeftFoot = LeftFoot != null ? baker.GetEntity(LeftFoot) : Entity.Null
            };
            baker.AddComponent(data);
        }
    }
}
