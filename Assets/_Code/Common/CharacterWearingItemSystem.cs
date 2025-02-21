using Unity.Entities;
using TzarGames.GameCore;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(ActivateItemRequestSystem))]
    public partial class CharacterWearingItemRequestSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithChangeFilter<ActivatedState, Item>()
                .ForEach((Entity itemEntity, in Item item, in ActivatedState state) =>
                {
                    if(SystemAPI.HasComponent<CharacterEquipment>(item.Owner) == false)
                    {
                        return;
                    }

                    var equipment = SystemAPI.GetComponent<CharacterEquipment>(item.Owner);
                    bool equipmentChanged = false;

                    if(SystemAPI.HasComponent<ArmorSet>(itemEntity))
                    {
                        if(state.Activated)
                        {
                            if(equipment.ArmorSet == Entity.Null)
                            {
                                equipment.ArmorSet = itemEntity;
                                equipmentChanged = true;
                            }
                        }
                        else
                        {
                            if(equipment.ArmorSet == itemEntity)
                            {
                                equipment.ArmorSet = Entity.Null;
                                equipmentChanged = true;
                            }
                        }
                    }

                    if(SystemAPI.HasComponent<OneHandedItem>(itemEntity))
                    {
                        if(state.Activated)
                        {
                            if(equipment.RightHandWeapon == Entity.Null)
                            {
                                equipment.RightHandWeapon = itemEntity;
                                equipmentChanged = true;
                            }
                        }
                        else
                        {
                            if(equipment.RightHandWeapon == itemEntity)
                            {
                                equipment.RightHandWeapon = Entity.Null;
                                equipmentChanged = true;
                            }
                        }
                    }
                    
                    if(SystemAPI.HasComponent<Shield>(itemEntity))
                    {
                        if(state.Activated)
                        {
                            if(equipment.LeftHandShield == Entity.Null)
                            {
                                equipment.LeftHandShield = itemEntity;
                                equipmentChanged = true;
                            }
                        }
                        else
                        {
                            if(equipment.LeftHandShield == itemEntity)
                            {
                                equipment.LeftHandShield = Entity.Null;
                                equipmentChanged = true;
                            }
                        }
                    }
                    
                    if(SystemAPI.HasComponent<Bow>(itemEntity))
                    {
                        if(state.Activated)
                        {
                            if(equipment.LeftHandBow == Entity.Null)
                            {
                                equipment.LeftHandBow = itemEntity;
                                equipmentChanged = true;
                            }
                        }
                        else
                        {
                            if(equipment.LeftHandBow == itemEntity)
                            {
                                equipment.LeftHandBow = Entity.Null;
                                equipmentChanged = true;
                            }
                        }
                    }

                    if(equipmentChanged)
                    {
                        SystemAPI.SetComponent(item.Owner, equipment);
                    }

                }).Schedule();


            Entities.ForEach((ref ActivateItemRequest request) =>
            {
                if(request.State != ActivateItemRequestState.Processing)
                {
                    return;
                }

                if(request.Activate == false)
                {
                    return;
                }

                if(SystemAPI.HasComponent<ClassUsage>(request.Item) == false)
                {
                    return;
                }

                var item = SystemAPI.GetComponent<Item>(request.Item);

                if(SystemAPI.HasComponent<CharacterClassData>(item.Owner) == false)
                {
                    request.State = ActivateItemRequestState.Cancelled;
                    return;
                }

                var characterClass = SystemAPI.GetComponent<CharacterClassData>(item.Owner);
                var usage = SystemAPI.GetComponent<ClassUsage>(request.Item);

                if(usage.HasFlag(characterClass.Value) == false)
                {
                    request.State = ActivateItemRequestState.Cancelled;
                } 

            }).Schedule();


            Entities.ForEach((ref ActivateItemRequest request) =>
            {
                if(request.State != ActivateItemRequestState.Processing)
                {
                    return;
                }

                var item = SystemAPI.GetComponent<Item>(request.Item);
                
                if(SystemAPI.HasComponent<CharacterEquipment>(item.Owner) == false)
                {
                    return;
                }

                var equipment = SystemAPI.GetComponent<CharacterEquipment>(item.Owner);

                bool equipmentChanged = false;

                if(SystemAPI.HasComponent<ArmorSet>(request.Item))
                {
                    if(request.Activate)
                    {
                        if(equipment.ArmorSet != Entity.Null
                            && equipment.ArmorSet != request.Item
                            && SystemAPI.HasComponent<ActivatedState>(equipment.ArmorSet))
                        {
                            SystemAPI.SetComponent(equipment.ArmorSet, new ActivatedState(false));
                        }

                        equipment.ArmorSet = request.Item;
                        equipmentChanged = true;
                    }
                    else
                    {
                        // нельзя деактивировать активный армор сет, только заменить на другой
                        if(equipment.ArmorSet == request.Item)
                        {
                            //#if UNITY_EDITOR
                            //UnityEngine.Debug.Log($"Нельзя деактивировать активный армор сет!");
                            //#endif
                            request.State = ActivateItemRequestState.Cancelled;
                        }
                    }
                }

                if(SystemAPI.HasComponent<OneHandedItem>(request.Item))
                {
                    if(request.Activate)
                    {
                        if(equipment.RightHandWeapon != Entity.Null
                            && equipment.RightHandWeapon != request.Item
                            && SystemAPI.HasComponent<ActivatedState>(equipment.RightHandWeapon))
                        {
                            SystemAPI.SetComponent(equipment.RightHandWeapon, new ActivatedState(false));
                        }

                        equipment.RightHandWeapon = request.Item;
                        equipmentChanged = true;
                    }
                    else
                    {
                        // нельзя деактивировать активное оружие, только заменить на другое
                        if(equipment.RightHandWeapon == request.Item)
                        {
                            request.State = ActivateItemRequestState.Cancelled;
                        }
                    }
                }
                
                if(SystemAPI.HasComponent<Shield>(request.Item))
                {
                    if(request.Activate)
                    {
                        if(equipment.LeftHandShield != Entity.Null
                           && equipment.LeftHandShield != request.Item
                           && SystemAPI.HasComponent<ActivatedState>(equipment.LeftHandShield))
                        {
                            SystemAPI.SetComponent(equipment.LeftHandShield, new ActivatedState(false));
                        }

                        equipment.LeftHandShield = request.Item;
                        equipmentChanged = true;
                    }
                    else
                    {
                        if(equipment.LeftHandShield == request.Item)
                        {
                            equipment.LeftHandShield = Entity.Null;
                            equipmentChanged = true;
                        }
                    }
                }
                
                if(SystemAPI.HasComponent<Bow>(request.Item))
                {
                    if(request.Activate)
                    {
                        if(equipment.LeftHandBow != Entity.Null
                           && equipment.LeftHandBow != request.Item
                           && SystemAPI.HasComponent<ActivatedState>(equipment.LeftHandBow))
                        {
                            SystemAPI.SetComponent(equipment.LeftHandBow, new ActivatedState(false));
                        }

                        equipment.LeftHandBow = request.Item;
                        equipmentChanged = true;
                    }
                    else
                    {
                        // нельзя деактивировать активное оружие, только заменить на другое
                        if(equipment.LeftHandBow == request.Item)
                        {
                            request.State = ActivateItemRequestState.Cancelled;
                        }
                    }
                }

                if(equipmentChanged)
                {
                    SystemAPI.SetComponent(item.Owner, equipment);
                }

            }).Schedule();
        }
    }
}
