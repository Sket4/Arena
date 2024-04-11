using Unity.Entities;

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
        Success
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
    }
}