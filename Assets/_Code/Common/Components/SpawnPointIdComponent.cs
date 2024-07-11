using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    public struct SpawnPointIdData : IComponentData
    {
        public int ID;
    }

    [UseDefaultInspector(false)]
    public class SpawnPointIdComponent : ComponentDataBehaviour<SpawnPointIdData>
    {
        public SpawnPointID ID;

        protected override void Bake<K>(ref SpawnPointIdData serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.ID = ID ? ID.Id : 0;
        }
    }
}
