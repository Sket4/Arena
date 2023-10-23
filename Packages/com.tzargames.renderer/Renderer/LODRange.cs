using Unity.Entities;

namespace TzarGames.Renderer
{
    [System.Serializable]
    public struct LODRange : IComponentData
    {
        public float SquareDistanceMin;
        public float SquareDistanceMax;
    }
}