using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MultiplayerKit;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(CharacterControlSystem))]
    [UpdateBefore(typeof(AbilitySystem))]
    public partial class PlayerCharacterTargetSystem : GameSystemBase
    {
        EntityQuery targetsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            targetsQuery = GetEntityQuery(ComponentType.ReadOnly<Group>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<Radius>());
        }

        protected override void OnSystemUpdate()
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            Entities
                .WithReadOnly(physicsWorld)
                .ForEach((
                    Entity myEntity, 
                    ref Target myTarget, 
                    in CharacterInputs myInputs, 
                    in PlayerController myPC, 
                    in LocalTransform muTransform,
                    in Height myHeight, 
                    in Group myGroup, 
                    in TargetDetection myTargetDetection
                    ) =>
            {
                if(SystemAPI.HasComponent<NetworkPlayer>(myPC.Value) == false)
                {
                    return;
                }

                var netPlayer = SystemAPI.GetComponent<NetworkPlayer>(myPC.Value);

                if(netPlayer.ItsMe == false)
                {
                    return;
                }
                
                var myDir = myInputs.MoveVector;

                if (myDir.x == 0.0f && myDir.z == 0.0f)
                {
                    var myTransform = SystemAPI.GetComponent<LocalTransform>(myEntity);
                    myDir = math.forward(myTransform.Rotation);
                }

                myDir.y = 0;
                
                var myPos = muTransform.Position;
                var hits = new NativeList<DistanceHit>(16, Allocator.Temp);

                if(physicsWorld.OverlapSphere(myPos + math.up() * (myHeight.Value * 0.5f), myTargetDetection.DetectionRadius, ref hits, myTargetDetection.CollisionFilter, QueryInteraction.IgnoreTriggers) == false)
                {
                    myTarget.Value = Entity.Null;
                    hits.Dispose();
                    return;
                }

                var maxDP = float.MinValue;
                var nextTarget = Entity.Null;

                foreach (var hit in hits)
                {
                    if (hit.Entity == myEntity)
                    {
                        continue;
                    }
                    if(SystemAPI.HasComponent<LivingState>(hit.Entity) == false)
                    {
                        continue;
                    }

                    if(SystemAPI.HasComponent<Group>(hit.Entity) == false)
                    {
                        continue;
                    }
                    if(SystemAPI.HasComponent<Radius>(hit.Entity) == false)
                    {
                        continue;
                    }
                    if (SystemAPI.HasComponent<LocalTransform>(hit.Entity) == false)
                    {
                        continue;
                    }

                    var targetGroup = SystemAPI.GetComponent<Group>(hit.Entity);

                    if (targetGroup.Equals(myGroup))
                    {
                        continue;
                    }

                    var targetLivingState = SystemAPI.GetComponent<LivingState>(hit.Entity);

                    if(targetLivingState.IsAlive == false)
                    {
                        continue;
                    }

                    var targetPosition = SystemAPI.GetComponent<LocalTransform>(hit.Entity);

                    var dirToTarget = targetPosition.Position - myPos;

                    dirToTarget = math.normalize(dirToTarget);

                    var dp = math.dot(dirToTarget, myDir);

                    if(dp > maxDP)
                    {
                        maxDP = dp;
                        nextTarget = hit.Entity;

                        if (nextTarget == myTarget.Value)
                        {
                            break;
                        }
                    }
                }
                
                myTarget.Value = nextTarget;

                hits.Dispose();
                
            }).Run();
        }
    }
}
