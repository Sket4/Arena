using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;

namespace Arena.GameSceneCode
{
    [System.Serializable]
    public struct TransitionZone : IComponentData
    {
        [System.NonSerialized]
        public TransitionZoneState State;

        [System.NonSerialized]
        public double TransitionStartTime;

        public float ActivationTime;
    }

    public enum TransitionZoneState : byte
    {
        Pending,
        Transition,
        Activated
    }

    [System.Serializable]
    public struct TransitionSectionToLoad : IBufferElementData
    {
        public int SectionIndex;
    }

    [System.Serializable]
    public struct TransitionSectionToUnload : IBufferElementData
    {
        public int SectionIndex;
    }

    [System.Serializable]
    public struct TransitionZoneEnableObject : IBufferElementData
    {
        public Entity Entity;
    }

    [System.Serializable]
    public struct TransitionZoneDisableObject : IBufferElementData
    {
        public Entity Entity;
    }

    [System.Serializable]
    public struct TransitionStartMessage : IComponentData
    {
        public Message Message;
    }

    [System.Serializable]
    public struct TransitionFinishedMessage : IComponentData
    {
        public Message Message;
    }

    [UseDefaultInspector]
    public class TransitionZoneComponent : ComponentDataBehaviour<TransitionZone>
    {
        public float ActivationTime = 3;

        public SceneSectionComponent[] SectionsToLoad;
        public SceneSectionComponent[] SectionsToUnload;
        public GameObject[] ObjectsToEnable;
        public GameObject[] ObjectsToDisable;

        public MessageAuthoring StartTransitionMessage;
        public MessageAuthoring FinishTransitionMessage;

        protected override void Bake<K>(ref TransitionZone serializedData, K baker)
        {
            serializedData.ActivationTime = ActivationTime;

            baker.AddComponent<SessionEntityReference>();

            var toLoad = baker.AddBuffer<TransitionSectionToLoad>();

            foreach (var section in SectionsToLoad)
            {
                toLoad.Add(new TransitionSectionToLoad { SectionIndex = section.SectionIndex });
            }

            var toUnload = baker.AddBuffer<TransitionSectionToUnload>();

            foreach (var section in SectionsToUnload)
            {
                toUnload.Add(new TransitionSectionToUnload { SectionIndex = section.SectionIndex });
            }

            var toEnable = baker.AddBuffer<TransitionZoneEnableObject>();

            foreach(var obj in ObjectsToEnable)
            {
                toEnable.Add(new TransitionZoneEnableObject
                {
                    Entity = baker.GetEntity(obj),
                });
            }

            var toDisable = baker.AddBuffer<TransitionZoneDisableObject>();

            foreach(var obj in ObjectsToDisable)
            {
                toDisable.Add(new TransitionZoneDisableObject
                {
                    Entity = baker.GetEntity(obj)
                });
            }

            if(string.IsNullOrEmpty(StartTransitionMessage.ID) == false)
            {
                baker.AddComponent(new TransitionStartMessage { Message = StartTransitionMessage });
            }
            
            if(string.IsNullOrEmpty(FinishTransitionMessage.ID) == false)
            {
                baker.AddComponent(new TransitionFinishedMessage { Message = FinishTransitionMessage });
            }
        }
    }
}
