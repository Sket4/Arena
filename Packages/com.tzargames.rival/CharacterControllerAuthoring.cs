using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;
using Unity.CharacterController;

namespace TzarGames.GameCore.CharacterController
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhysicsShapeAuthoring))]
    public class CharacterControllerAuthoring : MonoBehaviour
    {
        public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();
        public CharacterControllerComponent Character = CharacterControllerComponent.GetDefault();

        public class Baker : Baker<CharacterControllerAuthoring>
        {
            public override void Bake(CharacterControllerAuthoring authoring)
            {
                KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.CharacterProperties);

                AddComponent(authoring.Character);
                AddComponent(new CharacterInputs());
            }
        }
    }
}
