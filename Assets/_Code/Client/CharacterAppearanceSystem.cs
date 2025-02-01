using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using TzarGames.GameCore;
using Unity.Collections;
using Unity.Mathematics;

namespace Arena.Client
{
    public struct CharacterAppearanceState : ICleanupComponentData
    {
        public Entity Owner;
        public Entity Instance;
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    //[UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CharacterAppearanceSystem : SystemBase
    {
        struct CharacterEquipmentAppearanceState : IComponentData
        {
            public Entity ArmorSetEntity;
            public Entity HeadModelEntity;
        }

        private ComponentLookup<PrefabID> prefabIdLookup;
        private ComponentLookup<Parent> parentLookup;
        private ComponentLookup<LocalTransform> transformLookup;
        private ComponentLookup<PostTransformMatrix> postTransformLookup;

        protected override void OnCreate()
        {
            base.OnCreate();
            prefabIdLookup = GetComponentLookup<PrefabID>(true);
            parentLookup = GetComponentLookup<Parent>(true);
            transformLookup = GetComponentLookup<LocalTransform>(true);
            postTransformLookup = GetComponentLookup<PostTransformMatrix>(true);
            RequireForUpdate<MainDatabaseTag>();
        }

        void destroyItemAppearance(Entity entity, CharacterAppearanceState state)
        {
            EntityManager.RemoveComponent<CharacterAppearanceState>(entity);

            if(EntityManager.Exists(state.Instance) == false)
            {
                return;
            }

            if(EntityManager.HasComponent<ArmorSetAppearance>(entity))
            {
                EntityManager.RemoveComponent<ArmorSetAppearance>(entity);
            }

            if(EntityManager.HasComponent<CharacterAnimation>(state.Owner))
            {
                var characterAnimation = EntityManager.GetComponentData<CharacterAnimation>(state.Owner);
                if(characterAnimation.AnimatorEntity == state.Instance)
                {
                    characterAnimation.AnimatorEntity = Entity.Null;
                    EntityManager.SetComponentData(state.Owner, characterAnimation);
                }
            }

            EntityManager.DestroyEntity(state.Instance);
        }

        protected override void OnUpdate()
        {
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithChangeFilter<CharacterEquipment>()
                .ForEach((Entity entity, in CharacterEquipment equipment) =>
            {
                if (equipment.ArmorSet == Entity.Null)
                {
                    return;
                }

                if(EntityManager.HasComponent<CharacterEquipmentAppearanceState>(entity) == false)
                {
                    var newState = new CharacterEquipmentAppearanceState
                    {
                        ArmorSetEntity = equipment.ArmorSet
                    };
                    EntityManager.AddComponentData(entity, newState);
                    return;
                }

                var state = EntityManager.GetComponentData<CharacterEquipmentAppearanceState>(entity);

                if(state.ArmorSetEntity == equipment.ArmorSet)
                {
                    return;
                }
                state.ArmorSetEntity = equipment.ArmorSet;
                EntityManager.SetComponentData(entity, state);

                if(EntityManager.HasComponent<CharacterAppearanceState>(equipment.RightHandWeapon))
                {
                    destroyItemAppearance(equipment.RightHandWeapon, EntityManager.GetComponentObject<CharacterAppearanceState>(equipment.RightHandWeapon));
                }

            }).Run();


            // Удаление предмета
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithNone<ActivatedState>()
                .ForEach((Entity entity, CharacterAppearanceState state) =>
            {
                destroyItemAppearance(entity, state);

            }).Run();

            Entities
                .WithStructuralChanges()
                .WithChangeFilter<ActivatedState>()
                .WithoutBurst()
                .ForEach((Entity entity, CharacterAppearanceState appearanceState, ref ActivatedState state) =>
            {
                if(state.Activated == false)
                {
                    destroyItemAppearance(entity, appearanceState);
                }

            }).Run();

            // добавление Armor Set
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithNone<CharacterAppearanceState>()
                .ForEach((Entity itemEntity, ref Item item, ref ArmorSet armorSet, in ActivatedItemAppearance itemAppearance, in ActivatedState state) =>
            {
                if(item.Owner == Entity.Null)
                {
                    return;
                }

                if(state.Activated == false)
                {
                    return;
                }

                var prefab = itemAppearance.Prefab;

                if (EntityManager.Exists(prefab) == false)
                {
                    Debug.LogErrorFormat($"Prefab {prefab} not found");
                    return;
                }

                try
                {
                    var instance = EntityManager.Instantiate(prefab);

                    EntityManager.SetComponentData(instance, new Owner(item.Owner));
                    EntityManager.AddComponentData(itemEntity, new CharacterAppearanceState { Instance = instance, Owner = item.Owner });

                    var animation = EntityManager.GetComponentData<CharacterAnimation>(item.Owner);
                    animation.AnimatorEntity = instance;
                    EntityManager.SetComponentData(item.Owner, animation);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }

            }).Run();


            // добавление одноручного оружия
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithAll<OneHandedItem>()
                .WithNone<CharacterAppearanceState>()
                .ForEach((Entity itemEntity, in Item item, in ActivatedItemAppearance itemAppearance, in ActivatedState state) =>
            {
                if (item.Owner == Entity.Null)
                {
                    return;
                }

                if (state.Activated == false)
                {
                    return;
                }

                if (EntityManager.HasComponent<CharacterEquipment>(item.Owner) == false)
                {
                    return;
                }

                var prefab = itemAppearance.Prefab;

                if (EntityManager.Exists(prefab) == false)
                {
                    Debug.LogError($"Prefab {prefab} not found");
                    return;
                }

                var equipment = EntityManager.GetComponentData<CharacterEquipment>(item.Owner);
                var armorSetAppearanceEntity = EntityManager.GetComponentData<CharacterAppearanceState>(equipment.ArmorSet).Instance;
                var socket = EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearanceEntity).RightHandWeaponSocket;

                try
                {
                    var instance = EntityManager.Instantiate(prefab);

                    EntityManager.AddComponentData(instance, new Parent { Value = socket });
                    // calculate new matrix???
                    //EntityManager.AddComponentData(instance, new LocalToParent());

                    EntityManager.AddComponentData(itemEntity, new CharacterAppearanceState { Instance = instance, Owner = item.Owner });
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }

            }).Run();
            
            // добавление лука
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithAll<Bow>()
                .WithNone<CharacterAppearanceState>()
                .ForEach((Entity itemEntity, in Item item, in ActivatedItemAppearance itemAppearance, in ActivatedState state) =>
                {
                    if (item.Owner == Entity.Null)
                    {
                        return;
                    }

                    if (state.Activated == false)
                    {
                        return;
                    }

                    if (EntityManager.HasComponent<CharacterEquipment>(item.Owner) == false)
                    {
                        return;
                    }

                    var prefab = itemAppearance.Prefab;

                    if (EntityManager.Exists(prefab) == false)
                    {
                        Debug.LogError($"Prefab {prefab} not found");
                        return;
                    }

                    var equipment = EntityManager.GetComponentData<CharacterEquipment>(item.Owner);
                    var armorSetAppearanceEntity = EntityManager.GetComponentData<CharacterAppearanceState>(equipment.ArmorSet).Instance;
                    var socket = EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearanceEntity).LeftHandBowSocket;

                    try
                    {
                        var instance = EntityManager.Instantiate(prefab);

                        EntityManager.AddComponentData(instance, new Parent { Value = socket });
                        // calculate new matrix???
                        //EntityManager.AddComponentData(instance, new LocalToParent());

                        EntityManager.AddComponentData(itemEntity, new CharacterAppearanceState { Instance = instance, Owner = item.Owner });
                        
                        if (EntityManager.HasComponent<Owner>(prefab))
                        {
                            EntityManager.SetComponentData(instance, new Owner(item.Owner));    
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                }).Run();
            
            prefabIdLookup.Update(this);
            postTransformLookup.Update(this);
            transformLookup.Update(this);
            parentLookup.Update(this);
            
            var dbEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var db = SystemAPI.GetBuffer<IdToEntity>(dbEntity).ToNativeArray(Allocator.Temp);
            
            Entities
                .WithStructuralChanges()
                .WithChangeFilter<CharacterHead>()
                .WithReadOnly(db)
                .ForEach((ref CharacterEquipmentAppearanceState appearance, in CharacterHead head, in Gender gender) =>
                {
                    processHeadModel(ref appearance, prefabIdLookup, ref parentLookup, ref transformLookup, ref postTransformLookup, head, db, gender);
                }).Run();
            
            Entities
                .WithStructuralChanges()
                .WithChangeFilter<CharacterEquipmentAppearanceState>()
                .WithReadOnly(db)
                .ForEach((ref CharacterEquipmentAppearanceState appearance, in CharacterHead head, in Gender gender) =>
                {
                    processHeadModel(ref appearance, prefabIdLookup, ref parentLookup, ref transformLookup, ref postTransformLookup, head, db, gender);
                }).Run();
        }

        private void processHeadModel(ref CharacterEquipmentAppearanceState appearance, 
            in ComponentLookup<PrefabID> prefabIdLookup, 
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<LocalTransform> transformLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformLookup,
            CharacterHead head,
            NativeArray<IdToEntity> db, 
            Gender gender)
        {
            bool needInstance = false;
            bool alreadyHasHeadModelInstance = false;

            if (appearance.HeadModelEntity != Entity.Null)
            {
                if (prefabIdLookup.TryGetComponent(appearance.HeadModelEntity, out var headModelID))
                {
                    alreadyHasHeadModelInstance = true;

                    if (headModelID.Value != head.ModelID.Value)
                    {
                        needInstance = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"Head model instance not found, entity: {appearance.HeadModelEntity.Index}");
                    appearance.HeadModelEntity = Entity.Null;
                    needInstance = true;
                }
            }
            else
            {
                needInstance = true;
            }

            if (needInstance == false)
            {
                return;
            }

            if (alreadyHasHeadModelInstance)
            {
                EntityManager.DestroyEntity(appearance.HeadModelEntity);
                appearance.HeadModelEntity = Entity.Null;
            }

            Entity headPrefab = Entity.Null;

            if (IdToEntity.TryGetEntityById(db, head.ModelID.Value, out var prefab) == false)
            {
                Debug.LogError($"Failed to find head prefab by id: {head.ModelID.Value}");

                var defaultHeadId = gender.Value == Genders.Female
                    ? Identifiers.DefaultFemaleHeadID
                    : Identifiers.DefaultMaleHeadID;

                if (IdToEntity.TryGetEntityById(db, defaultHeadId, out prefab) == false)
                {
                    Debug.LogError($"Failed to find prefab for default head id: {defaultHeadId}");
                }
                else
                {
                    headPrefab = prefab;
                }
            }
            else
            {
                headPrefab = prefab;
            }

            if (headPrefab == Entity.Null)
            {
                Debug.LogError("no head prefab found");
                return;
            }

            var headInstance = EntityManager.Instantiate(headPrefab);
            appearance.HeadModelEntity = headInstance;

            var armorSetAppearanceState = EntityManager.GetComponentData<CharacterAppearanceState>(appearance.ArmorSetEntity);
            var armorSetAppearance =
                EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearanceState.Instance);

            EntityManager.AddComponentData(headInstance, new Parent { Value = armorSetAppearance.HeadSocket });
            EntityManager.SetComponentData(headInstance, LocalTransform.Identity);

            var armorSetInstanceRig = EntityManager.GetComponentData<HumanRig>(armorSetAppearanceState.Instance);
            var headRig = EntityManager.GetComponentData<HumanRig>(headInstance);
            var headPrefabRig = EntityManager.GetComponentData<HumanRig>(headPrefab);

            var armorSetAppearancePrefab =
                EntityManager.GetComponentData<ActivatedItemAppearance>(appearance.ArmorSetEntity).Prefab;
            var armorSetPrefabRig = EntityManager.GetComponentData<HumanRig>(armorSetAppearancePrefab);
            var armorSetPrefabAppearance = EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearancePrefab);
            var armorSetPrefabHeadSocket = armorSetPrefabAppearance.HeadSocket;
            var armorSetPrefabHeadSocketTransform =
                EntityManager.GetComponentData<LocalTransform>(armorSetPrefabHeadSocket);
            var positionDisp = armorSetPrefabHeadSocketTransform.Position;
            
