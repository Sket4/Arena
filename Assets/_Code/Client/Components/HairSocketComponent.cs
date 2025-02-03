using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct HairSocket : IComponentData
    {
        public Entity SocketEntity;
    }
    
    [UseDefaultInspector]
    public class HairSocketComponent : ComponentDataBehaviour<HairSocket>
    {
        public Transform SocketTransform;

        protected override void Bake<K>(ref HairSocket serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.SocketEntity = baker.GetEntity(SocketTransform);
        }
    }
}
