using System.Collections.Generic;
using System.Reflection;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Arena
{
    public enum NavMeshShapeType : byte
    {
        Mesh,
        Box,
        Capsule,
    }

    public enum ShapeSource : byte
    {
        Renderer,
        Collider,
    }

    [System.Serializable]
    public struct NavMeshShape : IComponentData
    {
        public NavMeshShapeType Shape;
        public WeakObjectReference<Mesh> MeshReference;
        public float3 Size;
        public float3 Offset;
        public quaternion Orientation;
        public int NavigationArea;
    }


    struct RendererBakeInfo
    {
        public Entity Entity;
        public Component Component;
    }

    [TemporaryBakingType]
    class TempBakingData : IComponentData
    {
        public NavMeshShapeComponent Component;
        public List<RendererBakeInfo> ChildComponents;
    }

    [UseDefaultInspector(true)]
    public class NavMeshShapeComponent : ComponentDataBehaviourBase
    {
        public ShapeSource ShapeSource = ShapeSource.Collider;
        public bool BakeShapesForChilds = false;
        public ShapeSource ChildShapeSource = ShapeSource.Collider;
        //static readonly FieldInfo meshField = (typeof(PhysicsShapeAuthoring)).GetField("m_CustomMesh", BindingFlags.Instance | BindingFlags.NonPublic);

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            ShapeSource = ShapeSource.Collider;
        }

        protected override void PreBake<T>(T baker)
        {
            var shapeData = new NavMeshShape
            {
                Shape = NavMeshShapeType.Box,
                Size = new float3(1)
            };

            switch (ShapeSource)
            {
                case ShapeSource.Renderer:
                    shapeData.Shape = NavMeshShapeType.Mesh;
                    var renderer = baker.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        SetupForRenderer(renderer, ref shapeData);
                        baker.AddComponent(shapeData);
                    }
                    break;
                case ShapeSource.Collider:
                    var cldr = baker.GetComponent<Collider>();
                    if (cldr != null)
                    {
                        SetupForCollider(cldr, ref shapeData);
                        baker.AddComponent(shapeData);
                    }
                    // else
                    // {
                    //     var shapeAuth = baker.GetComponent<Unity.Physics.Authoring.PhysicsShapeAuthoring>();
                    //     if (shapeAuth != null)
                    //     {
                    //         if (SetupForPhysicsAuthoring(shapeAuth, ref shapeData))
                    //         {
                    //             baker.AddComponent(shapeData);
                    //         }
                    //     }
                    // }
                    break;
            };

            if (BakeShapesForChilds)
            {
                var bakingData = new TempBakingData
                {
                    Component = this,
                    ChildComponents = new List<RendererBakeInfo>()
                };

                if(ChildShapeSource == ShapeSource.Renderer)
                {
                    var childRenderers = GetComponentsInChildren<Renderer>(true);

                    foreach (var childRenderer in childRenderers)
                    {
                        processComponent(childRenderer, bakingData.ChildComponents, baker);
                    }
                }
                else if(ChildShapeSource == ShapeSource.Collider)
                {
                    var childColliders = GetComponentsInChildren<Collider>();

                    foreach (var childCollider in childColliders)
                    {
                        processComponent(childCollider, bakingData.ChildComponents, baker);
                    }

                    // var childShapeAuthorings = GetComponentsInChildren<Unity.Physics.Authoring.PhysicsShapeAuthoring>();
                    //
                    // foreach (var childShapeAuthoring in childShapeAuthorings)
                    // {
                    //     processComponent(childShapeAuthoring, bakingData.ChildComponents, baker);
                    // }
                }

                if (bakingData.ChildComponents.Count > 0)
                {
                    baker.AddComponentObject(bakingData);
                }
            }
        }

        void processComponent(Component component, List<RendererBakeInfo> bakeInfos, IGCBaker baker)
        {
            if(component == null
                || component is Renderer rnd && rnd.enabled == false
                || component is Collider cldr && cldr.enabled == false
                //|| component is Unity.Physics.Authoring.PhysicsShapeAuthoring phyShape && phyShape.enabled == false
            )
            {
                return;
            }
            if (component.gameObject == gameObject)
            {
                return;
            }
            if (component.GetComponent<NavMeshShapeComponent>() != null)
            {
                return;
            }
            if (component.GetComponent<IgnoreNavMeshGenerationComponent>() != null)
            {
                return;
            }

            bakeInfos.Add(new RendererBakeInfo { Entity = baker.GetEntity(component), Component = component });
        }

        public static void SetupForCollider(Collider collider, ref NavMeshShape navMeshShape)
        {
            switch(collider)
            {
                case BoxCollider box:
                    navMeshShape.Shape = NavMeshShapeType.Box;
                    var transform1 = box.transform;
                    var bounds = new Bounds(box.center + transform1.position, transform1.rotation * box.size);
                    SetupForBounds(transform1, bounds, ref navMeshShape);
                    break;

                case CapsuleCollider capsule:
                    navMeshShape.Shape = NavMeshShapeType.Capsule;
                    SetupForBounds(capsule.transform, capsule.bounds, ref navMeshShape);
                    break;
                default:
                    Debug.LogError($"Collider type of {collider.GetType().Name} is not supported");
                    break;
            }
        }

        /*
        public static bool SetupForPhysicsAuthoring(Unity.Physics.Authoring.PhysicsShapeAuthoring shapeAuthoring, ref NavMeshShape navMeshShape)
        {
            if(shapeAuthoring.CollisionResponse != Unity.Physics.CollisionResponsePolicy.Collide
                && shapeAuthoring.CollisionResponse != Unity.Physics.CollisionResponsePolicy.CollideRaiseCollisionEvents)
            {
                return false;
            }

            switch (shapeAuthoring.ShapeType)
            {
                case Unity.Physics.Authoring.ShapeType.Box:
                    {
                        navMeshShape.Shape = NavMeshShapeType.Box;
                        var props = shapeAuthoring.GetBoxProperties();
                        navMeshShape.Offset = props.Center;
                        navMeshShape.Size = props.Size;
                        navMeshShape.Orientation = props.Orientation;
                    }
                    break;
                case Unity.Physics.Authoring.ShapeType.Capsule:
                    {
                        navMeshShape.Shape = NavMeshShapeType.Capsule;
                        var props = shapeAuthoring.GetCapsuleProperties();
                        navMeshShape.Offset = props.Center;
                        navMeshShape.Size = new Vector3(props.Radius * 2, props.Height, props.Radius * 2);
                        navMeshShape.Orientation = props.Orientation;
                    }
                    break;
                case Unity.Physics.Authoring.ShapeType.Cylinder:
                    {
                        navMeshShape.Shape = NavMeshShapeType.Capsule;
                        var props = shapeAuthoring.GetCylinderProperties();
                        navMeshShape.Offset = props.Center;
                        //navMeshShape.Offset -= (float3)(Quaternion.Euler(90,0,0) * (Vector3.up * props.Radius));
                        navMeshShape.Size = new Vector3(props.Radius * 2, props.Height + props.Radius * 2, props.Radius * 2);
                        navMeshShape.Orientation = props.Orientation;
                    }
                    break;
                case ShapeType.Mesh:
                case ShapeType.ConvexHull:
                    {
                        navMeshShape.Shape = NavMeshShapeType.Mesh;
                        
                        var mesh = meshField.GetValue(shapeAuthoring) as Mesh;

                        if (mesh != null)
                        {
                            SetupForMesh(shapeAuthoring.transform, mesh.bounds, mesh, ref navMeshShape);    
                        }
                        else
                        {
                            Debug.LogError($"null custom mesh on physics shape {shapeAuthoring}");
                        }
                        
                        // navMeshShape.Offset = default;
                        // navMeshShape.MeshReference
                        // //navMeshShape.Offset -= (float3)(Quaternion.Euler(90,0,0) * (Vector3.up * props.Radius));
                        // navMeshShape.Size = new Vector3(props.Radius * 2, props.Height + props.Radius * 2, props.Radius * 2);
                        // navMeshShape.Orientation = props.Orientation;
                    }
                    break;
                default:
                    Debug.LogError($"Collider shape type {shapeAuthoring.ShapeType} is not supported");
                    return false;
            }

            return true;
        }
        */
        
        public static void SetupForRenderer(Renderer renderer, ref NavMeshShape navMeshShape)
        {
            Mesh mesh = default;
            if(renderer is MeshRenderer)
            {
                var mf = renderer.GetComponent<MeshFilter>();
                mesh = mf.sharedMesh;
            }
            else if(renderer is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
            }

            SetupForMesh(renderer.transform, renderer.bounds, mesh, ref navMeshShape);
        }

        public static void SetupForMesh(Transform transform, Bounds bounds, Mesh mesh, ref NavMeshShape navMeshShape)
        {
            if (mesh != null)
            {
                navMeshShape.MeshReference = new WeakObjectReference<Mesh>(Unity.Entities.Serialization.UntypedWeakReferenceId.CreateFromObjectInstance(mesh));
            }

            SetupForBounds(transform, bounds, ref navMeshShape);
        }

        public static void SetupForBounds(Transform tr, Bounds bounds, ref NavMeshShape navMeshShape)
        {
            var inv = math.inverse(float4x4.TRS(default, tr.rotation, tr.localScale));
            navMeshShape.Size = math.abs(inv.TransformPoint(bounds.size));
            navMeshShape.Offset = inv.TransformPoint(bounds.center - tr.position);
        }

        //private void OnDrawGizmosSelected()
        //{
        //    var mtx = float4x4.TRS(default, transform.rotation, transform.localScale);

        //    switch(Value.Shape)
        //    {
        //        case NavMeshShapeType.Mesh:
        //            break;

        //        case NavMeshShapeType.Capsule:
        //        case NavMeshShapeType.Box:
        //            Gizmos.DrawWireCube(transform.position + (Vector3)mtx.TransformPoint(Value.Offset), mtx.TransformPoint(Value.Size));
        //            break;
        //    }      
        //}
