using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct TerrainPhysicsMaterial : ISharedComponentData, IEquatable<TerrainPhysicsMaterial>
    {
        public WeakObjectReference<Texture2D> LayerTextureRef;
        public byte Layer1;
        public byte Layer2;
        public byte Layer3;
        public byte Layer4;
        public Bounds Bounds;

        public float2 Trace(float3 worldPosition)
        {
            var min = Bounds.min;
            var max = Bounds.max;

            var x = (worldPosition.x - min.x) / (max.x - min.x);
            var z = (worldPosition.z - min.z) / (max.z - min.z);

            var result = new float2(x, z);
            result = math.saturate(result);
            return result;
        }

        public byte WorldPositionToLayer(float3 worldPosition)
        {
            if (LayerTextureRef.LoadingStatus == ObjectLoadingStatus.None)
            {
                LayerTextureRef.LoadAsync();
            }

            if (LayerTextureRef.LoadingStatus != ObjectLoadingStatus.Completed)
            {
                LayerTextureRef.WaitForCompletion();    
            }
            
            if (LayerTextureRef.LoadingStatus != ObjectLoadingStatus.Completed)
            {
                return 0;
            }
            
            var texture = LayerTextureRef.Result;

            if (texture == null)
            {
                Debug.LogError("null terrain texture");
            }
            
            var textureSpace = Trace(worldPosition);
            
            var pixel = texture.GetPixel((int)(textureSpace.x * texture.width), (int)(textureSpace.y * texture.height));

            if (pixel.r >= pixel.g 
                && pixel.r >= pixel.b 
                && pixel.r >= pixel.a)
            {
                return Layer1;
            }
            if (pixel.g >= pixel.r 
                && pixel.g >= pixel.b 
                && pixel.g >= pixel.a)
            {
                return Layer2;
            }
            
            if (pixel.b >= pixel.r 
                && pixel.b >= pixel.g 
                && pixel.b >= pixel.a)
            {
                return Layer3;
            }
            return Layer4;
        }

        public bool Equals(TerrainPhysicsMaterial other)
        {
            return LayerTextureRef.Equals(other.LayerTextureRef) && Layer1 == other.Layer1 && Layer2 == other.Layer2 && Layer3 == other.Layer3 && Layer4 == other.Layer4 && Bounds.Equals(other.Bounds);
        }

        public override bool Equals(object obj)
        {
            return obj is TerrainPhysicsMaterial other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LayerTextureRef, Layer1, Layer2, Layer3, Layer4, Bounds);
        }
    }
    
    [UseDefaultInspector(false)]
    public class TerrainPhysicsMaterialComponent : SharedComponentDataBehaviour<TerrainPhysicsMaterial>
    {
        public Texture2D LayerTexture;
        public Terrain Terrain;
        public CustomPhysicsMaterialTags Layer1;
        public CustomPhysicsMaterialTags Layer2;
        public CustomPhysicsMaterialTags Layer3;
        public CustomPhysicsMaterialTags Layer4;
        
        #if UNITY_EDITOR
        protected override void Bake<T1>(ref TerrainPhysicsMaterial data, T1 baker)
        {
            data.LayerTextureRef = new WeakObjectReference<Texture2D>(LayerTexture);
            data.Layer1 = Layer1.Value;
            data.Layer2 = Layer2.Value;
            data.Layer3 = Layer3.Value;
            data.Layer4 = Layer4.Value;

            data.Bounds = Terrain != null && Terrain.terrainData != null ? Terrain.terrainData.bounds : default;
        }
        #endif
    }
}

