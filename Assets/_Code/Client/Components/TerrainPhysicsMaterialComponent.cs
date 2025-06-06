using System;
using System.Text;
using TzarGames.GameCore;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct TerrainPhysicsMaterial : IComponentData, IEquatable<TerrainPhysicsMaterial>
    {
        public BlobAssetReference<BlobTexture> LayerTexture;
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
            return WorldPositionToLayer(worldPosition, out _);
        }

        public byte WorldPositionToLayer(float3 worldPosition, out float strength)
        {
            var textureSpace = Trace(worldPosition);
            
            var pixel = LayerTexture.Value.GetPixel32((int)(textureSpace.x * LayerTexture.Value.Width), (int)(textureSpace.y * LayerTexture.Value.Height));

            if (pixel.r >= pixel.g 
                && pixel.r >= pixel.b 
                && pixel.r >= pixel.a)
            {
                strength = pixel.r / (float) byte.MaxValue;
                return Layer1;
            }
            if (pixel.g >= pixel.r 
                && pixel.g >= pixel.b 
                && pixel.g >= pixel.a)
            {
                strength = pixel.g / (float) byte.MaxValue;
                return Layer2;
            }
            
            if (pixel.b >= pixel.r 
                && pixel.b >= pixel.g 
                && pixel.b >= pixel.a)
            {
                strength = pixel.b / (float) byte.MaxValue;
                return Layer3;
            }
            strength = pixel.a / (float) byte.MaxValue;
            return Layer4;
        }

        public bool Equals(TerrainPhysicsMaterial other)
        {
            return LayerTexture.Equals(other.LayerTexture) && Layer1 == other.Layer1 && Layer2 == other.Layer2 && Layer3 == other.Layer3 && Layer4 == other.Layer4 && Bounds.Equals(other.Bounds);
        }

        public override bool Equals(object obj)
        {
            return obj is TerrainPhysicsMaterial other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LayerTexture, Layer1, Layer2, Layer3, Layer4, Bounds);
        }
    }

    public struct BlobTexture
    {
        public int Width;
        public int Height;
        public BlobArray<Color32> Pixels;

        public Color GetPixel(int x, int y)
        {
            return Pixels[x + y*Width];
        }
        public Color32 GetPixel32(int x, int y)
        {
            return Pixels[x + y*Width];
        }
    }
    
    [UseDefaultInspector(false)]
    public class TerrainPhysicsMaterialComponent : ComponentDataBehaviour<TerrainPhysicsMaterial>
    {
        [Header("Не забудь про Rigidbody (kinematic,no gravity)")]
        public Texture2D LayerTexture;
        public Terrain Terrain;
        public CustomPhysicsMaterialTags Layer1;
        public CustomPhysicsMaterialTags Layer2;
        public CustomPhysicsMaterialTags Layer3;
        public CustomPhysicsMaterialTags Layer4;
        
        #if UNITY_EDITOR
        protected unsafe override void Bake<T1>(ref TerrainPhysicsMaterial data, T1 baker)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var blobData = ref blobBuilder.ConstructRoot<BlobTexture>();
            blobData.Width = LayerTexture.width;
            blobData.Height = LayerTexture.height;

            // var pix = LayerTexture.GetPixels();
            // var sb = new StringBuilder();
            // sb.Append('|');
            // int counter = 0;
            // foreach (var p in pix)
            // {
            //     sb.Append($"{p.r:F1} {p.g:F1} {p.b:F1} {p.a:F1}| ");
            //     counter++;
            //     if (counter == LayerTexture.width)
            //     {
            //         sb.Append(Environment.NewLine);
            //         counter = 0;
            //     }
            // }
            // Debug.Log(sb.ToString());

            var pixels = LayerTexture.GetPixels32();
            var arrayBuilder = blobBuilder.Allocate(ref blobData.Pixels, pixels.Length);

            fixed (void* ptr = pixels)
            {
                UnsafeUtility.MemCpy(arrayBuilder.GetUnsafePtr(), ptr, UnsafeUtility.SizeOf<Color32>() * pixels.Length);
            }

            var reference = blobBuilder.CreateBlobAssetReference<BlobTexture>(Allocator.Persistent);

            data.LayerTexture = reference;
            data.Layer1 = Layer1.Value;
            data.Layer2 = Layer2.Value;
            data.Layer3 = Layer3.Value;
            data.Layer4 = Layer4.Value;

            data.Bounds = Terrain != null && Terrain.terrainData != null ? Terrain.terrainData.bounds : default;
        }
        #endif
    }
}

