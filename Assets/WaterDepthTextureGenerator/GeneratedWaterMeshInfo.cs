using UnityEngine;

namespace TzarGames.Editor.WaterMeshGenerator
{
    [System.Serializable]
    public struct WaterMeshGeneratorData
    {
        public float Width;
        public float Height;
        public int WidthDivisions;
        public int HeightDivisions;
        public float FoamWidth;
        public LayerMask TraceLayers;
        [Range(0,3)]
        public int ColorChannelToWrite;
        [Range(0,3)]
        public int DepthChannelToWrite;
        public bool InverseColor;
        public float DepthTraceDistance;
    }

    public class GeneratedWaterMeshInfo : MonoBehaviour
    {
        public Mesh Mesh;
        public WaterMeshGeneratorData Data = new WaterMeshGeneratorData
        {
            Width = 100,
            Height = 100,
            HeightDivisions = 100,
            WidthDivisions = 100,
            FoamWidth = 2, 
            ColorChannelToWrite = 0,
            DepthChannelToWrite = 1,
            DepthTraceDistance = 1000,
        };

        private void Reset()
        {
            var mf = GetComponent<MeshFilter>();
            if(mf != null)
            {
                Mesh = mf.sharedMesh;
            }
        }
    }
}
