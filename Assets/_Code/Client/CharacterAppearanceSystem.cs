using Arena.Client.Presentation;
using Google.Protobuf.WellKnownTypes;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Arena.Client
{
    public struct CharacterAppearanceState : ICleanupComponentData
    {
        public Entity Owner;
        public Entity Instance;
    }
    
    struct CharacterEquipmentAppearanceState : IComponentData
    {
        public Entity ArmorSetEntity;
        public Entity HeadModelEntity;
        public Entity HairModelEntity;
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    //[UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CharacterAppearanceSystem : SystemBase
    {
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
                    destroyItemAppearance(equipment.RightHandWeapon, EntityManager.GetComponentData<CharacterAppearanceState>(equipment.RightHandWeapon));
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
                .ForEach((Entity itemEntity, ref Item item, ref ArmorSet armorSet, in ActivatedItemAppearance itemAppearance, in ActivatedState state, in SyncedColor syncedColor) =>
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
                    var ownerTransform = EntityManager.GetComponentData<LocalTransform>(item.Owner);
                    EntityManager.SetComponentData(instance, ownerTransform);
                    EntityManager.SetComponentData(instance, new LocalToWorld { Value = ownerTransform.ToMatrix() });
                    EntityManager.AddComponentData(itemEntity, new CharacterAppearanceState { Instance = instance, Owner = item.Owner });

                    var animation = EntityManager.GetComponentData<CharacterAnimation>(item.Owner);
                    animation.AnimatorEntity = instance;
                    EntityManager.SetComponentData(item.Owner, animation);

                    var ownerSkinColor = EntityManager.GetComponentData<CharacterSkinColor>(item.Owner);
                    var armorSetAppearance = EntityManager.GetComponentData<ArmorSetAppearance>(instance);
                    if (armorSetAppearance.SkinModel1 != Entity.Null)
                    {
                        EntityManager.SetComponentData(armorSetAppearance.SkinModel1, new SkinColor(ownerSkinColor.Value));
                    }
                    if (armorSetAppearance.SkinModel2 != Entity.Null)
                    {
                        EntityManager.SetComponentData(armorSetAppearance.SkinModel2, new SkinColor(ownerSkinColor.Value));
                    }
                    if (armorSetAppearance.ColoredModel1 != Entity.Null)
                    {
                        EntityManager.SetComponentData(armorSetAppearance.ColoredModel1, new SkinColor(syncedColor.Value));
                    }
                    if (armorSetAppearance.ColoredModel2 != Entity.Null)
                    {
                        EntityManager.SetComponentData(armorSetAppearance.ColoredModel2, new SkinColor(syncedColor.Value));
                    }
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
        }
    }
    
    [DisableAutoCreation]
    [UpdateAfter(typeof(CharacterAppearanceSystem))]
    [BurstCompile]
    partial struct CharacterAppearanceNativeSystem : ISystem
    {
        private ComponentLookup<PrefabID> prefabIdLookup;
        private ComponentLookup<Parent> parentLookup;
        private ComponentLookup<LocalTransform> transformLookup;
        private ComponentLookup<PostTransformMatrix> postTransformLookup;
        private ComponentLookup<HeadAppearance> headAppearanceLookup;
        private ComponentLookup<ArmorSetAppearance> armorSetAppearanceLookup;

        private EntityQuery characterQuery;

        private ComponentType appearanceStateType;
        private ComponentType characterHeadType;
        private ComponentType hairstyleType;
        
        ComponentTypeHandle<CharacterEquipmentAppearanceState> appearanceStateTypeHandle;
        ComponentTypeHandle<CharacterHead> characterHeadTypeHandle;
        ComponentTypeHandle<CharacterHairstyle> hairstyleTypeHandle;
        ComponentTypeHandle<CharacterHairColor> hairColorTypeHandle;
        ComponentTypeHandle<CharacterSkinColor> skinColorTypeHandle;
        ComponentTypeHandle<CharacterEyeColor> eyeColorTypeHandle;
        ComponentTypeHandle<Gender> genderTypeHandle;
        
        public void OnCreate(ref SystemState state)
        {
            prefabIdLookup = state.GetComponentLookup<PrefabID>(true);
            parentLookup = state.GetComponentLookup<Parent>(true);
            transformLookup = state.GetComponentLookup<LocalTransform>(true);
            postTransformLookup = state.GetComponentLookup<PostTransformMatrix>(true);
            headAppearanceLookup = state.GetComponentLookup<HeadAppearance>(true);
            armorSetAppearanceLookup = state.GetComponentLookup<ArmorSetAppearance>(true);
            
            state.RequireForUpdate<MainDatabaseTag>();

            appearanceStateType = ComponentType.ReadWrite<CharacterEquipmentAppearanceState>();
            characterHeadType = ComponentType.ReadOnly<CharacterHead>();
            hairstyleType = ComponentType.ReadOnly<CharacterHairstyle>();

            appearanceStateTypeHandle = state.GetComponentTypeHandle<CharacterEquipmentAppearanceState>(false);
            characterHeadTypeHandle = state.GetComponentTypeHandle<CharacterHead>(true);
            hairstyleTypeHandle = state.GetComponentTypeHandle<CharacterHairstyle>(true);
            hairColorTypeHandle = state.GetComponentTypeHandle<CharacterHairColor>(true);
            eyeColorTypeHandle = state.GetComponentTypeHandle<CharacterEyeColor>(true);
            skinColorTypeHandle = state.GetComponentTypeHandle<CharacterSkinColor>(true);
            genderTypeHandle = state.GetComponentTypeHandle<Gender>(true);

            characterQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new []
                {
                    appearanceStateType,
                    characterHeadType,
                    hairstyleType,
                    ComponentType.ReadOnly<Gender>()
                }
            });
        }
        
        [BurstCompile]
        [WithChangeFilter(typeof(SyncedColor))]
        [WithAll(typeof(ArmorSet))]
        partial struct ArmorSetColorChangedJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<ArmorSetAppearance> AppearanceLookup;
            public EntityCommandBuffer Commands;
            
            public void Execute(in CharacterAppearanceState appearanceState, in SyncedColor color)
            {
                if (AppearanceLookup.TryGetComponent(appearanceState.Instance, out var armorSetAppearance) == false)
                {
                    return;
                }

                if (armorSetAppearance.ColoredModel1 != Entity.Null)
                {
                    Commands.SetComponent(armorSetAppearance.ColoredModel1, new SkinColor(color.Value));
                }
                if (armorSetAppearance.ColoredModel2 != Entity.Null)
                {
                    Commands.SetComponent(armorSetAppearance.ColoredModel2, new SkinColor(color.Value));
                }
            }
        }

        [BurstCompile]
        [WithChangeFilter(typeof(CharacterHairColor))]
        partial struct HairColorChangeJob : IJobEntity
        {
            public EntityCommandBuffer Commands;

            [ReadOnly]
            public ComponentLookup<HeadAppearance> HeadAppearanceLookup;
            
            public void Execute(in CharacterHairColor hairColor, in CharacterEquipmentAppearanceState appearance)
            {
                if (appearance.HairModelEntity != Entity.Null)
                {
                    Debug.Log("changed hair color");
                    Commands.SetComponent(appearance.HairModelEntity, new ColorData(hairColor.Value));    
                }

                if (HeadAppearanceLookup.TryGetComponent(appearance.HeadModelEntity, out var headAppearance))
                {
                    Commands.SetComponent(headAppearance.BrowsModel, new ColorData(hairColor.Value));
                }
            }
        }
        
        [BurstCompile]
        [WithChangeFilter(typeof(CharacterSkinColor))]
        partial struct SkinColorChangeJob : IJobEntity
        {
            public EntityCommandBuffer Commands;

            [ReadOnly] public ComponentLookup<HeadAppearance> HeadAppearanceLookup;
            [ReadOnly] public ComponentLookup<ArmorSetAppearance> ArmorSetAppearLookup;
            
            public void Execute(in CharacterSkinColor skinColor, in CharacterAnimation animation, in CharacterEquipmentAppearanceState appearanceState)
            {
                Debug.Log("skin color changed");
                
                if (HeadAppearanceLookup.TryGetComponent(appearanceState.HeadModelEntity, out var headAppearance))
                {
                    Commands.SetComponent(headAppearance.HeadModel, new SkinColor(skinColor.Value));
                }

                if (ArmorSetAppearLookup.TryGetComponent(animation.AnimatorEntity, out var armorSetAppearance))
                {
                    if (armorSetAppearance.SkinModel1 != Entity.Null)
                    {
                        Commands.SetComponent(armorSetAppearance.SkinModel1, new SkinColor(skinColor.Value));
                    }
                    if (armorSetAppearance.SkinModel2 != Entity.Null)
                    {
                        Commands.SetComponent(armorSetAppearance.SkinModel2, new SkinColor(skinColor.Value));
                    }
                }
            }
        }
        
        [BurstCompile]
        [WithChangeFilter(typeof(CharacterEyeColor))]
        partial struct EyeColorChangeJob : IJobEntity
        {
            public EntityCommandBuffer Commands;

            [ReadOnly]
            public ComponentLookup<HeadAppearance> HeadAppearanceLookup;
            
            public void Execute(in CharacterEyeColor eyeColor, in CharacterEquipmentAppearanceState appearance)
            {
                if (HeadAppearanceLookup.TryGetComponent(appearance.HeadModelEntity, out var headAppearance) == false)
                {
                    return;
                }
                Debug.Log("eye color changed");
                Commands.SetComponent(headAppearance.EyesModel, new SkinColor(eyeColor.Value));
            }
        }
        
        [BurstCompile]
        [WithNone(typeof(Parent))]
        partial struct CleanupHeadJob : IJobEntity
        {
            public EntityCommandBuffer Commands;
            
            public void Execute(Entity entity, in HeadAppearance head)
            {
                Commands.DestroyEntity(entity);
                if (head.HairInstance != Entity.Null)
                {
                    Commands.DestroyEntity(head.HairInstance);    
                }
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var dbEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var db = SystemAPI.GetBuffer<IdToEntity>(dbEntity).ToNativeArray(Allocator.Temp);
            
            characterQuery.SetChangedVersionFilter(characterHeadType);
            processHeadQuery(ref state, in db);
            
            characterQuery.SetChangedVersionFilter(appearanceStateType);
            processHeadQuery(ref state, in db);
            processHairstyleQuery(ref state, in db);
            
            characterQuery.SetChangedVersionFilter(hairstyleType);
            processHairstyleQuery(ref state, in db);
            
            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                headAppearanceLookup.Update(ref state);
                armorSetAppearanceLookup.Update(ref state);
                
                // TODO совместить эти 3 джоба в один и работать через Chunk и DidChange
                
                // hair color
                var hairColorChangedJob = new HairColorChangeJob
                {
                    Commands = ecb,
                    HeadAppearanceLookup = headAppearanceLookup
                };
                hairColorChangedJob.Run();
                
                // skin color
                var skinColorChangeJob = new SkinColorChangeJob
                {
                    Commands = ecb,
                    HeadAppearanceLookup = headAppearanceLookup,
                    ArmorSetAppearLookup = armorSetAppearanceLookup
                };
                skinColorChangeJob.Run();
                
                // eye color
                var eyeColorChangeJob = new EyeColorChangeJob
                {
                    Commands = ecb,
                    HeadAppearanceLookup = headAppearanceLookup,
                };
                eyeColorChangeJob.Run();
                
                // cleanup head
                var cleanupHeadJob = new CleanupHeadJob
                {
                    Commands = ecb
                };
                cleanupHeadJob.Run();
                
                // armor color
                var armorColorChangedJob = new ArmorSetColorChangedJob
                {
                    Commands = ecb,
                    AppearanceLookup = armorSetAppearanceLookup,
                };
                armorColorChangedJob.Run();
                
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }
        
        void processHairstyleQuery(ref SystemState state, in NativeArray<IdToEntity> db)
        {
            var chunks = characterQuery.ToArchetypeChunkArray(Allocator.Temp);

            foreach (var chunk in chunks)
            {
                appearanceStateTypeHandle.Update(ref state);
                hairstyleTypeHandle.Update(ref state);
                hairColorTypeHandle.Update(ref state);
                
                var appearances = chunk.GetNativeArray(ref appearanceStateTypeHandle);
                var hairstyles = chunk.GetNativeArray(ref hairstyleTypeHandle);
                var haircolors = chunk.GetNativeArray(ref hairColorTypeHandle);

                for (int c = 0; c < chunk.Count; c++)
                {
                    var appearance = appearances[c];
                    var hairstyle = hairstyles[c];
                    var haircolor = haircolors[c];
                    
                    processHairstyleModel(ref state, ref appearance, in hairstyle, in haircolor, in db);
                    
                    appearanceStateTypeHandle.Update(ref state);
                    appearances = chunk.GetNativeArray(ref appearanceStateTypeHandle);
                    appearances[c] = appearance;
                }
            }
        }

        private void processHairstyleModel(ref SystemState state, ref CharacterEquipmentAppearanceState appearance, in CharacterHairstyle hairstyle, in CharacterHairColor hairColor, in NativeArray<IdToEntity> db)
        {
            var removeHairAndExit = hairstyle.ID.Value == 0;

            if (removeHairAndExit)
            {
                if (appearance.HairModelEntity != Entity.Null)
                {
                    state.EntityManager.DestroyEntity(appearance.HairModelEntity);
                    appearance.HairModelEntity = Entity.Null;
                }
                return;
            }

            var needChangeHairstyle = true;

            if (appearance.HairModelEntity != Entity.Null)
            {
                if (state.EntityManager.HasComponent<PrefabID>(appearance.HairModelEntity))
                {
                    var currentHairstyleID =
                        state.EntityManager.GetComponentData<PrefabID>(appearance.HairModelEntity);

                    if (currentHairstyleID == hairstyle.ID)
                    {
                        needChangeHairstyle = false;
                    }
                }
                else
                {
                    Debug.LogError($"invalid existing hairstyle with entity {appearance.HairModelEntity.Index}");
                    appearance.HairModelEntity = Entity.Null;
                }
            }

            if (appearance.HeadModelEntity == Entity.Null)
            {
                Debug.LogError("failed to change hairstyle - no head instance");
                return;
            }

            if (state.EntityManager.HasComponent<HeadAppearance>(appearance.HeadModelEntity) == false)
            {
                Debug.LogError($"no head socket in head {appearance.HeadModelEntity.Index}");
                return;
            }

            Debug.Log($"changing or updating hairstyle {hairstyle.ID.Value}");
            
            var headAppearance = state.EntityManager.GetComponentData<HeadAppearance>(appearance.HeadModelEntity);
            
            if (needChangeHairstyle)
            {
                if (IdToEntity.TryGetEntityById(db, hairstyle.ID.Value, out var hairstylePrefab) == false)
                {
                    Debug.LogError($"failed to find hairstyle prefab with id {hairstyle.ID.Value}");
                    return;       
                }

                if (appearance.HairModelEntity != Entity.Null)
                {
                    state.EntityManager.DestroyEntity(appearance.HairModelEntity);    
                }
                appearance.HairModelEntity = state.EntityManager.Instantiate(hairstylePrefab);
                state.EntityManager.AddComponentData(appearance.HairModelEntity, new Parent { Value = headAppearance.HairSocketEntity });
                state.EntityManager.SetComponentData(appearance.HairModelEntity, new CopyAmbientLight { Source = headAppearance.HeadModel });
                
                state.EntityManager.SetComponentData(appearance.HairModelEntity, new ColorData(hairColor.Value));
            }
            else
            {
                state.EntityManager.SetComponentData(appearance.HairModelEntity, new Parent { Value = headAppearance.HairSocketEntity });
            }

            headAppearance.HairInstance = appearance.HairModelEntity;
            state.EntityManager.SetComponentData(appearance.HeadModelEntity, headAppearance);
        }

        void processHeadQuery(ref SystemState state, in NativeArray<IdToEntity> db)
        {
            var chunks = characterQuery.ToArchetypeChunkArray(Allocator.Temp);

            foreach (var chunk in chunks)
            {
                appearanceStateTypeHandle.Update(ref state);
                characterHeadTypeHandle.Update(ref state);
                genderTypeHandle.Update(ref state);
                eyeColorTypeHandle.Update(ref state);
                skinColorTypeHandle.Update(ref state);
                hairColorTypeHandle.Update(ref state);
                
                var appearances = chunk.GetNativeArray(ref appearanceStateTypeHandle);
                var heads = chunk.GetNativeArray(ref characterHeadTypeHandle);
                var genders = chunk.GetNativeArray(ref genderTypeHandle);
                var skinColors = chunk.GetNativeArray(ref skinColorTypeHandle);
                var eyeColors = chunk.GetNativeArray(ref eyeColorTypeHandle);
                var hairColors = chunk.GetNativeArray(ref hairColorTypeHandle);

                for (int c = 0; c < chunk.Count; c++)
                {
                    var appearance = appearances[c];
                    var head = heads[c];
                    var gender = genders[c];
                    var skinColor = skinColors[c];
                    var eyeColor = eyeColors[c];
                    var hairColor = hairColors[c];
                    
                    processHeadModel(ref state, ref appearance, in head, in hairColor, in skinColor, in eyeColor, in gender, in db);
                    
                    appearanceStateTypeHandle.Update(ref state);
                    appearances = chunk.GetNativeArray(ref appearanceStateTypeHandle);
                    appearances[c] = appearance;
                }
            }
        }
        
        private void processHeadModel(
            ref SystemState state, 
            ref CharacterEquipmentAppearanceState appearance,
            in CharacterHead head, 
            in CharacterHairColor haircolor,
            in CharacterSkinColor skincolor,
            in CharacterEyeColor eyecolor,
            in Gender gender,
            in NativeArray<IdToEntity> db)
        {
            bool shouldProcess = false;
            var existingHeadInstance = Entity.Null;
            var needChangeHead = false;
            
            var armorSetAppearanceState = state.EntityManager.GetComponentData<CharacterAppearanceState>(appearance.ArmorSetEntity);
            var armorSetAppearance =
                state.EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearanceState.Instance);

            if (appearance.HeadModelEntity != Entity.Null)
            {
                prefabIdLookup.Update(ref state);
                
                if (prefabIdLookup.TryGetComponent(appearance.HeadModelEntity, out var headModelID))
                {
                    existingHeadInstance = appearance.HeadModelEntity;
                    
                    if (headModelID.Value != head.ModelID.Value)
                    {
                        needChangeHead = true;
                        shouldProcess = true;
                    }
                    else
                    {
                        parentLookup.Update(ref state);
                        
                        if (parentLookup.TryGetComponent(existingHeadInstance, out var existingHeadParent))
                        {
                            if (existingHeadParent.Value != armorSetAppearance.HeadSocket)
                            {
                                needChangeHead = true;
                                shouldProcess = true;
                            }
                        }
                        else
                        {
                            // почему то у головы не оказалось сокета-родителя, поэтому нужен сброс и пересборка
                            needChangeHead = true;
                            shouldProcess = true;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Head model instance not found, entity: {appearance.HeadModelEntity.Index}");
                    appearance.HeadModelEntity = Entity.Null;
                    shouldProcess = true;
                }
            }
            else
            {
                shouldProcess = true;
            }

            if (shouldProcess == false)
            {
                return;
            }

            if (existingHeadInstance != Entity.Null && needChangeHead)
            {
                state.EntityManager.DestroyEntity(existingHeadInstance);
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

            Entity headInstance;

            if (existingHeadInstance != Entity.Null && needChangeHead == false)
            {
                headInstance = existingHeadInstance;
            }
            else
            {
                headInstance = state.EntityManager.Instantiate(headPrefab);
                state.EntityManager.AddComponentData(headInstance, new Parent { Value = armorSetAppearance.HeadSocket });
            }
            
            appearance.HeadModelEntity = headInstance;
            state.EntityManager.SetComponentData(headInstance, LocalTransform.Identity);

            var headAppearance = state.EntityManager.GetComponentData<HeadAppearance>(headInstance);
            state.EntityManager.SetComponentData(headAppearance.BrowsModel, new ColorData(haircolor.Value));
            state.EntityManager.SetComponentData(headAppearance.HeadModel, new SkinColor(skincolor.Value));
            
            var ambientLightSource = Entity.Null;

            if (armorSetAppearance.ColoredModel1 != Entity.Null)
                ambientLightSource = armorSetAppearance.ColoredModel1;
            if (armorSetAppearance.SkinModel1 != Entity.Null)
                ambientLightSource = armorSetAppearance.SkinModel1;
            
            state.EntityManager.SetComponentData(headAppearance.HeadModel, new CopyAmbientLight { Source = ambientLightSource });
            state.EntityManager.SetComponentData(headAppearance.EyesModel, new CopyAmbientLight { Source = ambientLightSource });
            state.EntityManager.SetComponentData(headAppearance.EyesModel, new SkinColor(eyecolor.Value));

            var armorSetInstanceRig = state.EntityManager.GetComponentData<HumanRig>(armorSetAppearanceState.Instance);
            var headRig = state.EntityManager.GetComponentData<HumanRig>(headInstance);
            var headPrefabRig = state.EntityManager.GetComponentData<HumanRig>(headPrefab);

            var armorSetAppearancePrefab =
                state.EntityManager.GetComponentData<ActivatedItemAppearance>(appearance.ArmorSetEntity).Prefab;
            var armorSetPrefabRig = state.EntityManager.GetComponentData<HumanRig>(armorSetAppearancePrefab);
            var armorSetPrefabAppearance = state.EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearancePrefab);
            var armorSetPrefabHeadSocket = armorSetPrefabAppearance.HeadSocket;
            
            ///
            postTransformLookup.Update(ref state);
            transformLookup.Update(ref state);
            parentLookup.Update(ref state);
            ///
            
            TransformHelpers.ComputeWorldTransformMatrix(armorSetPrefabHeadSocket, out var armorSetPrefabHeadSocketTransform, ref transformLookup, ref parentLookup, ref postTransformLookup);
            var positionDisp = armorSetPrefabHeadSocketTransform.Translation();
            
            transformBone(ref state, armorSetPrefabRig.Head, headPrefabRig.Head, positionDisp, armorSetInstanceRig.Head, headRig.Head);
            transformBone(ref state, armorSetPrefabRig.Neck, headPrefabRig.Neck, positionDisp, armorSetInstanceRig.Neck, headRig.Neck);
            transformBone(ref state, armorSetPrefabRig.UpperChest, headPrefabRig.UpperChest, positionDisp, armorSetInstanceRig.UpperChest, headRig.UpperChest);
            transformBone(ref state, armorSetPrefabRig.LeftShoulder, headPrefabRig.LeftShoulder, positionDisp, armorSetInstanceRig.LeftShoulder, headRig.LeftShoulder);
            transformBone(ref state, armorSetPrefabRig.LeftUpperArm, headPrefabRig.LeftUpperArm, positionDisp, armorSetInstanceRig.LeftUpperArm, headRig.LeftUpperArm);
            transformBone(ref state, armorSetPrefabRig.RightShoulder, headPrefabRig.RightShoulder, positionDisp, armorSetInstanceRig.RightShoulder, headRig.RightShoulder);
            transformBone(ref state, armorSetPrefabRig.RightUpperArm, headPrefabRig.RightUpperArm, positionDisp, armorSetInstanceRig.RightUpperArm, headRig.RightUpperArm);
        }

        void transformBone(ref SystemState state, Entity armorPrefabBone, Entity headPrefabBone, float3 positionDisp, Entity parent, Entity instanceBone)
        {
            TransformHelpers.ComputeWorldTransformMatrix(armorPrefabBone, out var prefabHeadL2W, ref transformLookup, ref parentLookup, ref postTransformLookup);
            TransformHelpers.ComputeWorldTransformMatrix(headPrefabBone, out var boneL2W, ref transformLookup, ref parentLookup, ref postTransformLookup);
            
            //Debug.DrawRay(prefabHeadL2W.Translation(), Vector3.left * 0.1f, Color.red, 999);
            //Debug.DrawRay(headL2W.Translation(), Vector3.left * 0.1f, Color.yellow, 999);

            var prefabBoneW2L = math.inverse(prefabHeadL2W);
            boneL2W = float4x4.TRS(boneL2W.Translation() + positionDisp, boneL2W.Rotation(), boneL2W.Scale());
            var prefabSpaceBoneTransform = math.mul(prefabBoneW2L, boneL2W);
            
            state.EntityManager.SetComponentData(instanceBone, new Parent { Value = parent });
            var newLT = LocalTransform.FromPositionRotationScale(prefabSpaceBoneTransform.Translation(), prefabSpaceBoneTransform.Rotation(), prefabSpaceBoneTransform.Scale().x);
            state.EntityManager.SetComponentData(instanceBone, newLT);
        }
    }
}