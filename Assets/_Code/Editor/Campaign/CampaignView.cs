using System;
using System.Collections.Generic;
using System.Linq;
using TzarGames.GameCore;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arena.CampaignTools.Editor
{
    public class CampaignView : GraphView
    {
        public static readonly Color StartNodeColor = new Color(0,0.5f,0.25f,1);
        public static readonly Color NormalNodeColor = new Color(0,0.375f,0.375f,1);
        public static readonly Color EndNodeColor = new Color(0,0.25f,0.5f,1);
        
        private readonly Vector2 defaultNodeSize = new Vector2(200, 150);

        public CampaignView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("CampaignStylesheet"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public void ClearAll()
        {
            var edgesToRemove = edges.ToList();
            edgesToRemove.ForEach(RemoveElement);
            
            var nodesToRemove = nodes.ToList();
            nodesToRemove.ForEach(RemoveElement);
        }

        public static Color GetColorForType(SceneNodeTypes sceneNodeTypes)
        {
            switch (sceneNodeTypes)
            {
                case SceneNodeTypes.Start:
                    return StartNodeColor;
                case SceneNodeTypes.Normal:
                    return NormalNodeColor;
                case SceneNodeTypes.End:
                    return EndNodeColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneNodeTypes), sceneNodeTypes, null);
            }
        }

        public void BuildFromGraph(Campaign graph)
        {
            ClearAll();

            if (graph == null)
            {
                return;
            }

            if(graph.Nodes == null)
            {
                graph.Nodes = new List<CampaignNode>();
                EditorUtility.SetDirty(graph);
            }

            var graphNodes = graph.Nodes;
            
            var nodesToEditorNodes = new Dictionary<CampaignNode, EditorCampaignNode>();
            
            // create nodes
            foreach (var node in graphNodes)
            {
                var firstSceneKey = node.GameSceneKeys?.FirstOrDefault(x => x != null);
                var editorNode = CreateSceneNode(node.Type, false, node.Guid);
                
                foreach (var sceneKey in node.GameSceneKeys)
                {
                    AddSceneKeyField(editorNode, sceneKey);
                }
                editorNode.RefreshExpandedState();
                editorNode.RefreshPorts();
                
                editorNode.SetPosition(new Rect(node.Position, defaultNodeSize));
                
                nodesToEditorNodes.Add(node, editorNode);
            }

            if(graph.Connections == null)
            {
                graph.Connections = new List<CampaignConnection>();
                EditorUtility.SetDirty(graph);
            }
            
            // connect nodes
            foreach (var connection in graph.Connections)
            {
                var inputNode = graph.Nodes.FirstOrDefault(x => x.Guid == connection.InputNodeGuid);
                var outputNode = graph.Nodes.FirstOrDefault(x => x.Guid == connection.OutputNodeGuid);

                var inputEditorNode = nodesToEditorNodes[inputNode];
                var outputEditorNode = nodesToEditorNodes[outputNode];

                var inputPort = inputEditorNode.inputContainer.Q<Port>();

                if (inputPort == null)
                {
                    throw new InvalidOperationException($"Failed to find input port from node {inputEditorNode.Guid}");
                }
                
                var outputPort = outputEditorNode.outputContainer.Q<Port>();
                
                if (outputPort == null)
                {
                    throw new InvalidOperationException($"Failed to find output port from node {outputEditorNode.Guid}");
                }
                
                LinkNodes(inputPort, outputPort);
            }

            // refresh nodes
            foreach (var node in nodes.ToList())
            {
                node.RefreshExpandedState();
                node.RefreshPorts();
            }
        }

        private void LinkNodes(Port input, Port output)
        {
            var edge = new Edge
            {
                input = input,
                output = output
            };
            
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            
            Add(edge);
        }

        private Port GeneratePort(EditorCampaignNode editorCampaignNode,
            UnityEditor.Experimental.GraphView.Direction direction,
            Port.Capacity capacity = Port.Capacity.Single)
        {
            return editorCampaignNode.InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(float));
        }

        public static string GetNodeNameFromType(SceneNodeTypes type)
        {
            switch (type)
            {
                case SceneNodeTypes.Start:
                    return "Начальный узел сцен";
                case SceneNodeTypes.Normal:
                    return "Узел сцен";
                case SceneNodeTypes.End:
                    return "Конечный узел сцен";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        public EditorCampaignNode CreateSceneNode(SceneNodeTypes type, bool addScenekeyField, string guid = default)
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
            }
            
            var node = new EditorCampaignNode
            {
                Guid = guid,
                title = GetNodeNameFromType(type),
                Types = type,
            };

            node.titleContainer.style.minHeight = 30;
            node.titleContainer.style.backgroundColor = GetColorForType(type);
            //node.outputContainer.style.minHeight = 30;
            //node.inputContainer.style.minHeight = 30;

            if (type == SceneNodeTypes.End || type == SceneNodeTypes.Normal)
            {
                var inputPort = GeneratePort(node, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi);
                inputPort.portName = "";
                node.inputContainer.Add(inputPort);    
            }

            if (type == SceneNodeTypes.Start || type == SceneNodeTypes.Normal)
            {
                var outputPort = GeneratePort(node, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi);
                outputPort.portName = "";
                node.outputContainer.Add(outputPort);    
            }
            
            // create scene key field button
            var createScenekeyButton = new Button(() =>
            {
                AddSceneKeyField(node);
            });
            createScenekeyButton.text = "+";
            createScenekeyButton.style.minHeight = 25;
            node.titleContainer.Add(createScenekeyButton);
            
            node.SetPosition(new Rect(defaultNodeSize * 2, defaultNodeSize));
            
            AddElement(node);

            if (addScenekeyField)
            {
                AddSceneKeyField(node);
            }

            node.RefreshExpandedState();
            node.RefreshPorts();
            
            return node;
        }

        public void AddSceneKeyField(EditorCampaignNode node, GameSceneKey sceneKey = null)
        {
            var fieldContainer = new VisualElement();
            fieldContainer.contentContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            fieldContainer.contentContainer.style.flexWrap = new StyleEnum<Wrap>(Wrap.Wrap);
            fieldContainer.contentContainer.style.flexGrow = new StyleFloat(1);
            fieldContainer.contentContainer.style.maxHeight = 25;
            node.extensionContainer.Add(fieldContainer);
            
            // object ref
            var sceneRef = new ObjectField();
            sceneRef.objectType = typeof(GameSceneKey);
            sceneRef.value = sceneKey;
            node.SceneKeyFields.Add(sceneRef);
            
            // remove button
            var removeButton = new Button(() =>
            {
                node.extensionContainer.Remove(fieldContainer);
            });
            removeButton.text = "X";
            removeButton.style.minWidth = 25;
            removeButton.style.maxWidth = 25;
            
            fieldContainer.Add(removeButton);
            fieldContainer.contentContainer.Add(sceneRef);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port 
                    && startPort.node != port.node
                    && startPort.direction != port.direction
                    )
                {
                    compatiblePorts.Add(port);
                }
            });
            
            return compatiblePorts;
        }

        public void SaveToAsset(Campaign graphAsset)
        {
            graphAsset.Nodes.Clear();
            graphAsset.Connections.Clear();
            
            var editorSceneNodes = nodes.ToList().Cast<EditorCampaignNode>().ToList();
            var editorNodesToSceneNodes = new Dictionary<EditorCampaignNode, CampaignNode>();

            foreach (var editorNode in editorSceneNodes)
            {
                var newNode = new CampaignNode
                {
                    Position = editorNode.GetPosition().position, 
                    Type = editorNode.Types, 
                    Guid = editorNode.Guid
                };

                foreach (var sceneKeyField in editorNode.SceneKeyFields)
                {
                    newNode.GameSceneKeys.Add(sceneKeyField.value as GameSceneKey);
                }
                
                graphAsset.Nodes.Add(newNode);
                editorNodesToSceneNodes.Add(editorNode, newNode);
            }

            var nodeEdges = edges.ToList();

            foreach (var connection in nodeEdges)
            {
                var inputNode = connection.input.node as EditorCampaignNode;
                var outputNode = connection.output.node as EditorCampaignNode;

                graphAsset.Connections.Add(new CampaignConnection
                {
                    InputNodeGuid = inputNode.Guid,
                    OutputNodeGuid = outputNode.Guid
                });
            }
        }
    }    
}