            ///
            postTransformLookup.Update(this);
            transformLookup.Update(this);
            parentLookup.Update(this);
            /// 
            
            transformBone(armorSetPrefabRig.Head, headPrefabRig.Head, positionDisp, armorSetInstanceRig.Head, headRig.Head);
            transformBone(armorSetPrefabRig.Neck, headPrefabRig.Neck, positionDisp, armorSetInstanceRig.Neck, headRig.Neck);
            transformBone(armorSetPrefabRig.UpperChest, headPrefabRig.UpperChest, positionDisp, armorSetInstanceRig.UpperChest, headRig.UpperChest);
            transformBone(armorSetPrefabRig.LeftShoulder, headPrefabRig.LeftShoulder, positionDisp, armorSetInstanceRig.LeftShoulder, headRig.LeftShoulder);
            transformBone(armorSetPrefabRig.LeftUpperArm, headPrefabRig.LeftUpperArm, positionDisp, armorSetInstanceRig.LeftUpperArm, headRig.LeftUpperArm);
            transformBone(armorSetPrefabRig.RightShoulder, headPrefabRig.RightShoulder, positionDisp, armorSetInstanceRig.RightShoulder, headRig.RightShoulder);
            transformBone(armorSetPrefabRig.RightUpperArm, headPrefabRig.RightUpperArm, positionDisp, armorSetInstanceRig.RightUpperArm, headRig.RightUpperArm);
        }

