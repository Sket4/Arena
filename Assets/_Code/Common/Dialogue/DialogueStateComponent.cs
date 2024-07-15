using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Arena.Dialogue
{
    public struct DialogueUpdateState : IComponentData, IEnableableComponent
    {
        [HideInInspector]
        public float3 StartPosition;
    }
    
    public struct DialogueState : IComponentData
    {
        [HideInInspector]
        public Entity Companion;
        
        [HideInInspector]
        public Entity DialogueEntity;
    }
    
    public class DialogueStateComponent : ComponentDataBehaviour<DialogueState>
    {
        protected override void Bake<K>(ref DialogueState serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddComponent<DialogueUpdateState>();
            baker.SetComponentEnabled<DialogueUpdateState>(false);
        }
    }
}
