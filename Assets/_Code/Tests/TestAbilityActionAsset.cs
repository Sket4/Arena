using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Tests
{
    public struct TestAbilityAction : IBufferElementData, IAbilityAction, IAbilityActionJob<TestAbilityActionJob>
    {
        public FixedString32Bytes Message;
        public int CallerId { get; set; }
        public byte ActionId { get; set; }
    }

    public struct TestAbilityActionCounter : IComponentData
    {
        public int Value;
    }
    
    public class TestAbilityActionAsset : AbilityActionAsset<TestAbilityAction>
    {
        public string Message;

        protected override TestAbilityAction Convert(IGCBaker baker)
        {
            return new TestAbilityAction
            {
                Message = Message
            };
        }


        public override void Bake(int callerId, byte actionId, IGCBaker baker)
        {
            base.Bake(callerId, actionId, baker);
            baker.AddComponent(new TestAbilityActionCounter());
        }
    }

    public struct TestAbilityActionJob
    {
        public ComponentTypeHandle<TestAbilityActionCounter> CounterType;
        
        public void Execute( int jobIndex, TestAbilityAction eventData, ref TestAbilityActionCounter counter)
        {
            counter.Value++;
            
            Debug.Log(eventData.Message);
        }
    }
}
