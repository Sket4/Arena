using Arena.Maze;
using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Arena
{
    struct NavMeshShapeProcessedFlag : IComponentData
    {
    }

    struct NavMeshShapeElement : IBufferElementData
    {
        public Entity Entity;
    }

    public struct GeneratedNavMeshData : IComponentData
    {
        public NavMeshDataInstance Instance;
    }

    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class NavMeshGenSystem : GameSystemBase
    {
        protected override void OnDestroy()
        {
            base.OnDestroy();

            Entities
                .WithoutBurst()
                .ForEach((in GeneratedNavMeshData navMeshData) =>
                {
                    Debug.Log($"Removing nav mesh data {navMeshData.Instance}");
                    NavMesh.RemoveNavMeshData(navMeshData.Instance);

                }).Run();
        }

        protected override void OnSystemUpdate()
        {
            var commands = CreateEntityCommandBufferParallel();
            List<Entity> navMeshShapeList = null;

            Entities
                .WithoutBurst()
                .WithNone<NavMeshShapeProcessedFlag>()
                .ForEach((Entity entity, NavMeshShape navMeshShape) =>
                {
                    commands.AddComponent<NavMeshShapeProcessedFlag>(0, entity);

                    if(navMeshShapeList == null)
                    {
                        navMeshShapeList = new List<Entity>();
                    }
                    navMeshShapeList.Add(entity);

                }).Run();

            if(navMeshShapeList != null)
            {
                Debug.Log("Creating nav mesh gen request...");
                var requestEntity = commands.CreateEntity(0);
                var elems = commands.AddBuffer<NavMeshShapeElement>(0, requestEntity);
                foreach(var shapeEntity in navMeshShapeList)
                {
                    elems.Add(new NavMeshShapeElement { Entity = shapeEntity });
                }
            }

            var maze = SystemAPI.GetSingleton<Maze.Maze>();
            var cellSize = SystemAPI.GetComponentRO<MazeWorldBuilder>(maze.Builder).ValueRO.CellSize;
            var worldSize = maze.CalculateWorldSize(cellSize);
            var bounds = new Bounds(Vector3.zero, new Vector3(worldSize.x, 100, worldSize.y));
            var localTransformLookup = GetComponentLookup<LocalTransform>(true);
            var parentLookup = GetComponentLookup<Parent>(true);
            var postTransformLookup = GetComponentLookup<PostTransformMatrix>(true);

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithReadOnly(localTransformLookup)
                .WithReadOnly(parentLookup)
                .WithReadOnly(postTransformLookup)
                .ForEach((Entity entity, DynamicBuffer<NavMeshShapeElement> shapes) =>
            {
                bool allReady = true;

                foreach(var shapeElem in shapes)
                {
                    var shape = SystemAPI.GetComponent<NavMeshShape>(shapeElem.Entity);

                    if(shape.Shape != NavMeshShapeType.Mesh)
                    {
                        continue;
                    }

                    if(shape.MeshReference.LoadingStatus != ObjectLoadingStatus.Completed)
                    {
                        allReady = false;
                    }

                    if(shape.MeshReference.LoadingStatus == ObjectLoadingStatus.None)
                    {
                        shape.MeshReference.LoadAsync();
                    }
                }

                if(allReady == false)
                {
                    return;
                }

                commands.DestroyEntity(0, entity);

                var buildSources = new List<NavMeshBuildSource>();

                foreach(var shapeElem in shapes)
                {
                    var shape = SystemAPI.GetComponent<NavMeshShape>(shapeElem.Entity);

                    float4x4 transform;

                    if(parentLookup.HasComponent(shapeElem.Entity))
                    {
                        TransformHelpers.ComputeWorldTransformMatrix(in shapeElem.Entity, out transform, ref localTransformLookup, ref parentLookup, ref postTransformLookup);
                    }
                    else
                    {
                        transform = SystemAPI.GetComponent<LocalTransform>(shapeElem.Entity).ToMatrix();
                        if(postTransformLookup.TryGetComponent(shapeElem.Entity, out PostTransformMatrix ptm))
                        {
                            transform = float4x4.TRS(transform.Translation(), transform.Rotation(), ptm.Value.Scale());
                        }
                    }

                    //Debug.DrawRay(transform.Translation(), Vector3.down, Color.yellow, 1000);
                    //Debug.DrawRay(transform.Translation(), shape.Offset, Color.blue, 1000);

                    var capsuleAdditionalRotation = quaternion.Euler(math.radians(90), 0, 0);

                    var currentRot = transform.Rotation();
                    var currentScale = transform.Scale();
                    var tmpTransform = float4x4.TRS(default, currentRot, currentScale);

                    var navMeshBuildShapeType = NavMeshBuildSourceShape.Box;
                    
                    currentRot = math.mul(currentRot, shape.Orientation);

                    if(/*shape.Shape == NavMeshShapeType.Box || */shape.Shape == NavMeshShapeType.Capsule)
                    {
                        var transformedOffset = tmpTransform.TransformPoint(shape.Offset);

                        if (shape.Shape == NavMeshShapeType.Capsule)
                        {
                            currentRot = math.mul(currentRot, capsuleAdditionalRotation);
                        }

                        transform = float4x4.TRS(transform.Translation() + transformedOffset, currentRot, currentScale);
                    }
                    
                    switch (shape.Shape)
                    {
                        case NavMeshShapeType.Box:
                            //Debug.DrawLine(transform.Translation() - shape.Size * 0.5f, transform.Translation() + shape.Size * 0.5f, Color.red, 9999);
                            navMeshBuildShapeType = NavMeshBuildSourceShape.Box;
                            break;
                        case NavMeshShapeType.Capsule:
                            //var pos = transform.Translation();
                            //Debug.Log($"Capsule {shapeElem.Entity.Index}:{shapeElem.Entity.Version}, pos {pos}");
                            //Debug.DrawLine(pos - shape.Size * 0.5f, pos + shape.Size * 0.5f, Color.blue, 9999);
                            navMeshBuildShapeType = NavMeshBuildSourceShape.Capsule;
                            break;
                        case NavMeshShapeType.Mesh:
                            //Debug.DrawLine(transform.Translation() - shape.Size * 0.5f, transform.Translation() + shape.Size * 0.5f, Color.yellow, 9999);
                            navMeshBuildShapeType = NavMeshBuildSourceShape.Mesh;
                            break;
                    }

                    //Debug.DrawRay(transform.Translation(), Vector3.up * 0.2f, Color.red, 1000);
                    Object sourceObject = null;

                    if (shape.Shape == NavMeshShapeType.Mesh && shape.MeshReference.IsReferenceValid)
                    {
                        sourceObject = shape.MeshReference.Result;
                    }

                    buildSources.Add(new NavMeshBuildSource
                    {
                        area = shape.NavigationArea,
                        component = null,
                        generateLinks = false,
                        shape = navMeshBuildShapeType,
                        size = shape.Size,
                        sourceObject = sourceObject,
                        transform = transform,
                    });
                }

                //UnityEngine.AI.NavMeshBuilder.CollectSources(bounds, ~0, UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes, 0, markups, buildSources);

                Debug.Log($"Build sources count: {buildSources.Count}");

                var buildSettings = NavMesh.GetSettingsByIndex(0);

                var navData = NavMeshBuilder.BuildNavMeshData(buildSettings, buildSources, bounds, Vector3.zero, Quaternion.identity);
                var navDataHandle = NavMesh.AddNavMeshData(navData);
                
                var navMeshEntity = EntityManager.CreateEntity(typeof(GeneratedNavMeshData));
                EntityManager.SetComponentData(navMeshEntity, new GeneratedNavMeshData
                {
                    Instance = navDataHandle,
                });

            }).Run();
        }
    }
}
