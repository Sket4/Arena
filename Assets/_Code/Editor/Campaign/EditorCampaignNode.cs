using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

namespace Arena.CampaignTools.Editor
{
    public class EditorCampaignNode : Node
    {
        public string Guid;
        public List<ObjectField> SceneKeyFields = new List<ObjectField>();
        public SceneNodeTypes Types;
    }
}
