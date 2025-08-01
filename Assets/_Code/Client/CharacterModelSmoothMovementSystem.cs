using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(AnimationCommandBufferSystem))]
    public partial class CharacterModelSmoothMovementSystem : GameSystemBase
    {
        private TimeSystem timeSystem;
        bool isNetworkedGame = false;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            timeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            isNetworkedGame = World.GetExistingSystemManaged<TzarGames.MultiplayerKit.Client.ClientSystem>() != null;
        }

        protected override void OnSystemUpdate()
        {
            if(isNetworkedGame)
            {
                updateNetworked();
            }
            else
            {
                updateLocal();
            }
        }

        private void updateLocal()
        {
            Entities
                .WithAll<PlayerController>()
                .ForEach((Entity entity, in CharacterAnimation characterAnimation) =>
            {
                if (characterAnimation.AnimatorEntity == Entity.Null || SystemAPI.HasComponent<LocalTransform>(characterAnimation.AnimatorEntity) == false)
                {
                    return;
                }

                var transform = SystemAPI.GetComponent<LocalTransform>(entity);
                SystemAPI.SetComponent(characterAnimation.AnimatorEntity, transform);

            }).Schedule();
        }

        private void updateNetworked()
        {
            var currentTime = timeSystem.GameTime;
            var transformLookup = GetComponentLookup<LocalTransform>(true);

            // ??? ??????????? ???????? NPC ? ?????? (?????? ????????? ???????? ?? ????????, "?????????")
            // ? ???????? ????????, ????????? ???????? ?????? ??????? ?? ??????? - ??? ????, ????? ???????? ????????? ???????? ?????????? ??????? ? ??? ????? ???????????????? ??????????? ??? ?????????????
            Entities
                .ForEach((Entity entity, in CharacterAnimation animation, in SmoothTranslation smoothTranslation) =>
                {
                    if (animation.AnimatorEntity == Entity.Null)
                    {
                        return;
                    }

                    var transform = transformLookup[entity];
                    var smoothPos = smoothTranslation.Value;
                    var m = float4x4.TRS(transform.Position, transform.Rotation, new float3(1));
                    m = math.inverse(m);
                    var newLocalPos = math.mul(m, new float4(smoothPos.x, smoothPos.y, smoothPos.z, 1));

                    SystemAPI.SetComponent(animation.AnimatorEntity, LocalTransform.FromPosition(new float3(newLocalPos.x, newLocalPos.y, newLocalPos.z)));

                }).Schedule();

            // ??? ??????????? ???????? ?????? ??????, ??? ??????????? ? ???????? ?? ???????
            Entities.ForEach((Entity entity, ref PlayerSmoothCorrection smoothCorrection, in CharacterAnimation characterAnimation) =>
            {
                if (characterAnimation.AnimatorEntity == Entity.Null || SystemAPI.HasComponent<LocalTransform>(characterAnimation.AnimatorEntity) == false)
                {
                    return;
                }

                var transform = SystemAPI.GetComponent<LocalTransform>(entity);
                var newTransform = LocalTransform.FromRotation(transform.Rotation);


                if (smoothCorrection.ShouldCorrect == false)
                {
                    newTransform.Position = transform.Position;
                    SystemAPI.SetComponent(characterAnimation.AnimatorEntity, newTransform);
                    return;
                }

                var currentTranslation = SystemAPI.GetComponent<LocalTransform>(characterAnimation.AnimatorEntity).Position;
                var alpha = (float)(currentTime - smoothCorrection.CorrectionStartTime) / smoothCorrection.CorrectionTime;
                var smoothPos = math.lerp(currentTranslation, transform.Position, math.saturate(alpha));

                newTransform.Position = smoothPos;
                SystemAPI.SetComponent(characterAnimation.AnimatorEntity, newTransform);

                if (alpha >= 1.0f)
                {
                    smoothCorrection.ShouldCorrect = false;
                }

            }).Schedule();
        }
    }
}
