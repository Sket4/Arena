using System;
using TzarGames.GameCore.ScriptViz;
using UnityEngine;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Entities;

namespace Arena.ScriptViz
{
    [Serializable]
    public struct DeadEventData : IBufferElementData, ICommandAddressData
    {
        [SerializeField] private Address commandAddress;
        public Address CommandAddress { get => commandAddress; set => commandAddress = value; }
    }
    
    [System.Serializable]
    public class CharacterDeathEventNode : DynamicBufferEventNode<DeadEventData>
    {
    }
}
