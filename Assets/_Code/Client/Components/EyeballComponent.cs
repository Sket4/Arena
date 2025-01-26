using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Arena.Client
{
    [Serializable]
    public struct Eyeball : IComponentData
    {
        public Entity TargetEye1;
        public LocalTransform TargetEye1_DefaultTransform;
        public Entity TargetEye2;
        public LocalTransform TargetEye2_DefaultTransform;
        
        public float MaxPitchAngle;
        public float MaxYawAngle;
        public float MinSwitchTime;
        public float MaxSwitchTime;
        public float SwitchSpeed;
    }
    
    [Serializable]
    public struct EyeballRuntimeData : IComponentData
    {
        public double LastSwitchTime;
        public float NextSwitchTime;

        public quaternion TargetRotation1;
        public quaternion TargetRotation2;
    }
    
    [UseDefaultInspector]
    public class EyeballComponent : ComponentDataBehaviour<Eyeball>
    {
        public Transform TargetEye1;
        public Transform TargetEye2;

        public float SwitchSpeed = 10;
        public float MinSwitchTime = 1;
        public float MaxSwitchTime = 3;
        public float MaxPitchAngle = 5;
        public float MaxYawAngle = 5;

        protected override void Bake<K>(ref Eyeball serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            serializedData.TargetEye1 = baker.GetEntity(TargetEye1);
            serializedData.TargetEye2 = baker.GetEntity(TargetEye2);
            
            serializedData.MaxYawAngle = math.TORADIANS * MaxYawAngle;
            serializedData.MaxPitchAngle = math.TORADIANS * MaxPitchAngle;
            serializedData.MinSwitchTime = MinSwitchTime;
            serializedData.MaxSwitchTime = MaxSwitchTime;
            serializedData.SwitchSpeed = SwitchSpeed;
            
            baker.AddComponent<EyeballRuntimeData>();
        }
    }
    
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class EyeballBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ltLookup = GetComponentLookup<LocalTransform>(true);
            
            Entities
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((ref Eyeball eb, ref EyeballRuntimeData ebData) =>
                {
                    if (eb.TargetEye1 != Entity.Null && ltLookup.TryGetComponent(eb.TargetEye1, out var lt1))
                    {
                        eb.TargetEye1_DefaultTransform = lt1;
                    }
                    if (eb.TargetEye2 != Entity.Null && ltLookup.TryGetComponent(eb.TargetEye2, out var lt2))
                    {
                        eb.TargetEye2_DefaultTransform = lt2;
                    }

                    ebData.NextSwitchTime = Random.CreateFromIndex(123).NextFloat(eb.MinSwitchTime, eb.MaxSwitchTime);
                    ebData.TargetRotation1 = eb.TargetEye1_DefaultTransform.Rotation;
                    ebData.TargetRotation2 = eb.TargetEye2_DefaultTransform.Rotation;

                }).Run();
        }
    }
}
