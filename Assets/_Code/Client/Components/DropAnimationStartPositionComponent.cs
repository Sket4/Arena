using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct DropAnimationStartPosition : IComponentData
    {
        public Entity PositionEntity;
    }

    [UseDefaultInspector]
    public class DropAnimationStartPositionComponent : ComponentDataBehaviour<DropAnimationStartPosition>
    {
        public GameObject Position;

        protected override void Bake<K>(ref DropAnimationStartPosition serializedData, K baker)
        {
            if(Position != null)
            {
                serializedData.PositionEntity = baker.GetEntity(Position);
            }
        }
    }
}
