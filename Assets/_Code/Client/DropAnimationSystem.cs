using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client
{
    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class DropAnimationSystem : SystemBase
    {
        TimeSystem timeSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            timeSystem = World.GetExistingSystemManaged<TimeSystem>();
        }

        protected override void OnUpdate()
        {
            var deltaTime = timeSystem.TimeDelta;

            Entities
                .ForEach((Entity entity, ref DropAnimation dropAnimation, ref LocalTransform transform) =>
            {
                if(dropAnimation.State == DropAnimationState.Finished)
                {
                    return;
                }

                if(dropAnimation.State == DropAnimationState.PendingStart)
                {
                    float3 startPos = transform.Position;

                    if(SystemAPI.HasComponent<Droppable>(dropAnimation.ItemEntity))
                    {
                        var droppable = SystemAPI.GetComponent<Droppable>(dropAnimation.ItemEntity);
                        
                        if(SystemAPI.HasComponent<DropAnimationStartPosition>(droppable.PreviousOwner))
                        {
                            var startPosEntity = SystemAPI.GetComponent<DropAnimationStartPosition>(droppable.PreviousOwner).PositionEntity;
                            var startL2W = SystemAPI.GetComponent<LocalToWorld>(startPosEntity);
                            
                            var animationL2W = SystemAPI.GetComponent<LocalToWorld>(entity);
                            var m = float4x4.TRS(animationL2W.Position, animationL2W.Rotation, new float3(1));

                            startPos = m.InverseTransformPoint(startL2W.Position);
                        }
                    }

                    dropAnimation.StartTranslation = startPos;
                    dropAnimation.EndTranslation = transform.Position;
                    dropAnimation.State = DropAnimationState.Playing;

                    //UnityEngine.Debug.DrawLine(dropAnimation.StartTranslation.Value, dropAnimation.StartTranslation.Value + math.up(), UnityEngine.Color.red, 10);
                    //UnityEngine.Debug.DrawLine(dropAnimation.EndTranslation.Value, dropAnimation.EndTranslation.Value + math.up(), UnityEngine.Color.blue, 10);
                }

                dropAnimation.PlayingTime += deltaTime;

                if(dropAnimation.Time <= dropAnimation.PlayingTime)
                {
                    dropAnimation.State = DropAnimationState.Finished;
                    dropAnimation.PlayingTime = dropAnimation.Time;
                    transform.Position = dropAnimation.EndTranslation;
                    transform.Rotation = quaternion.identity;
                    return;
                }

                var normalizedTime = dropAnimation.PlayingTime / dropAnimation.Time;
                var alpha = math.clamp(normalizedTime, 0, 1);
                var heightAlpha = alpha;
                if(heightAlpha > 0.5f)
                {
                    heightAlpha = 1.0f - heightAlpha;
                }
                heightAlpha *= 2.0f;
                heightAlpha = math.pow(heightAlpha, 0.5f);
                var height = dropAnimation.Height * heightAlpha;
                var blendPos = math.lerp(dropAnimation.StartTranslation, dropAnimation.EndTranslation, alpha);
                blendPos.y += height;
                
                transform.Position = blendPos;
                transform.Rotation = quaternion.AxisAngle(new float3(1, 0, 0), dropAnimation.PlayingTime * dropAnimation.RotationSpeed);

            }).Run();

            // TODO в определенном случае сбросить состояния для повторного проигрывания
        }
    }
}
