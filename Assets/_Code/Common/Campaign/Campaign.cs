using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena.CampaignTools
{
    public enum SceneNodeTypes
    {
        Start,
        Normal,
        End
    }
    
    [Serializable]
    public class CampaignNode
    {
        public string Guid;
        public List<GameSceneKey> GameSceneKeys = new List<GameSceneKey>();
        public Vector2 Position;
        public SceneNodeTypes Type;
    }

    [Serializable]
    public class CampaignConnection
    {
        public string InputNodeGuid;
        public string OutputNodeGuid;
    }
    
    [CreateAssetMenu(menuName = "Arena/Граф сцен", fileName = "scene graph")]
    public class Campaign : ScriptableObject
    {
        public List<CampaignNode> Nodes;
        public List<CampaignConnection> Connections;
    }
}
