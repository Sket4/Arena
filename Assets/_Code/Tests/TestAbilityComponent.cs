using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Entities;

namespace Arena.Tests
{
    public struct TestAbilityComponentData : IComponentData, IAbilityComponentJob<TestAbilityComponentJob>
    {
    }

    public struct TestAbilityComponentBufferElement : IBufferElementData
    {
        public byte Value;
    }

    public class TestAbilityComponent : ComponentDataBehaviour<TestAbilityComponentData>
    {
    }

    public struct TestAbilityComponentJob
    {
        public void OnStarted(UniversalCommandBuffer commands)
        {
        }

        public void OnUpdate(DynamicBuffer<TestAbilityComponentBufferElement> testElements, ref AbilityControl abilityControl, int commandBufferIndex)
        {

        }

        public void OnIdleUpdate(ref TestAbilityComponentData data, in Speed speed, float deltaTime)
        {

        }

        public void OnStopped(in TestAbilityComponentData data, Entity abilityEntity)
        {

        }

        public bool OnValidate(TestAbilityComponentData data, ref LivingState livingState, in DeathData deathData, ref DeltaTime deltaTime, Entity abilityEntity, int commandBufferIndex)
        {
            return true;
        }
    }
}