#else
        protected override void PreBake<T>(T baker) {}
#endif
    }

#if UNITY_EDITOR
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    partial class NavMeshShapeBakingSystem : SystemBase
    {
        private static readonly int nonWalkableAreaIndex = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
        
        protected override void OnUpdate()
        {
            using (var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp))
            {
                Entities
                    .WithoutBurst()
                    .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                    .ForEach((TempBakingData tempBakingData) =>
                {
                    foreach (var child in tempBakingData.ChildComponents)
                    {
                        int area = 0;
                        
                        if (child.Component.GetComponent<NotWalkableNavMeshObject>())
                        {
                            area = nonWalkableAreaIndex;
                        }
                        
                        var shape = new NavMeshShape
                        {
                            Shape = NavMeshShapeType.Box,
                            NavigationArea = area
                        };

                        if (child.Component is Renderer rndr)
                        {
                            shape.Shape = NavMeshShapeType.Mesh;
                            NavMeshShapeComponent.SetupForRenderer(rndr, ref shape);
                        }
                        else if(child.Component is Collider cldr)
                        {
                            NavMeshShapeComponent.SetupForCollider(cldr, ref shape);
                        }
                        // else if(child.Component is Unity.Physics.Authoring.PhysicsShapeAuthoring shapeAuth)
                        // {
                        //     NavMeshShapeComponent.SetupForPhysicsAuthoring(shapeAuth, ref shape);
                        // }
                        ecb.AddComponent(child.Entity, shape);
                    }

                }).Run();

                ecb.Playback(EntityManager);
            }
        }
    }
#endif
}
