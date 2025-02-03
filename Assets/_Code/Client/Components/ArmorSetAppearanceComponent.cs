using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct ArmorSetAppearance : IComponentData
    {
        public Entity HeadSocket;
        public Entity RightHandWeaponSocket;
        public Entity LeftHandBowSocket;
        public Entity LeftFoot;
        public Entity RightFoot;

        public Entity SkinModel1;
        public Entity SkinModel2;
    }

    public class ArmorSetAppearanceComponent : ComponentDataBehaviourBase
    {
        public Transform HeadSocket;
        public Transform RightHandWeaponSocket;
        public Transform LeftHandBowSocket;
        public Transform LeftFoot;
        public Transform RightFoot;

        public Renderer SkinModel1;
        public Renderer SkinModel2;

        protected override void PreBake<T>(T baker)
        {
            var data = new ArmorSetAppearance
            {
                HeadSocket = baker.GetEntity(HeadSocket),
                RightHandWeaponSocket = RightHandWeaponSocket != null ? baker.GetEntity(RightHandWeaponSocket) : Entity.Null,
                LeftHandBowSocket = LeftHandBowSocket != null ? baker.GetEntity(LeftHandBowSocket) : Entity.Null,
                RightFoot = RightFoot != null ? baker.GetEntity(RightFoot) : Entity.Null,
                LeftFoot = LeftFoot != null ? baker.GetEntity(LeftFoot) : Entity.Null,
                SkinModel1 = baker.GetEntity(SkinModel1),
                SkinModel2 = baker.GetEntity(SkinModel2)
            };
            baker.AddComponent(data);
        }
    }
}
