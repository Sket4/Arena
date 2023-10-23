using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Serialization.Binary;
using UnityEngine;
using UnityEngine.Rendering;

namespace TzarGames.Renderer
{
    [Serializable]
    struct RenderInfoWrapper
    {
        [Unity.Serialization.DontSerialize]
        public WeakObjectReference<Material> Material;

        [Unity.Serialization.DontSerialize]
        public WeakObjectReference<Mesh>[] MeshArray;
    }

    /// <summary>
    /// из-за бага при сериализации RenderInfo (если там присутствуют WeakObjectReference) пришлось написать кастомный сериализатор
    /// </summary>
    unsafe class RenderInfoWrapperAdapter : IBinaryAdapter<RenderInfoWrapper>
    {
        void IBinaryAdapter<RenderInfoWrapper>.Serialize(in BinarySerializationContext<RenderInfoWrapper> context, RenderInfoWrapper value)
        {
            var matId = RenderInfo.GetMaterialInternalId(value.Material);
            context.Writer->Add(matId);

            int meshArrayLen = value.MeshArray.Length;
            context.Writer->Add(meshArrayLen);

            for (int i = 0; i < meshArrayLen; i++)
            {
                var mesh = value.MeshArray[i];
                var id = RenderInfo.GetMeshInternalId(mesh);
                context.Writer->Add(id);
            }
        }

        RenderInfoWrapper IBinaryAdapter<RenderInfoWrapper>.Deserialize(in BinaryDeserializationContext<RenderInfoWrapper> context)
        {
            RenderInfoWrapper result = default;

            var matId = context.Reader->ReadNext<UntypedWeakReferenceId>();
            result.Material = new WeakObjectReference<Material>(matId);

            var meshArrayLen = context.Reader->ReadNext<int>();
            result.MeshArray = new WeakObjectReference<Mesh>[meshArrayLen];

            for (int i = 0; i < meshArrayLen; i++)
            {
                var id = context.Reader->ReadNext<UntypedWeakReferenceId>();
                result.MeshArray[i] = new WeakObjectReference<Mesh>(id);
            }

            return result;
        }

        static bool isRegistered = false;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void register()
        {
            if (isRegistered)
            {
                return;
            }
            isRegistered = true;

            BinarySerialization.AddGlobalAdapter(new RenderInfoWrapperAdapter());
        }
    }

    [Serializable]
    public struct RenderInfo : ISharedComponentData, IEquatable<RenderInfo>
    {
        [SerializeField] private RenderInfoWrapper meshArrayWrapper;
        [SerializeField] private uint4 m_Hash128;
        public WeakObjectReference<Material> Material 
        {
            get => meshArrayWrapper.Material;
            set => meshArrayWrapper.Material = value;
        }
        public WeakObjectReference<Mesh>[] MeshArray
        {
            get
            {
                return meshArrayWrapper.MeshArray;
            }
            set => meshArrayWrapper.MeshArray = value;
        }
        
        public bool Equals(RenderInfo other)
        {
            return math.all(m_Hash128 == other.m_Hash128);
        }

        public override bool Equals(object obj)
        {
            return obj is RenderInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)m_Hash128.x;
        }

        public void ResetHash128()
        {
            m_Hash128 = ComputeHash128();
        }
        
        public uint4 ComputeHash128()
        {
            var hash = new xxHash3.StreamingState(false);

            int numMeshes = MeshArray?.Length ?? 0;
            int numMaterials = 1;//m_Materials?.Length ?? 0;

            hash.Update(numMeshes);
            hash.Update(numMaterials);

            for (int i = 0; i < numMeshes; ++i)
                UpdateAsset(ref hash, GetMeshInternalId(MeshArray[i]));

            // for (int i = 0; i < numMaterials; ++i)
            //     UpdateAsset(ref hash, m_Materials[i]);
            
            UpdateAsset(ref hash, GetMaterialInternalId(Material));

            uint4 H = hash.DigestHash128();

            // Make sure the hash is never exactly zero, to keep zero as a null value
            if (math.all(H == uint4.zero))
                return new uint4(1, 0, 0, 0);

            return H;
        }

        static readonly System.Reflection.FieldInfo materialIdField = typeof(WeakObjectReference<Material>).GetField("Id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        static readonly System.Reflection.FieldInfo meshIdField = typeof(WeakObjectReference<Mesh>).GetField("Id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static UntypedWeakReferenceId GetMaterialInternalId(object value)
        {
            return (UntypedWeakReferenceId)materialIdField.GetValue(value);
        }
        public static UntypedWeakReferenceId GetMeshInternalId(object value)
        {
            return (UntypedWeakReferenceId)meshIdField.GetValue(value);
        }

        static void UpdateAsset(ref xxHash3.StreamingState hash, UntypedWeakReferenceId id)
        {
            hash.Update(id);

            //            // In the editor we can compute a stable serializable hash using an asset GUID
            //#if UNITY_EDITOR
            //            bool success = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
            //            hash.Update(success);
            //            if (!success)
            //            {
            //                hash.Update(asset.GetInstanceID());
            //                return;
            //            }
            //            var guidBytes = Encoding.UTF8.GetBytes(guid);

            //            hash.Update(guidBytes.Length);
            //            for (int j = 0; j < guidBytes.Length; ++j)
            //                hash.Update(guidBytes[j]);
            //            hash.Update(localId);
            //#else
            //            // In standalone, we have to resort to using the instance ID which is not serializable,
            //            // but should be usable in the context of this execution.
            //            hash.Update(asset.GetInstanceID());
            //#endif
        }

        public override string ToString()
        {
            if(Material.IsReferenceValid == false)
            {
                return "Invalid material reference";
            }

            if(Material.LoadingStatus == ObjectLoadingStatus.Completed)
            {
                var mat = Material.Result;

                if(mat != null)
                {
                    return mat.ToString();
                }
                else
                {
                    return "null material";
                }
            }
            else
            {
                return "not loaded " + Material.ToString();
            } 
        }
    }


    public struct MeshInfoLoadingStatus : IComponentData, IEnableableComponent
    {
    }
    
    [MaximumChunkCapacity(128)]
    [Serializable]
    public struct MeshInfo : IComponentData, IEquatable<MeshInfo>
    {
        public BatchMeshID MeshID;
        public int MeshIndex;
        public byte SubMeshIndex;

        public bool Equals(MeshInfo other)
        {
            return MeshID == other.MeshID && SubMeshIndex == other.SubMeshIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is MeshInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MeshIndex, SubMeshIndex);
        }

        public bool HasValidMeshID => MeshID != BatchMeshID.Null;
    }
    
    public struct WorldRenderBounds : IComponentData
    {
        public AABB Value;
    }
    
    public struct LocalRenderBounds : IComponentData
    {
        public AABB Value;
    }
    
    [Serializable]
    public struct ReflectionProbeData : ICleanupComponentData
    {
        public bool IsInitialized;
        public int Index;
    }
}