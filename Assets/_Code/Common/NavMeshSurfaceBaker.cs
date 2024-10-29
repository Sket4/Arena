using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine.AI;

namespace Arena
{
    [System.Serializable]
    public sealed class NavMeshManagedData : IComponentData
    {
        public NavMeshData Data;
        public bool IsProcessed;
    }
    
    public class NavMeshSurfaceBaker : Baker<NavMeshSurface>
    {
        public override void Bake(NavMeshSurface authoring)
        {
            AddComponentObject(new NavMeshManagedData
            {
                Data = authoring.navMeshData
            });
        }
    }
}
