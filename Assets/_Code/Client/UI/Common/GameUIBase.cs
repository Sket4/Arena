// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using Arena.Client;
using TzarGames.Common.UI;
using Unity.Entities;

namespace TzarGames.GameFramework.UI
{
    public class GameUIBase : UIBase
    {
        public Entity OwnerEntity { get; private set; }
        public Entity UIEntity { get; private set; }
        public EntityManager EntityManager { get; private set; }

        public bool HasData<T>()
        {
            return EntityManager.HasComponent<T>(OwnerEntity);
        }

        public bool HasData<T>(Entity entity)
        {
            return EntityManager.HasComponent<T>(entity);
        }

        public bool IsEnabled<T>() where T : IEnableableComponent
        {
            return IsEnabled<T>(OwnerEntity);
        }

        public bool IsEnabled<T>(Entity entity) where T : IEnableableComponent
        {
            return EntityManager.IsComponentEnabled<T>(entity);
        }

        public T GetData<T>(Entity entity) where T : unmanaged, IComponentData
        {
	        return EntityManager.GetComponentData<T>(entity);
        }

        public bool TryGetData<T>(out T data) where T : unmanaged, IComponentData
        {
            return TryGetData(OwnerEntity, out data);
        }

        public bool TryGetData<T>(Entity entity, out T data) where T : unmanaged, IComponentData
        {
            if(HasData<T>(entity) == false)
            {
                data = default;
                return false;
            }
            data = GetData<T>(entity);
            return true;
        }

        public T GetData<T>() where T : unmanaged, IComponentData
        {
	        return EntityManager.GetComponentData<T>(OwnerEntity);
        }

        public DynamicBuffer<T> GetBuffer<T>() where T : unmanaged, IBufferElementData
        {
	        return GetBuffer<T>(OwnerEntity);
        }

        public DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            return EntityManager.GetBuffer<T>(entity);
        }

        public T GetObjectData<T>()
        {
            return GetObjectData<T>(OwnerEntity);
        }
        
        public T GetObjectData<T>(Entity entity)
        {
            return EntityManager.GetComponentObject<T>(entity);
        }

        public T GetSharedComponentManaged<T>(Entity entity) where T : struct, ISharedComponentData
        {
            return EntityManager.GetSharedComponentManaged<T>(entity);
        }

        public void Setup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
		{
            if(OwnerEntity == ownerEntity)
            {
                return;
            }

            OwnerEntity = ownerEntity;
            EntityManager = manager;
            UIEntity = uiEntity;
            OnSetup(ownerEntity, uiEntity, manager);
		}
		
		protected virtual void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager) 
        {
        }

        public virtual void OnSystemUpdate(UISystem system)
        {
        }
	}	
}
