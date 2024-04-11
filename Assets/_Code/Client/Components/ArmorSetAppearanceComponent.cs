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
        [SerializeField]
        Transform rightHandWeaponSocket;

        [SerializeField]
        Transform leftHandBowSocket;

        [SerializeField]
        Transform leftFoot;

        [SerializeField]
        Transform rightFoot;

        public Transform RightHandWeaponSocket => rightHandWeaponSocket;
        public Transform LeftHandBowSocket => leftHandBowSocket;
        public Transform LeftFooot => leftFoot;
        public Transform RightFoot => rightFoot;

        protected override void PreBake<T>(T baker)
        {
            var data = new ArmorSetAppearance
            {
                RightHandWeaponSocket = rightHandWeaponSocket != null ? baker.GetEntity(rightHandWeaponSocket) : Entity.Null,
                LeftHandBowSocket = leftHandBowSocket != null ? baker.GetEntity(leftHandBowSocket) : Entity.Null,
                RightFoot = rightFoot != null ? baker.GetEntity(rightFoot) : Entity.Null,
                LeftFoot = leftFoot != null ? baker.GetEntity(leftFoot) : Entity.Null
            };
            baker.AddComponent(data);
        }
    }
}
