using System;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    public enum PurchaseRequestStatus : byte
    {
        InProcess = 0,
        InventoryValidation,
        
        // results
        UnknownError,
        NotEnoughMoney,
        StoreUnavailable,
        InventoryError,
        Success,
        CustomerCheckError,
        NoCharacterContact
    }

    public enum SellRequestStatus : byte
    {
        InProcess = 0,
        InventoryValidation,
        
        // results
        StoreUnavailable,
        NoInventoryError,
        InventoryError,
        Success,
        InvalidStore,
        WrongItemRequest,
        InvalidItem,
        InvalidItemPrice,
        NoItemInInventory,
        TotalPriceError,
        InvalidRequest,
        SellAlreadyInProcess,
        InvalidCharacter,
        UnknownError,
        SellerCheckError,
        NoCharacterContact
    }
    
    public struct PurchaseRequest : IComponentData
    {
        public Entity Customer;
        public Entity Store;
        public PurchaseRequestStatus Status;
        public System.Guid Guid;
        public Entity InventoryTransactionEntity;

        public static bool IsResultStatus(PurchaseRequestStatus resultState)
        {
            return resultState != PurchaseRequestStatus.InProcess
                   && resultState != PurchaseRequestStatus.InventoryValidation;
        }
    }

    public struct PurchaseRequest_Item : IBufferElementData
    {
        public int ItemID;
        public int Count;
        public Color Color;
    }

    public struct SellRequest : IComponentData
    {
        public Entity Seller;
        public Entity Store;
        public SellRequestStatus Status;
        public System.Guid Guid;
        public Entity InventoryTransactionEntity;

        public static bool IsResultStatus(SellRequestStatus status)
        {
            return status != SellRequestStatus.InProcess && status != SellRequestStatus.InventoryValidation;
        }
    }

    public struct SellRequest_Item : IBufferElementData
    {
        public Entity ItemEntity;
        public uint Count;
    }

    public struct SellRequest_NetItem
    {
        public NetworkID ID;
        public uint Count;
    }
}