        void transformBone(Entity armorPrefabBone, Entity headPrefabBone, float3 positionDisp, Entity parent, Entity instanceBone)
        {
            TransformHelpers.ComputeWorldTransformMatrix(armorPrefabBone, out var prefabHeadL2W, ref transformLookup, ref parentLookup, ref postTransformLookup);
            TransformHelpers.ComputeWorldTransformMatrix(headPrefabBone, out var boneL2W, ref transformLookup, ref parentLookup, ref postTransformLookup);
            
            //Debug.DrawRay(prefabHeadL2W.Translation(), Vector3.left * 0.1f, Color.red, 999);
            //Debug.DrawRay(headL2W.Translation(), Vector3.left * 0.1f, Color.yellow, 999);

            var prefabBoneW2L = math.inverse(prefabHeadL2W);
            boneL2W = float4x4.TRS(boneL2W.Translation() + positionDisp, boneL2W.Rotation(), boneL2W.Scale());
            var prefabSpaceBoneTransform = math.mul(prefabBoneW2L, boneL2W);
            
            EntityManager.SetComponentData(instanceBone, new Parent { Value = parent });
            var newLT = LocalTransform.FromPositionRotationScale(prefabSpaceBoneTransform.Translation(), prefabSpaceBoneTransform.Rotation(), prefabSpaceBoneTransform.Scale().x);
            EntityManager.SetComponentData(instanceBone, newLT);
        }
    }
}