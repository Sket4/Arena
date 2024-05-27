using TzarGames.GameCore;
using TzarGames.GameCore.Client.Physics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Arena.Client.Physics
{
    [UpdateAfter(typeof(ApplyDamageSystem))]
    [UpdateBefore(typeof(DestroyHitSystem))]
    [DisableAutoCreation]
    public partial class RagdollAndBreakableSystem : GameSystemBase
    {
        EntityQuery deathRagdollsQuery;
        EntityQuery deadBreakablesQuery;
        EntityQuery physicsJointBodyPairsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            physicsJointBodyPairsQuery = GetEntityQuery(ComponentType.ReadOnly<PhysicsConstrainedBodyPair>());
        }

        protected unsafe override void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();

            if(deadBreakablesQuery.CalculateEntityCount() > 0)
            {
                Entities
                    .WithStoreEntityQueryInField(ref deadBreakablesQuery)
                    .WithChangeFilter<DeathData>()
                    .ForEach((DynamicBuffer<BreakableBones> boneArray, ref Breakable breakable, in DeathData deathData, in LivingState livingState) =>
                {
                    if(livingState.IsAlive)
                    {
                        return;
                    }

                    if(breakable.IsBroken)
                    {
                        return;
                    }

                    breakable.IsBroken = true;

                    var impulse = deathData.Hit.Direction * 5;
                    var rand = Unity.Mathematics.Random.CreateFromIndex((uint)deathData.Hit.Target.Index);
                    
                    Debug.Log($"Applying hit force {impulse} to breakable");

                    for (int i = 0; i < boneArray.Length; i++)
                    {
                        var bone = boneArray[i];
                        
                        #if UNITY_EDITOR
                        if (SystemAPI.HasComponent<PhysicsCollider>(bone.BoneEntity) == false)
                        {
                            Debug.LogError($"Entity {bone.BoneEntity.Index} does not have collider");
                            continue;
                        }
                        #endif
                        
                        var colliderData = SystemAPI.GetComponent<PhysicsCollider>(bone.BoneEntity);
                        var collider = colliderData.ColliderPtr;

                        if (collider == null)
                        {
                            Debug.LogError($"null collider ptr on {bone.BoneEntity.Index}");
                            continue;
                        }
                        
                        var mass = PhysicsMass.CreateDynamic(collider->MassProperties, bone.Mass);
                        commands.AddComponent(0, bone.BoneEntity, mass);

                        var vel = new PhysicsVelocity();
                        Unity.Physics.Extensions.PhysicsComponentExtensions.ApplyLinearImpulse(ref vel, mass, impulse);
                        var angularImpulse = rand.NextFloat3Direction();
                        Unity.Physics.Extensions.PhysicsComponentExtensions.ApplyAngularImpulse(ref vel, mass, angularImpulse);
                        commands.AddComponent(0, bone.BoneEntity, vel);

                        commands.AddComponent(0, bone.BoneEntity, new PhysicsDamping()
                        {
                            Linear = 0.01f,
                            Angular = 0.05f
                        });

                        var l2w = SystemAPI.GetComponent<LocalToWorld>(bone.BoneEntity);
                        commands.SetComponent(0, bone.BoneEntity, LocalTransform.FromPositionRotation(l2w.Position, l2w.Rotation));

                        commands.RemoveComponent<Parent>(0, bone.BoneEntity);
                        
                        commands.SetSharedComponentData(0, bone.BoneEntity, new PhysicsWorldIndex { Value = 0 });

                    }

                }).Run();
            }

            if(deathRagdollsQuery.CalculateEntityCount() > 0)
            {
                var ragdollBones = GetBufferLookup<RagdollBoneEntity>(true);
                var jointPairs = CreateArchetypeChunkArrayWithUpdateAllocator(physicsJointBodyPairsQuery);
                var entityType = GetEntityTypeHandle();
                var pairType = GetComponentTypeHandle<PhysicsConstrainedBodyPair>(true);

                Entities
                .WithStoreEntityQueryInField(ref deathRagdollsQuery)
                //.WithStructuralChanges()
                //.WithoutBurst()
                .WithChangeFilter<DeathData>()
                .ForEach((Entity entity, in DeathData deathData, in CharacterAnimation characterAnimation, in LivingState livingState) =>
                {
                    if (livingState.IsAlive)
                    {
                        return;
                    }

                    if (ragdollBones.HasBuffer(characterAnimation.AnimatorEntity) == false)
                    {
                        Debug.Log($"Animator entity {characterAnimation.AnimatorEntity} does not have ragdoll component");
                        return;
                    }

                    var ragdoll = SystemAPI.GetComponent<Ragdoll>(characterAnimation.AnimatorEntity);
                    if(ragdoll.IsActivated)
                    {
                        return;
                    }

                    ragdoll.IsActivated = true;
                    SystemAPI.SetComponent(characterAnimation.AnimatorEntity, ragdoll);

                    // enable physics and apply forces
                    var boneArray = ragdollBones[characterAnimation.AnimatorEntity];

                    const float linearImpulseScale = 15;
                    const float angularImpulseScale = 1;
                    var random = Random.CreateFromIndex((uint)deathData.HitEntity.Index);

                    for (int i = 0; i < boneArray.Length; i++)
                    {
                        var bone = boneArray[i];

                        if(SystemAPI.HasComponent<PhysicsVelocity>(bone.Value) == false)
                        {
                            Debug.LogError($"Bone {bone.Value.Index}:{bone.Value.Version} has no PhysicsVelocity component");
                            continue;
                        }

                        commands.AddComponent(0, bone.Value, new PhysicsGravityFactor { Value = 1 });

                        var vel = SystemAPI.GetComponent<PhysicsVelocity>(bone.Value);
                        var mass = SystemAPI.GetComponent<PhysicsMass>(bone.Value);
                        var l2w = SystemAPI.GetComponent<LocalToWorld>(bone.Value);

                        var distance = math.distance(l2w.Position, deathData.Hit.Position);
                        var distanceForceScale = 1.0f - math.saturate(distance);

                        var angularImpulse = random.NextFloat3(new float3(-1), new float3(1)) * angularImpulseScale;

                        bool isBoneDetached = bone.IsBreakable;

                        float3 linearDir;

                        if (isBoneDetached)
                        {
                            linearDir = math.normalizesafe(deathData.Hit.Direction + math.up(), deathData.Hit.Direction);
                        }
                        else
                        {
                            linearDir = deathData.Hit.Direction;
                        }
                        
                        var linearImpulse = linearDir * linearImpulseScale * distanceForceScale;
                        
                        Unity.Physics.Extensions.PhysicsComponentExtensions.ApplyLinearImpulse(ref vel, mass, linearImpulse);

                        if (isBoneDetached)
                        {
                            Unity.Physics.Extensions.PhysicsComponentExtensions.ApplyAngularImpulse(ref vel, mass, angularImpulse);    
                        }
                        
                        commands.SetComponent(0, bone.Value, vel);

                        commands.SetComponent(0, bone.Value, LocalTransform.FromPositionRotation(l2w.Position, l2w.Rotation));

                        commands.RemoveComponent<Parent>(0, bone.Value);
                        commands.AddSharedComponentData(0, bone.Value, new PhysicsWorldIndex { Value = 0 });

                        //Debug.DrawLine(l2w.Position, l2w.Position + math.up() * 0.1f, Color.red, 10);

                        if (isBoneDetached)
                        {
                            foreach (var chunk in jointPairs)
                            {
                                var pairs = chunk.GetNativeArray(ref pairType);

                                for (int p = 0; p < chunk.Count; p++)
                                {
                                    var pair = pairs[p];

                                    if(pair.EntityA == bone.Value || pair.EntityB == bone.Value)
                                    {
                                        var pairEntity = chunk.GetNativeArray(entityType)[p];
                                        commands.AddComponent(0, pairEntity, new Disabled());
                                    }
                                }
                            }
                        }
                    }

                    // disable animation
                    var animation = SystemAPI.GetComponent<SimpleAnimation>(characterAnimation.AnimatorEntity);
                    animation.IsEnabled = false;
                    SystemAPI.SetComponent(characterAnimation.AnimatorEntity, animation);

                    var animStates = SystemAPI.GetBuffer<TzarGames.AnimationFramework.AnimationState>(characterAnimation.AnimatorEntity);

                    for (int i = 0; i < animStates.Length; i++)
                    {
                        var state = animStates[i];
                        state.Weight = 0;
                        animStates[i] = state;
                    }

                }).Run();
            }

            //var ifTriggerPressed = UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;

            //if (ifTriggerPressed)
            //{
            //    Entities
            //        .WithStructuralChanges()
            //        .WithoutBurst()
            //        .ForEach((Entity entity, DynamicBuffer<RagdollBoneEntity> boneEntities) =>
            //        {
            //            using (var boneArray = boneEntities.ToNativeArray(Unity.Collections.Allocator.Temp))
            //            {
            //                if (ifTriggerPressed)
            //                {
            //                    foreach (var bone in boneArray)
            //                    {
            //                        if (EntityManager.HasComponent<PhysicsWorldIndex>(bone.Value) == false)
            //                        {
            //                            EntityManager.AddSharedComponent(bone.Value, new PhysicsWorldIndex { Value = 0 });
            //                        }

            //                        //commands.AddComponent(0, entity, new PhysicsWorldIndex { Value = 0 });

            //                        var vel = GetComponent<PhysicsVelocity>(bone.Value);
            //                        var mass = GetComponent<PhysicsMass>(bone.Value);
            //                        Unity.Physics.Extensions.PhysicsComponentExtensions.ApplyLinearImpulse(ref vel, mass, new float3(0, 50, 0));
            //                        SetComponent(bone.Value, vel);

            //                        //EntityManager.RemoveComponent<LocalToParent>(bone.Value);
            //                        EntityManager.RemoveComponent<Parent>(bone.Value);

            //                        var l2w = GetComponent<LocalToWorld>(bone.Value);
            //                        var localTransform = LocalTransform.FromPositionRotation(l2w.Position, l2w.Rotation);
            //                        SetComponent(bone.Value, localTransform);
            //                        //SetComponent(bone.Value, new Translation { Value = l2w.Position });
            //                        //SetComponent(bone.Value, new Rotation { Value = l2w.Rotation });
            //                    }
            //                }
            //            }

            //        }).Run();
            //}
        }
    }
}
