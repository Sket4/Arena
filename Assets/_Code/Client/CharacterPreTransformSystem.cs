using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena.Client
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CharacterPreTransformSystem : SystemBase
    {
        private ComponentLookup<LocalTransform> localTransformLookup;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            localTransformLookup = GetComponentLookup<LocalTransform>(false);
        }

        protected override void OnUpdate()
        {
            var time = World.Time;
            localTransformLookup.Update(this);
            var ltLookup = localTransformLookup;
            
            Entities.ForEach((ref EyeballRuntimeData eyeData, in Eyeball eyeball) =>
            {
                var lt1Ref = ltLookup.GetRefRW(eyeball.TargetEye1);
                var lt2Ref = ltLookup.GetRefRW(eyeball.TargetEye2);
                
                if (time.ElapsedTime - eyeData.LastSwitchTime >= eyeData.NextSwitchTime)
                {
                    var random = Random.CreateFromIndex((uint)time.ElapsedTime);
                    
                    eyeData.LastSwitchTime = time.ElapsedTime;
                    eyeData.NextSwitchTime = random.NextFloat(eyeball.MinSwitchTime, eyeball.MaxSwitchTime);

                    var newPitch = random.NextFloat(-eyeball.MaxPitchAngle, eyeball.MaxPitchAngle);
                    var newYaw = random.NextFloat(-eyeball.MaxYawAngle, eyeball.MaxYawAngle);
                
                    var newPitchRot = quaternion.AxisAngle(eyeball.TargetEye1_DefaultTransform.Right(), newPitch);
                    var newYawRot = quaternion.AxisAngle(eyeball.TargetEye1_DefaultTransform.Up(), newYaw);

                    var newRot = math.mul(newPitchRot, newYawRot);

                    if (lt1Ref.IsValid)
                    {
                        eyeData.TargetRotation1 = math.mul(eyeball.TargetEye1_DefaultTransform.Rotation, newRot);
                    }
                    if (lt2Ref.IsValid)
                    {
                        eyeData.TargetRotation2 = math.mul(eyeball.TargetEye2_DefaultTransform.Rotation, newRot);
                    }
                }

                var delta = time.DeltaTime * eyeball.SwitchSpeed;

                if (lt1Ref.IsValid)
                {
                    lt1Ref.ValueRW.Rotation = math.slerp(lt1Ref.ValueRO.Rotation, eyeData.TargetRotation1, delta);
                }
                if (lt2Ref.IsValid)
                {
                    lt2Ref.ValueRW.Rotation = math.slerp(lt2Ref.ValueRO.Rotation, eyeData.TargetRotation2, delta);
                }
                
            }).Schedule();
        }
    }    
}

