using System;
using System.Linq;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace Arena
{
    [Serializable]
    public sealed class SplineContainerData : IComponentData
    {
        public string Name;
        public Spline[] Splines;
    }

    public sealed class SplineContainerReference : ICleanupComponentData
    {
        public SplineContainer Value;
        public float CachedLength;
    }
    
    public class SplineContainerBaker : Baker<SplineContainer>
    {
        public override void Bake(SplineContainer authoring)
        {
            var cmp = new SplineContainerData
            {
                Name = authoring.name,
                Splines = authoring.Splines.ToArray()
            };
            AddComponentObject(GetEntity(TransformUsageFlags.WorldSpace), cmp);
        }
    }

    public partial class SplineContainerUnbakeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<SplineContainerReference>()
                .ForEach((Entity entity, in SplineContainerData data, in LocalToWorld l2w) =>
                {
                    var splineObject = new GameObject(data.Name);
                    splineObject.transform.position = l2w.Position;
                    splineObject.transform.rotation = l2w.Rotation;
                    
                    var container = splineObject.AddComponent<SplineContainer>();
                    container.Splines = data.Splines;
                    data.Splines = null;
                    EntityManager.AddComponentObject(entity, new SplineContainerReference
                    {
                        Value = container,
                        CachedLength = container.CalculateLength()
                    });

                }).Run();
            
            
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<SplineContainerData>()
                .ForEach((Entity entity, in SplineContainerReference reference) =>
                {
                    if (reference.Value)
                    {
                        Object.Destroy(reference.Value.gameObject);
                    }
                    EntityManager.RemoveComponent<SplineContainerReference>(entity);

                }).Run();
        }
    }
}
