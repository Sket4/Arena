using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using TzarGames.GameCore;
using Unity.Mathematics;

namespace Arena.Client
{
    public struct CharacterItemAppearanceState : ICleanupComponentData
    {
        public Entity Owner;
        public Entity Instance;
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    //[UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CharacterItemAppearanceSystem : SystemBase
    {
        struct CharacterEquipmentAppearanceState : IComponentData
        {
            public Entity ArmorSetEntity;
        }

        void destroyItemAppearance(Entity entity, CharacterItemAppearanceState state)
        {
            EntityManager.RemoveComponent<CharacterItemAppearanceState>(entity);

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
                .ForEach((Entity entity, CharacterEquipment equipment) =>
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

                if(EntityManager.HasComponent<CharacterItemAppearanceState>(equipment.RightHandWeapon))
                {
                    destroyItemAppearance(equipment.RightHandWeapon, EntityManager.GetComponentObject<CharacterItemAppearanceState>(equipment.RightHandWeapon));
                }

            }).Run();


            // Удаление предмета
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithNone<ActivatedState>()
                .ForEach((Entity entity, CharacterItemAppearanceState state) =>
            {
                destroyItemAppearance(entity, state);

            }).Run();

            Entities
                .WithStructuralChanges()
                .WithChangeFilter<ActivatedState>()
                .WithoutBurst()
                .ForEach((Entity entity, CharacterItemAppearanceState appearanceState, ref ActivatedState state) =>
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
                .WithNone<CharacterItemAppearanceState>()
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
                    EntityManager.AddComponentData(itemEntity, new CharacterItemAppearanceState { Instance = instance, Owner = item.Owner });

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
                .WithNone<CharacterItemAppearanceState>()
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
                var armorSetAppearanceEntity = EntityManager.GetComponentData<CharacterItemAppearanceState>(equipment.ArmorSet).Instance;
                var socket = EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearanceEntity).RightHandWeaponSocket;

                try
                {
                    var instance = EntityManager.Instantiate(prefab);

                    EntityManager.AddComponentData(instance, new Parent { Value = socket });
                    // calculate new matrix???
                    //EntityManager.AddComponentData(instance, new LocalToParent());

                    EntityManager.AddComponentData(itemEntity, new CharacterItemAppearanceState { Instance = instance, Owner = item.Owner });
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
                .WithNone<CharacterItemAppearanceState>()
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
                    var armorSetAppearanceEntity = EntityManager.GetComponentData<CharacterItemAppearanceState>(equipment.ArmorSet).Instance;
                    var socket = EntityManager.GetComponentData<ArmorSetAppearance>(armorSetAppearanceEntity).LeftHandBowSocket;

                    try
                    {
                        var instance = EntityManager.Instantiate(prefab);

                        EntityManager.AddComponentData(instance, new Parent { Value = socket });
                        // calculate new matrix???
                        //EntityManager.AddComponentData(instance, new LocalToParent());

                        EntityManager.AddComponentData(itemEntity, new CharacterItemAppearanceState { Instance = instance, Owner = item.Owner });
                        
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


            //// перемещение тела
            //var translationFromEntity = GetComponentLookup<Translation>(true);
            //var rotationFromEntity = GetComponentLookup<Rotation>(true);

            //Entities
            //    .WithReadOnly(translationFromEntity)
            //    .WithReadOnly(rotationFromEntity)
            //    .ForEach((ref Translation translation, ref Rotation rotation, in ArmorSetAppearanceInstance appearance) =>
            //{
            //    if(translationFromEntity.HasComponent(appearance.Owner) == false)
            //    {
            //        return;
            //    }
            //    var ownerTranslation = translationFromEntity[appearance.Owner];
            //    var ownerRotation = rotationFromEntity[appearance.Owner];

            //    translation.Value = ownerTranslation.Value;
            //    rotation.Value = ownerRotation.Value;

            //}).Run();
        }
    }
}