using System;
using System.Collections.Generic;
using System.Linq;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.CampaignTools
{
    [Serializable]
    public struct CampaignData : IComponentData
    {
        public Entity StartNode;
        public Entity EndNode;
    }
    
    [Serializable]
    public struct GameSceneNodeEntityArray : IBufferElementData
    {
        public Entity Value;
    }

    [Serializable]
    public struct GameSceneNodeEntityReference : IComponentData
    {
        public Entity Value;
    }

    [Serializable]
    public struct GameSceneNodeType : IComponentData
    {
        public SceneNodeTypes Value;
    }

    [Serializable]
    public struct GameSceneAssetReference : IBufferElementData
    {
        public Entity Value;
    }

    [Serializable]
    public struct GameSceneNodeConnection : IBufferElementData
    {
        public Entity Value;
    }

    [Serializable]
    public struct CampaignEntityReference : IComponentData
    {
        public Entity Value;
    }

    [TemporaryBakingType]
    struct CampaignBakingGameSceneAssets : IBufferElementData
    {
        public Entity GameSceneEntity;
    }

    public class CampaignComponent : ObjectKeyGenericComponent<CampaignKey>
    {
        public Campaign Campaign;

        protected override void Bake<T>(T baker)
        {
#if UNITY_EDITOR
            base.Bake(baker);

            if (Campaign == null || ID == null)
            {
                return;
            }

            baker.AddComponent(new PrefabID { Value = ID.Id });

            var sceneNodeList = new List<GameSceneNodeEntityArray>();
            Entity startNode = Entity.Null;
            Entity endNode = Entity.Null;
            var graphNodeToEntity = new Dictionary<CampaignNode, Entity>();

            // nodes
            foreach (var graphNode in Campaign.Nodes)
            {
                var nodeEntity = baker.CreateAdditionalEntity(TransformUsageFlags.None);
                //baker.AddComponent(nodeEntity, new Prefab());
                baker.AddComponent(nodeEntity, new CampaignEntityReference { Value = baker.GetEntity() });
                sceneNodeList.Add(new GameSceneNodeEntityArray { Value = nodeEntity });
                graphNodeToEntity.Add(graphNode, nodeEntity);

                baker.AddComponent(nodeEntity, new GameSceneNodeType { Value = graphNode.Type });
                var sceneAssets = baker.AddBuffer<GameSceneAssetReference>(nodeEntity);

                foreach (var sceneKey in graphNode.GameSceneKeys)
                {
                    var assetEntity = baker.ConvertObjectKey(sceneKey);
                    sceneAssets.Add(new GameSceneAssetReference { Value = assetEntity });
                }

                if (graphNode.Type == SceneNodeTypes.Start)
                {
                    startNode = nodeEntity;
                }
                else if (graphNode.Type == SceneNodeTypes.End)
                {
                    endNode = nodeEntity;
                }

                //baker.SetName(nodeEntity, $"{Campaign.name} node {graphNode.Guid}");
            }

            var sceneNodes = baker.AddBuffer<GameSceneNodeEntityArray>();
            foreach (var nodeEntity in sceneNodeList)
            {
                sceneNodes.Add(nodeEntity);
            }

            var graphData = new CampaignData
            {
                StartNode = startNode,
                EndNode = endNode
            };
            baker.AddComponent(graphData);

            // connections
            foreach (var graphNode in Campaign.Nodes)
            {
                var nodeEntity = graphNodeToEntity[graphNode];

                var connections = baker.AddBuffer<GameSceneNodeConnection>(nodeEntity);

                var nodeOutputs
                    = Campaign.Connections.Where(x => x.OutputNodeGuid == graphNode.Guid).ToList();

                foreach (var connection in nodeOutputs)
                {
                    var otherNode = Campaign.Nodes.FirstOrDefault(x => x.Guid == connection.InputNodeGuid);
                    var otherNodeEntity = graphNodeToEntity[otherNode];

                    connections.Add(new GameSceneNodeConnection { Value = otherNodeEntity });
                }
            }
#endif
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class CampaignBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            using (var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp))
            {
                Entities
                    .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
                    .ForEach((Entity entity, DynamicBuffer<GameSceneAssetReference> gameSceneRefs) =>
                {
                    foreach (var gameSceneRef in gameSceneRefs)
                    {
                        ecb.AddComponent(gameSceneRef.Value, new GameSceneNodeEntityReference { Value = entity });
                    }

                }).Run();

                ecb.Playback(EntityManager);
            }
        }
    }

}
