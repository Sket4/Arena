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
        public Entity ShieldSocket;
        public Entity LeftFoot;
        public Entity RightFoot;

        public Entity SkinModel1;
        public Entity SkinModel2;
        
        public Entity ColoredModel1;
        public Entity ColoredModel2;
    }

    public class ArmorSetAppearanceComponent : ComponentDataBehaviourBase
    {
        public Transform HeadSocket;
        public Transform RightHandWeaponSocket;
        public Transform LeftHandBowSocket;
        public Transform ShieldSocket;
        public Transform LeftFoot;
        public Transform RightFoot;

        public Renderer SkinModel1;
        public Renderer SkinModel2;
        public Renderer ColoredModel1;
        public Renderer ColoredModel2;

        protected override void PreBake<T>(T baker)
        {
            var data = new ArmorSetAppearance
            {
                HeadSocket = baker.GetEntity(HeadSocket),
                RightHandWeaponSocket = RightHandWeaponSocket != null ? baker.GetEntity(RightHandWeaponSocket) : Entity.Null,
                LeftHandBowSocket = LeftHandBowSocket != null ? baker.GetEntity(LeftHandBowSocket) : Entity.Null,
                ShieldSocket = baker.GetEntity(ShieldSocket),
                RightFoot = RightFoot != null ? baker.GetEntity(RightFoot) : Entity.Null,
                LeftFoot = LeftFoot != null ? baker.GetEntity(LeftFoot) : Entity.Null,
                SkinModel1 = baker.GetEntity(SkinModel1),
                SkinModel2 = baker.GetEntity(SkinModel2),
                
                ColoredModel1 = baker.GetEntity(ColoredModel1),
                ColoredModel2 = baker.GetEntity(ColoredModel2),
            };
            baker.AddComponent(data);
        }
    }
}
