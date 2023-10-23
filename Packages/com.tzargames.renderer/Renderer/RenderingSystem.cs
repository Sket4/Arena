using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Deformations;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace TzarGames.Renderer
{
    [Flags]
    enum ChunkGraphicsDataFlags : byte
    {
        None = 0,
        Transparent = 1 << 0
    }

    struct ChunkGraphicsData : IComponentData
    {
        public BatchMaterialID MaterialID;
        public ChunkGraphicsDataFlags Flags;
    }

    struct DisabledHybridComponentTag : IComponentData
    {
    }

    // Unity provided shaders such as Universal Render Pipeline/Lit expect
    // unity_ObjectToWorld and unity_WorldToObject in a special packed 48 byte
    // format when the DOTS_INSTANCING_ON keyword is enabled.
    // This saves both GPU memory and GPU bandwidth.
    // We define a convenience type here so we can easily convert into this format.
    struct PackedMatrix
    {
        public float c0x;
        public float c0y;
        public float c0z;
        public float c1x;
        public float c1y;
        public float c1z;
        public float c2x;
        public float c2y;
        public float c2z;
        public float c3x;
        public float c3y;
        public float c3z;

        public PackedMatrix(Matrix4x4 m)
        {
            c0x = m.m00;
            c0y = m.m10;
            c0z = m.m20;
            c1x = m.m01;
            c1y = m.m11;
            c1z = m.m21;
            c2x = m.m02;
            c2y = m.m12;
            c2z = m.m22;
            c3x = m.m03;
            c3y = m.m13;
            c3z = m.m23;
        }

        public const int SizeInBytes = sizeof(float) * 12;
    }

    struct SkinningData
    {
        public uint4 Value;
        public static readonly int SizeInBytes = UnsafeUtility.SizeOf<SkinningData>();
    }

    struct ChunkBatchData
    {
        public int BufferHandle;
        public BatchID BatchID;
        public int InstanceCount;
        public bool NeedRecreateBatch;
        public int ActualSizeInBytes;
    }

    //[UpdateAfter(typeof(SkinnedMeshAnimationSystem))]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class RenderingSystem : SystemBase
    {
        public bool LogChangingData = false;
        public int LoadingMaterialCount { get; private set; } = 0;
        public int LoadingMeshCount { get; private set; } = 0;

        EntityQuery notRegisteredMeshesQuery;
        EntityQuery batchedMeshesQuery;
        EntityQuery skinMatrixQuery;
        EntityQuery changedRenderInfoQuery;

        EntityQuery disabledParticlesQuery;
        EntityQuery nonDisabledParticlesQuery;

        SharedComponentTypeHandle<RenderInfo> renderInfoTypeHandle;
        ComponentTypeHandle<ChunkGraphicsData> chunkGraphicsDataHandle;

        BatchRendererGroup brg;
        private ulong chunkIdCount = 1;
        Dictionary<int, CircularGraphicsBuffer> graphicBuffers = new Dictionary<int, CircularGraphicsBuffer>();
        private List<int> freeGraphicBuffers = new();
        NativeHashMap<WeakObjectReference<Mesh>, BatchMeshID> registeredMeshes = new();
        NativeHashMap<WeakObjectReference<Material>, BatchMaterialID> registeredMaterials = new();

        private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;
        private int BufferOffset => 0;
        private int BufferWindowSize => UseConstantBuffer ? BatchRendererGroup.GetConstantBufferMaxWindowSize() : 0;

        private GraphicsBuffer.Target graphicsBufferTarget;

        private const int SizeOfMatrix = sizeof(float) * 4 * 4;
        private const int BufferExtraBytes = SizeOfMatrix * 2;
        private const int BytesForMainMatrices = PackedMatrix.SizeInBytes * 2;
        private static readonly int SkinMatrixSize = UnsafeUtility.SizeOf<SkinMatrix>();

        JobHandle cullingJobDependency = default;

        NativeParallelMultiHashMap<ulong, ChunkBatchData> chunkBatchDataMap;
        private List<int> freeReflectionProbes = new();
        private BeginPresentationEntityCommandBufferSystem commandBufferSystem;

        Unity.Profiling.ProfilerMarker releasingBuffersMarker = new Unity.Profiling.ProfilerMarker("Releasing buffers");
        Unity.Profiling.ProfilerMarker updatingChunksMarker = new Unity.Profiling.ProfilerMarker("Updating chunks");
        Unity.Profiling.ProfilerMarker writeChangedDataMarker = new Unity.Profiling.ProfilerMarker("Writing changed data");
        Unity.Profiling.ProfilerMarker preparingMatricesMarker = new Unity.Profiling.ProfilerMarker("Preparing matrices");

        bool isWorking = true;

        protected override void OnCreate()
        {
            base.OnCreate();
            var constantBufferSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();

            Debug.Log("GPU instancing support " + SystemInfo.supportsInstancing);
            Debug.Log($"System const size: {SystemInfo.maxConstantBufferSize}, BRG constant size: {constantBufferSize}");

            notRegisteredMeshesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RenderInfo>(), ComponentType.ReadOnly<LocalToWorld>() },
                None = new[] { ComponentType.ChunkComponent<ChunkGraphicsData>() },
            });

            batchedMeshesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new []
                {
                    ComponentType.ReadOnly<RenderInfo>(), 
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ChunkComponentReadOnly<ChunkGraphicsData>()    
                }
            });

            changedRenderInfoQuery = GetEntityQuery(ComponentType.ReadOnly<RenderInfo>());
            changedRenderInfoQuery.AddChangedVersionFilter(ComponentType.ReadOnly<RenderInfo>());

            skinMatrixQuery = GetEntityQuery(ComponentType.ReadOnly<SkinMatrix>(), ComponentType.ChunkComponent<ChunkGraphicsData>());
            //skinMatrixQuery.AddChangedVersionFilter(typeof(SkinMatrix));

            renderInfoTypeHandle = GetSharedComponentTypeHandle<RenderInfo>();
            chunkGraphicsDataHandle = GetComponentTypeHandle<ChunkGraphicsData>();

            registeredMeshes = new NativeHashMap<WeakObjectReference<Mesh>, BatchMeshID>(128, Allocator.Persistent);
            registeredMaterials = new NativeHashMap<WeakObjectReference<Material>, BatchMaterialID>(64, Allocator.Persistent);

            initBRG();

            graphicsBufferTarget = GraphicsBuffer.Target.Raw;
            if (SystemInfo.graphicsDeviceType is GraphicsDeviceType.OpenGLCore or GraphicsDeviceType.OpenGLES3)
                graphicsBufferTarget |= GraphicsBuffer.Target.Constant;

            chunkBatchDataMap = new NativeParallelMultiHashMap<ulong, ChunkBatchData>(1024, Allocator.Persistent);

            for (int i = 0; i < 16; i++)
            {
                freeReflectionProbes.Add(i);
            }

            commandBufferSystem = World.GetExistingSystemManaged<BeginPresentationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            isWorking = false;
            destroyBRG();
        }

        void initBRG()
        {
            brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
        }

        void destroyBRG()
        {
            brg.Dispose();

            foreach(var kv in registeredMeshes)
            {
                kv.Key.Release();
            }
            registeredMeshes.Dispose();

            foreach(var kv in registeredMaterials)
            {
                //Debug.Log($"Releasing material {material}");
                kv.Key.Release();
            }
            registeredMaterials.Dispose();

            Debug.Log($"Cleaning up {graphicBuffers.Count} graphic buffers");
            foreach(var buff in graphicBuffers.Values)
            {
                buff.Dispose();
            }
            if(chunkBatchDataMap.IsCreated)
            {
                chunkBatchDataMap.Dispose();
            }
        }

        bool tryGetFreeGraphicsBuffer(out CircularGraphicsBuffer buffer, out int handle)
        {
            if (freeGraphicBuffers.Count == 0)
            {
                buffer = default;
                handle = -1;
                return false;
            }

            handle = freeGraphicBuffers[0];
            freeGraphicBuffers.RemoveAt(0);
            return graphicBuffers.TryGetValue(handle, out buffer);
        }

        void releaseGraphicsBuffer(int handle)
        {
            if (handle == -1)
            {
                return;
            }
            //Debug.Log($"Releasing graphics buffer {handle}");
            if (freeGraphicBuffers.Contains(handle))
            {
                return;
            }
            freeGraphicBuffers.Add(handle);
        }

        [BurstCompile]
        struct CheckBatchesForReleaseJob : IJob
        {
            public NativeParallelMultiHashMap<ulong, ChunkBatchData> ChunkBatchDataMap;
            public NativeArray<ArchetypeChunk> BatchedChunks;
            public NativeList<ChunkBatchData> RemoveList;

            public void Execute()
            {
                var batchDataMapKeyArray = ChunkBatchDataMap.GetKeyArray(Allocator.Temp);
                var batchKeysCount = batchDataMapKeyArray.Length;
                var batchChunkCount = BatchedChunks.Length;

                for (int i = 0; i < batchKeysCount; i++)
                {
                    var chunkId = batchDataMapKeyArray[i];

                    bool contains = false;

                    for (int c = 0; c < batchChunkCount; c++)
                    {
                        var batchedChunk = BatchedChunks[c];

                        if (batchedChunk.SequenceNumber == chunkId)
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (contains)
                    {
                        continue;
                    }

                    var enumerator = ChunkBatchDataMap.GetValuesForKey(chunkId);

                    while (enumerator.MoveNext())
                    {
                        var bData = enumerator.Current;

                        RemoveList.Add(bData);
                    }

                    ChunkBatchDataMap.Remove(chunkId);
                }
            }
        }

        [BurstCompile]
        struct PrepareMatricesJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<LocalToWorld> L2WArray;

            public NativeArray<PackedMatrix> L2WPackedArray;
            public NativeArray<PackedMatrix> L2WPackedInverseArray;

            public void Execute(int index)
            {
                var m = L2WArray[index].Value;

                L2WPackedArray[index] = new PackedMatrix(m);
                L2WPackedInverseArray[index] = new PackedMatrix(math.inverse(m));
            }
        }
        
        async void registerMaterialAsync(WeakObjectReference<Material> material)
        {
            LoadingMaterialCount++;
            
            try
            {   
                if(registeredMaterials.ContainsKey(material))
                {
                    return;
                }
                registeredMaterials.Add(material, BatchMaterialID.Null);
                material.LoadAsync();

                while (material.LoadingStatus != ObjectLoadingStatus.Completed)
                {
                    if(isWorking == false)
                    {
                        return;
                    }
                    await Task.Yield();
                }

                var materialID = brg.RegisterMaterial(material.Result);
                registeredMaterials[material] = materialID;

                if (LogChangingData)
                {
                    Debug.Log($"Registered material {material} with id {materialID.value}");
                }
            }
            finally
            {
                LoadingMaterialCount--;
            }
        }

        async void registerMeshAsync(WeakObjectReference<Mesh> mesh)
        {
            LoadingMeshCount++;
            
            try
            {
                if(registeredMeshes.ContainsKey(mesh))
                {
                    return;
                }
                registeredMeshes.Add(mesh, BatchMeshID.Null);
                mesh.LoadAsync();

                while (mesh.LoadingStatus != ObjectLoadingStatus.Completed)
                {
                    if (isWorking == false)
                    {
                        return;
                    }
                    await Task.Yield();
                }

                //Debug.Log($"register mesh {mesh.Result} {mesh}");

                var meshID = brg.RegisterMesh(mesh.Result);
                registeredMeshes[mesh] = meshID;
            }
            finally
            {
                LoadingMeshCount--;
            }
        }

        void registerMeshesAndMaterials()
        {
            if (changedRenderInfoQuery.CalculateChunkCount() > 0)
            {
                var changedRenderInfoChunks = changedRenderInfoQuery.ToArchetypeChunkArray(Allocator.Temp);

                foreach (var chunk in changedRenderInfoChunks)
                {
                    var renderInfo = chunk.GetSharedComponentManaged(renderInfoTypeHandle, EntityManager);

                    registerMaterialAsync(renderInfo.Material);

                    if (renderInfo.MeshArray != null)
                    {
                        foreach (var mesh in renderInfo.MeshArray)
                        {
                            registerMeshAsync(mesh);
                        }
                    }
                }
            }

            var meshInfoLoadingLookup = GetComponentLookup<MeshInfoLoadingStatus>(false);

            Entities
                .WithAll<MeshInfoLoadingStatus>()
                .WithoutBurst()
                .ForEach((Entity entity, ref MeshInfo meshInfo, in RenderInfo renderInfo) =>
                {
                    var mesh = renderInfo.MeshArray[meshInfo.MeshIndex];

                    if (registeredMeshes.TryGetValue(mesh, out BatchMeshID meshID))
                    {
                        if(meshID != BatchMeshID.Null)
                        {
                            meshInfo.MeshID = meshID;
                            meshInfoLoadingLookup.SetComponentEnabled(entity, false);
                        }
                    }
                })
                .Run();

            Entities
                .WithChangeFilter<MeshInfo>()
                .WithoutBurst()
                .ForEach((Entity entity, ref MeshInfo meshInfo, in RenderInfo renderInfo) =>
                {
                    var mesh = renderInfo.MeshArray[meshInfo.MeshIndex];

                    if(registeredMeshes.TryGetValue(mesh, out BatchMeshID meshID) == false || meshID == BatchMeshID.Null)
                    {
                        if(meshInfoLoadingLookup.IsComponentEnabled(entity) == false)
                        {
                            meshInfoLoadingLookup.SetComponentEnabled(entity, true);
                        }
                    }
                    else 
                    {
                        meshInfo.MeshID = registeredMeshes[mesh];
                    }
                })
                .Run();


            //cmdBuffer.Playback(EntityManager);
        }

        struct MeshLoadWait : IComponentData
        {
            public Entity TargetEntity;
            public WeakObjectReference<Mesh> Reference;
        }

        protected override unsafe void OnUpdate()
        {
            var sysVersion = LastSystemVersion;

            Dependency = JobHandle.CombineDependencies(Dependency, cullingJobDependency);
            
            updateReflectionProbes();
            updateParticles();
            
            if (notRegisteredMeshesQuery.IsEmpty == false)
            {
                // adding empty chunk components
                EntityManager.AddChunkComponentData(notRegisteredMeshesQuery, new ChunkGraphicsData());
            }

            renderInfoTypeHandle.Update(this);
            chunkGraphicsDataHandle.Update(this);

            // init or update instance buffers
            var l2wType = GetComponentTypeHandle<LocalToWorld>(true);
            var skinDataType = GetBufferTypeHandle<SkinMatrix>(true);
            //var entityTypeHandle = GetEntityTypeHandle();

            // TODO
            skinMatrixQuery.CompleteDependency();

            registerMeshesAndMaterials();

            using (var batchedChunks = batchedMeshesQuery.ToArchetypeChunkArray(Allocator.TempJob))
            {
                releasingBuffersMarker.Begin();

                var checkForReleaseJob = new CheckBatchesForReleaseJob
                {
                    BatchedChunks = batchedChunks,
                    ChunkBatchDataMap = chunkBatchDataMap,
                    RemoveList = new NativeList<ChunkBatchData>(16, Allocator.TempJob)
                };

                checkForReleaseJob.Run();

                foreach(var bData in checkForReleaseJob.RemoveList)
                {
                    if (bData.BufferHandle != -1)
                    {
                        releaseGraphicsBuffer(bData.BufferHandle);
                    }

                    if (bData.BatchID != BatchID.Null)
                    {
                        brg.RemoveBatch(bData.BatchID);
                    }
                }

                releasingBuffersMarker.End();

                chunkGraphicsDataHandle.Update(this);

                // update material IDs
                foreach (var chunk in batchedChunks)
                {
                    var chunkGraphicsData = chunk.GetChunkComponentData(ref chunkGraphicsDataHandle);

                    if(chunkGraphicsData.MaterialID != BatchMaterialID.Null)
                    {
                        continue;
                    }

                    var renderInfo = chunk.GetSharedComponentManaged(renderInfoTypeHandle, EntityManager);
                    var material = renderInfo.Material.Result;

                    if(material == null)
                    {
                        continue;
                    }

                    if(registeredMaterials.TryGetValue(renderInfo.Material, out BatchMaterialID materialID) == false)
                    {
                        continue;
                    }

                    if(materialID == BatchMaterialID.Null)
                    {
                        continue;
                    }

                    chunkGraphicsData.MaterialID = registeredMaterials[renderInfo.Material];

                    if (material.renderQueue > (int)RenderQueue.GeometryLast)
                    {
                        chunkGraphicsData.Flags |= ChunkGraphicsDataFlags.Transparent;
                    }

                    chunk.SetChunkComponentData(ref chunkGraphicsDataHandle, chunkGraphicsData);
                }

                // checking and updating buffers
                updatingChunksMarker.Begin();

                foreach (var chunk in batchedChunks)
                {
                    var chunkID = chunk.SequenceNumber;

                    var l2wChanged = chunk.DidChange(ref l2wType, sysVersion);
                    var skinChanged = chunk.DidChange(ref skinDataType, sysVersion);
                    var containsBatchMap = chunkBatchDataMap.ContainsKey(chunkID);

                    if (l2wChanged == false 
                        && skinChanged == false
                        && containsBatchMap
                       )
                    {
                        continue;
                    }

                    var chunkGraphicsData = chunk.GetChunkComponentData(ref chunkGraphicsDataHandle);

                    if (LogChangingData)
                    {
                        Debug.Log($"Graphics chunk data changed, material ID: {chunkGraphicsData.MaterialID.value}");
                    }

                    if (chunkGraphicsData.MaterialID == BatchMaterialID.Null)
                    {
                        continue;
                    }

                    var instanceCount = chunk.Count;
                    var hasSkinMatrices = chunk.Has(ref skinDataType);
                    
                    BufferAccessor<SkinMatrix> skinMatrixBufferAccessor = default;

                    if(hasSkinMatrices)
                    {
                        skinMatrixBufferAccessor = chunk.GetBufferAccessor(ref skinDataType);
                    }

                    var batchList = new NativeList<ChunkBatchData>(16, Allocator.Temp);
                    var maxBufferSizeInBytes = BatchRendererGroup.GetConstantBufferMaxWindowSize();
                    var maxBufferCount = maxBufferSizeInBytes / sizeof(int);

                    // инициализируем батчи и считаем их размеры
                    int batchIndex = 0;

                    int batchDataSizeCounter = BufferExtraBytes;
                        int batchInstanceCounter = 0;
                        
                        for (int i=0; i<instanceCount; i++)
                        {
                            batchInstanceCounter++;

                            int sizeForInstance = BytesForMainMatrices;

                            if(hasSkinMatrices)
                            {
                                sizeForInstance += SkinningData.SizeInBytes;

                                var skinMatrices = skinMatrixBufferAccessor[i];
                                sizeForInstance += skinMatrices.Length * SkinMatrixSize;
                            }

                            if(sizeForInstance == 0)
                            {
                                continue;
                            }
                        
                            if (batchDataSizeCounter + sizeForInstance > maxBufferSizeInBytes)
                            {
                                batchInstanceCounter = 1;
                                batchDataSizeCounter = sizeForInstance + BufferExtraBytes;

                                batchList.Add(default);

                                batchIndex++;
                            }
                            else
                            {
                                batchDataSizeCounter += sizeForInstance;
                            }

                            var data = new ChunkBatchData
                            {
                                ActualSizeInBytes = batchDataSizeCounter,
                                InstanceCount = batchInstanceCounter
                            };

                            if (batchList.Length == 0)
                            {
                                batchList.Add(data);
                            }
                            else
                            {
                                batchList[batchIndex] = data;
                            }
                        }

                    // обновляем батчи
                    batchIndex = 0;

                    // удаляем лишние батчи
                    if(chunkBatchDataMap.TryGetFirstValue(chunkID, out ChunkBatchData batchData, out NativeParallelMultiHashMapIterator<ulong> it))
                    {
                        do
                        {
                            if (batchIndex >= batchList.Length)
                            {
                                if (batchData.BatchID != BatchID.Null)
                                {
                                    //Debug.Log($"Removing batch with id {batchData.BatchID.value}");
                                    brg.RemoveBatch(batchData.BatchID);

                                    releaseGraphicsBuffer(batchData.BufferHandle);
                                    batchData.BufferHandle = -1;
                                    
                                    batchData.BatchID = BatchID.Null;
                                    batchData.InstanceCount = 0;
                                    batchData.ActualSizeInBytes = 0;
                                    
                                    chunkBatchDataMap.SetValue(batchData, it);
                                }
                            }
                            batchIndex++;
                        }
                        while (chunkBatchDataMap.TryGetNextValue(out batchData, ref it));
                    }

                    if(batchList.Length == 0)
                    {
                        continue;
                    }

                    // добавляем новые батчи, если необходимо
                    var currentBatchCount = chunkBatchDataMap.CountValuesForKey(chunkID);
                    var batchCountToAdd = batchList.Length - currentBatchCount;

                    if(batchCountToAdd > 0)
                    {
                        for(int i=0; i<batchCountToAdd; i++)
                        {
                            chunkBatchDataMap.Add(chunkID, new ChunkBatchData
                            {
                                ActualSizeInBytes = 0,
                                BatchID = BatchID.Null,
                                BufferHandle = -1,
                                InstanceCount = 0,
                                NeedRecreateBatch = true
                            });
                        }
                    }

                    batchIndex = 0;

                    // создаем буферы для батчей
                    if(chunkBatchDataMap.TryGetFirstValue(chunkID, out batchData, out it))
                    {
                        do
                        {
                            if(batchIndex >= batchList.Length)
                            {
                                break;
                            }

                            var newBatchData = batchList[batchIndex];

                            batchData.ActualSizeInBytes = newBatchData.ActualSizeInBytes;
                            batchData.NeedRecreateBatch = batchData.InstanceCount != newBatchData.InstanceCount;
                            batchData.InstanceCount = newBatchData.InstanceCount;

                            if (graphicBuffers.TryGetValue(batchData.BufferHandle, out CircularGraphicsBuffer graphicsBuffer) == false)
                            {
                                int handle;
                                
                                if (tryGetFreeGraphicsBuffer(out graphicsBuffer, out handle))
                                {
                                    //Debug.Log($"Getting free graphics buffer for chunk {chunkID}, actual data size for batch: {newBatchData.ActualSizeInBytes}");
                                }
                                else
                                {
                                    //Debug.Log($"Creating graphics buffer for chunk {chunkID}, actual data size: {newBatchData.ActualSizeInBytes}");
                                    graphicsBuffer = new CircularGraphicsBuffer(graphicsBufferTarget, GraphicsBuffer.UsageFlags.LockBufferForWrite, maxBufferCount, sizeof(int));
                                    handle = graphicBuffers.Count;
                                    graphicBuffers.Add(handle, graphicsBuffer);
                                }
                                
                                batchData.BufferHandle = handle;
                            }

                            chunkBatchDataMap.SetValue(batchData, it);
                            batchList[batchIndex] = batchData;

                            batchIndex++;
                        }
                        while (chunkBatchDataMap.TryGetNextValue(out batchData, ref it));
                    }

                    //Debug.Log($"Updating batches for chunk, num of instances: {chunk.Count}");

                    // обновляем данные в буферах
                    preparingMatricesMarker.Begin();

                    var l2wArray = chunk.GetNativeArray(ref l2wType);
                    var l2wPackedArray = new NativeArray<PackedMatrix>(instanceCount, Allocator.TempJob);
                    var l2wPackedAddr = (PackedMatrix*)l2wPackedArray.GetUnsafePtr();
                    var l2wPackedInverseArray = new NativeArray<PackedMatrix>(instanceCount, Allocator.TempJob);
                    var l2wPackedInverseAddr = (PackedMatrix*)l2wPackedInverseArray.GetUnsafePtr();

                    var prepareMatricesJob = new PrepareMatricesJob
                    {
                        L2WArray = l2wArray,
                        L2WPackedArray = l2wPackedArray,
                        L2WPackedInverseArray = l2wPackedInverseArray
                    };

                    prepareMatricesJob.Run(instanceCount);

                    preparingMatricesMarker.End();

                    int readOffset = 0;

                    writeChangedDataMarker.Begin();

                    for(int b=0; b<batchList.Length; b++)
                    {
                        var batch = batchList[b];

                        var bytesPerInstance = BytesForMainMatrices;

                        if (hasSkinMatrices)
                        {
                            bytesPerInstance += SkinningData.SizeInBytes;
                        }

                        //var bufferCountToWrite = BufferCountForInstances(bytesPerInstance, batch.InstanceCount, BufferExtraBytes);
                        var bufferCountToWrite = ((bytesPerInstance * batch.InstanceCount) + BufferExtraBytes + sizeof(int) - 1) / sizeof(int);

                        uint byteAddressObjectToWorld = PackedMatrix.SizeInBytes * 2;
                        uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(PackedMatrix.SizeInBytes * batch.InstanceCount);

                        uint byteAddressForSkinDatas = 0;
                        uint startAddressForSkinMatrices = 0;

                        NativeArray<SkinningData> skinningDataArray = default;
                        var skinMatrixSizeInBytes = UnsafeUtility.SizeOf<SkinMatrix>();

                        if (hasSkinMatrices)
                        {
                            skinningDataArray = new NativeArray<SkinningData>(batch.InstanceCount, Allocator.Temp);
                            byteAddressForSkinDatas = byteAddressWorldToObject + (uint)(PackedMatrix.SizeInBytes * batch.InstanceCount);
                            startAddressForSkinMatrices = byteAddressForSkinDatas + (uint)(SkinningData.SizeInBytes * batch.InstanceCount);

                            int matrixCount = 0;
                            for(int i=0; i<batch.InstanceCount; i++)
                            {
                                var skinMatrices = skinMatrixBufferAccessor[i + readOffset];
                                matrixCount += skinMatrices.Length;
                            }

                            bufferCountToWrite += (matrixCount * skinMatrixSizeInBytes + sizeof(int) - 1) / sizeof(int);
                        }

                        var circularGraphicsBuffer = graphicBuffers[batch.BufferHandle];
                        var graphicsBuffer = circularGraphicsBuffer.SwitchToNextBuffer();

                        var bufferData = graphicsBuffer.LockBufferForWrite<int>(0, bufferCountToWrite);
                        {
                            // Place one zero matrix at the start of the instance data buffer, so loads from address 0 will return zero
                            var zero = float4x4.zero;
                            var bufferPtr = (byte*)bufferData.GetUnsafePtr();

                            UnsafeUtility.CopyStructureToPtr(ref zero, bufferPtr);
                            UnsafeUtility.MemCpy(bufferPtr + byteAddressObjectToWorld, l2wPackedAddr + readOffset, batch.InstanceCount * PackedMatrix.SizeInBytes);
                            UnsafeUtility.MemCpy(bufferPtr + byteAddressWorldToObject, l2wPackedInverseAddr + readOffset, batch.InstanceCount * PackedMatrix.SizeInBytes);

                            if (hasSkinMatrices)
                            {
                                uint currentSkinMatrixOffset = 0;

                                for (int i = 0; i < batch.InstanceCount; i++)
                                {
                                    var skinMatrices = skinMatrixBufferAccessor[i + readOffset];
                                    uint skinMatricesAddrOffset = startAddressForSkinMatrices + currentSkinMatrixOffset;

                                    skinningDataArray[i] = new SkinningData()
                                    {
                                        Value = new uint4(skinMatricesAddrOffset, 0, 0, 0)
                                    };
                                    var writeSize = (uint)(skinMatrices.Length * skinMatrixSizeInBytes);
                                    UnsafeUtility.MemCpy(bufferPtr + skinMatricesAddrOffset, skinMatrices.GetUnsafeReadOnlyPtr(), writeSize);
                                    currentSkinMatrixOffset += writeSize;
                                }

                                UnsafeUtility.MemCpy(bufferPtr + byteAddressForSkinDatas, skinningDataArray.GetUnsafePtr(), skinningDataArray.Length * SkinningData.SizeInBytes);
                            }
                        }
                        graphicsBuffer.UnlockBufferAfterWrite<int>(bufferCountToWrite);
                        readOffset += batch.InstanceCount;

                        // batch
                        if (batch.BatchID == BatchID.Null || batch.NeedRecreateBatch)
                        {
                            if (batch.BatchID != BatchID.Null)
                            {
                                brg.RemoveBatch(batch.BatchID);
                            }

                            int metaDataCount = 2; // o2w and w2o matrices
                            if (hasSkinMatrices)
                                metaDataCount++;
                            
                            var metadata = new NativeArray<MetadataValue>(metaDataCount, Allocator.Temp);
                            metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
                            metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
                            if (hasSkinMatrices)
                            {
                                metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_SkinningData"), Value = 0x80000000 | byteAddressForSkinDatas, };
                            }
                            
                            //#define unity_LightmapST            UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapST)
                            //#define unity_LightmapIndex         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4,   unity_LightmapIndex)

                            // Finally, create a batch for our instances, and make the batch use the GraphicsBuffer with our
                            // instance data, and the metadata values that specify where the properties are. Note that
                            // we do not need to pass any batch size here.
                            batch.BatchID = brg.AddBatch(metadata, graphicsBuffer.bufferHandle, (uint)BufferOffset, (uint)BufferWindowSize);

                            //Debug.Log($"Created batch for {batch.InstanceCount} instances, batchID: {batch.BatchID.value}, bufferID: {graphicsBuffer.bufferHandle.value}, mat ID: {chunkGraphicsData.MaterialID.value}, with skinning data: {hasSkinMatrices}");

                            batch.NeedRecreateBatch = false;
                            batchList[b] = batch;
                        }
                        else
                        {
                            brg.SetBatchBuffer(batch.BatchID, graphicsBuffer.bufferHandle);
                        }
                    }

                    l2wPackedArray.Dispose();
                    l2wPackedInverseArray.Dispose();

                    writeChangedDataMarker.End();

                    // обновляем данные батчей
                    batchIndex = 0;

                    if (chunkBatchDataMap.TryGetFirstValue(chunkID, out _, out it))
                    {
                        do
                        {
                            if (batchIndex >= batchList.Length)
                            {
                                break;
                            }
                            chunkBatchDataMap.SetValue(batchList[batchIndex], it);

                            batchIndex++;
                        }
                        while (chunkBatchDataMap.TryGetNextValue(out _, ref it));
                    }

                    // chunk 
                    chunk.SetChunkComponentData(ref chunkGraphicsDataHandle, chunkGraphicsData);
                }

                updatingChunksMarker.End();
            }
            
            updateBounds();

            cullingJobDependency = Dependency;
        }

        void updateReflectionProbes()
        {
            var ecb = commandBufferSystem.CreateCommandBuffer();
            
            Entities
                .WithoutBurst()
                .WithNone<ReflectionProbeData>()
                .WithChangeFilter<ReflectionProbe>()
                .ForEach((Entity entity, in ReflectionProbe reflectionProbe) =>
                {
                    if (freeReflectionProbes.Count == 0)
                    {
                        Debug.LogWarning("Failed to initialize reflection probe, not available slots");
                        return;
                    }

                    var rpData = new ReflectionProbeData();
                    rpData.Index = freeReflectionProbes[0]; 
                    freeReflectionProbes.RemoveAt(0);

                    Debug.Log($"Set reflection probe {reflectionProbe.name} to index {rpData.Index}");
                    Shader.SetGlobalTexture($"_ReflProbe{rpData.Index}", reflectionProbe.texture);
                    Shader.SetGlobalFloat("_ReflProbe0_Intensity", reflectionProbe.intensity);
                    
                    ecb.AddComponent(entity, rpData);
                    
                }).Run();
            
            Entities
                .WithoutBurst()
                .WithNone<ReflectionProbe>()
                .ForEach((Entity entity, in ReflectionProbeData probeData) =>
                {
                    if (freeReflectionProbes.Contains(probeData.Index) == false)
                    {
                        Debug.Log($"Free reflection probe index {probeData.Index}");
                        freeReflectionProbes.Add(probeData.Index);
                        freeReflectionProbes.Sort();
                        Shader.SetGlobalTexture($"_ReflProbe{probeData.Index}", null);
                    }

                    ecb.RemoveComponent<ReflectionProbeData>(entity);

                }).Run();
        }

        void updateParticles()
        {
            if(disabledParticlesQuery.IsEmpty == false
                || nonDisabledParticlesQuery.IsEmpty == false)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                Entities
                    .WithStoreEntityQueryInField(ref disabledParticlesQuery)
                    .WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities)
                    .WithoutBurst()
                    .WithAll<Disabled>()
                    .WithNone<DisabledHybridComponentTag>()
                    .ForEach((Entity entity, ParticleSystemRenderer particleSystemRenderer) =>
                    {
                        particleSystemRenderer.enabled = false;
                        ecb.AddComponent<DisabledHybridComponentTag>(entity);

                    }).Run();

                Entities
                    .WithStoreEntityQueryInField(ref nonDisabledParticlesQuery)
                    .WithoutBurst()
                    .WithAll<DisabledHybridComponentTag>()
                    .WithNone<Disabled>()
                    .ForEach((Entity entity, ParticleSystemRenderer particleSystemRenderer) =>
                    {
                        particleSystemRenderer.enabled = true;
                        ecb.RemoveComponent<DisabledHybridComponentTag>(entity);

                    }).Run();

                ecb.Playback(EntityManager);
            }
            
        }

        void updateBounds()
        {
            Entities
                .WithChangeFilter<LocalToWorld>()
                .ForEach((ref WorldRenderBounds worldBounds, in LocalRenderBounds localBounds, in LocalToWorld l2w) =>
                {
                    worldBounds.Value = AABB.Transform(l2w.Value, localBounds.Value);
                    
                }).ScheduleParallel();


            Entities.ForEach((ref WorldRenderBounds worldBounds, in DynamicBuffer<BoneArrayElement> bones) =>
            {
                var bounds = new Bounds();

                foreach(var bone in bones)
                {
                    var boneL2W = SystemAPI.GetComponent<LocalToWorld>(bone.Entity);
                    bounds.Encapsulate(boneL2W.Position);
                }

                worldBounds.Value = bounds.ToAABB();

            }).ScheduleParallel();
        }

        [BurstCompile]
        struct CullingJob : IJobChunk
        {
            public float3 CameraPosition;
            public float LodDistanceScale;

            [ReadOnly]
            public NativeArray<Plane> CullingPlanes;

            [ReadOnly]
            public ComponentTypeHandle<WorldRenderBounds> RenderBoundsType;

            [ReadOnly]
            public ComponentTypeHandle<LODRange> LodRangeType;

            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> L2WType;

            public NativeParallelHashMap<ulong, ChunkVisibility>.ParallelWriter ChunkVisibilitySet;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var lodVisibility = new ChunkVisibility();
                lodCulling(in chunk, ref lodVisibility);
                
                var finalVisibility = new ChunkVisibility();
                frustumCulling(in chunk, ref finalVisibility, in lodVisibility);

                ChunkVisibilitySet.TryAdd(chunk.SequenceNumber, finalVisibility);
            }

            void lodCulling(in ArchetypeChunk chunk, ref ChunkVisibility visibility)
            {
                if(chunk.Has(ref LodRangeType) == false || chunk.Has(ref L2WType) == false)
                {
                    visibility.Mask1 = ulong.MaxValue;
                    visibility.Mask2 = ulong.MaxValue;
                    return;
                }

                var lodRanges = chunk.GetNativeArray(ref LodRangeType);
                var l2wArray = chunk.GetNativeArray(ref L2WType);

                int firstMaskCount = math.min(chunk.Count, 64);

                for (int i = 0; i < firstMaskCount; i++)
                {
                    var minDist = lodRanges[i].SquareDistanceMin;
                    var maxDist = lodRanges[i].SquareDistanceMax;

                    var pos = l2wArray[i].Position;
                    var sqDist = math.distancesq(CameraPosition, pos);
                    sqDist *= LodDistanceScale;

                    if (sqDist >= minDist && sqDist < maxDist)
                    {
                        visibility.Mask1 |= (ulong)1 << i;
                    }
                }
                if (chunk.Count > 64)
                {
                    for (int i = 64; i < chunk.Count; i++)
                    {
                        var minDist = lodRanges[i].SquareDistanceMin;
                        var maxDist = lodRanges[i].SquareDistanceMax;

                        var pos = l2wArray[i].Position;
                        var sqDist = math.distancesq(CameraPosition, pos);
                        sqDist *= LodDistanceScale;

                        if (sqDist >= minDist && sqDist < maxDist)
                        {
                            visibility.Mask2 |= (ulong)1 << i;
                        }
                    }
                }
            }

            void frustumCulling(in ArchetypeChunk chunk, ref ChunkVisibility visibility, in ChunkVisibility lodVisibility)
            {
                var bounds = chunk.GetNativeArray(ref RenderBoundsType);

                int firstMaskCount = math.min(chunk.Count, 64);

                for (int i = 0; i < firstMaskCount; i++)
                {
                    var instanceMask = (ulong)1 << i;

                    if ((instanceMask & lodVisibility.Mask1) == 0)
                    {
                        continue;
                    }
                    
                    var aabb = bounds[i].Value;

                    if (frustumContainsAABB(in aabb))
                    {
                        visibility.Mask1 |= instanceMask;
                    }
                }
                if (chunk.Count > 64)
                {
                    for (int i = 64; i < chunk.Count; i++)
                    {
                        var instanceMask = (ulong)1 << (i - 64);

                        if ((instanceMask & lodVisibility.Mask2) == 0)
                        {
                            continue;
                        }
                        
                        var aabb = bounds[i].Value;

                        if (frustumContainsAABB(in aabb))
                        {
                            visibility.Mask2 |= instanceMask;
                        }
                    }
                }
            }

            bool frustumContainsAABB(in AABB aabb)
            {
                float3 pos;

                for (int i = 0; i < 6; i++)
                {
                    pos.x = CullingPlanes[i].normal.x > 0 ? aabb.Max.x : aabb.Min.x;
                    pos.y = CullingPlanes[i].normal.y > 0 ? aabb.Max.y : aabb.Min.y;
                    pos.z = CullingPlanes[i].normal.z > 0 ? aabb.Max.z : aabb.Min.z;

                    
                    if (CullingPlanes[i].GetDistanceToPoint(pos) < 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        struct ChunkVisibility
        {
            // 128 entities per chunk max
            public ulong Mask1;
            public ulong Mask2;

            public bool IsAnyVisible => (Mask1 | Mask2) != 0;
        }

        static float CalculateLodDistanceScale(float fieldOfView, float globalLodBias, bool isOrtho, float orthoSize)
        {
            float distanceScale;

            if (isOrtho)
            {
                distanceScale = 2.0f * orthoSize / globalLodBias;
            }
            else
            {
                var halfAngle = math.tan(math.radians(fieldOfView * 0.5F));
                // Half angle at 90 degrees is 1.0 (So we skip halfAngle / 1.0 calculation)
                distanceScale = (2.0f * halfAngle) / globalLodBias;
            }

            return distanceScale;
        }

        [BurstCompile]
        struct DrawCommandsJob : IJob
        {
            public float3 CameraWorldPosition;
            [ReadOnly] public ComponentTypeHandle<ChunkGraphicsData> BatchChunkDataTypeHandle;
            [ReadOnly] public ComponentTypeHandle<MeshInfo> MeshInfoType;
            [ReadOnly] public ComponentTypeHandle<WorldRenderBounds> WorldBoundsType;
            [ReadOnly] public NativeArray<ArchetypeChunk> BatchChunks;
            [ReadOnly] public NativeParallelHashMap<ulong, ChunkVisibility> VisiblitySet;
            [ReadOnly] public NativeParallelMultiHashMap<ulong, ChunkBatchData> ChunkBatchDataMap;
            public BatchCullingOutput CullingOutput;

            [BurstCompile]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool checkVisibility(in int instanceIndex, in ChunkVisibility visibility)
            {
                ulong mask = instanceIndex < 64 ? visibility.Mask1 : visibility.Mask2;
                int bitShift = instanceIndex < 64 ? instanceIndex : instanceIndex - 64;

                if ((mask & ((ulong)1 << bitShift)) != 0)
                {
                    return true;
                }

                return false;
            }

            struct InstanceDrawInfo
            {
                public BatchID BatchID;
                public BatchMaterialID MaterialID;
                public BatchMeshID MeshID;
                public byte SubMeshIndex;

                public float3 WorldPosition;
                public int InstanceIndex;

                public bool BelongsToSameDrawCall(in InstanceDrawInfo drawInfo)
                {
                    return BatchID == drawInfo.BatchID
                        && MaterialID == drawInfo.MaterialID
                        && MeshID == drawInfo.MeshID
                        && SubMeshIndex == drawInfo.SubMeshIndex;
                }
            }

            struct InstanceDrawInfoComparer : IComparer<InstanceDrawInfo>
            {
                public float3 CameraWorldPosition;

                public int Compare(InstanceDrawInfo x, InstanceDrawInfo y)
                {
                    var distX = math.distancesq(x.WorldPosition, CameraWorldPosition);
                    var distY = math.distancesq(y.WorldPosition, CameraWorldPosition);

                    if (distX < distY) return 1;
                    if (distX > distY) return -1;
                    return 0;
                }
            }

            public unsafe void Execute()
            {
                // count transparent instances
                int transparentInstanceCount = 0;

                foreach(var chunk in BatchChunks)
                {
                    if(chunk.Count == 0)
                    {
                        continue;
                    }

                    var graphicsData = chunk.GetChunkComponentData(ref BatchChunkDataTypeHandle);
                    var isTransparent = (graphicsData.Flags & ChunkGraphicsDataFlags.Transparent) == ChunkGraphicsDataFlags.Transparent;

                    if(isTransparent == false)
                    {
                        continue;
                    }

                    var visibility = VisiblitySet[chunk.SequenceNumber];

                    if(visibility.IsAnyVisible == false)
                    {
                        continue;
                    }

                    var meshInfos = chunk.GetNativeArray(ref MeshInfoType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var meshInfo = meshInfos[i];

                        if(meshInfo.HasValidMeshID == false)
                        {
                            continue;
                        }

                        if (checkVisibility(in i, in visibility))
                        {
                            transparentInstanceCount++;
                        }
                    }
                }

                NativeArray<InstanceDrawInfo> transparentInstanceList = default;

                if (transparentInstanceCount != 0)
                {
                    transparentInstanceList = new NativeArray<InstanceDrawInfo>(transparentInstanceCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                }

                // count draw calls
                var drawCommandCount = 0;
                int numOfVisibles = 0;
                var meshInfoList = new NativeList<MeshInfo>(16, Allocator.Temp);
                int transparentInstanceIndex = 0;

                foreach(var chunk in BatchChunks)
                {
                    if(chunk.Count == 0)
                    {
                        continue;
                    }

                    var graphicsData = chunk.GetChunkComponentData(ref BatchChunkDataTypeHandle);
                    
                    var visibility = VisiblitySet[chunk.SequenceNumber];

                    if(visibility.IsAnyVisible == false)
                    {
                        continue;
                    }

                    var batchEnumerator = ChunkBatchDataMap.GetValuesForKey(chunk.SequenceNumber);
                    int instanceIndex = 0;
                    var meshInfos = chunk.GetNativeArray(ref MeshInfoType);
                    var boundsArray = chunk.GetNativeArray(ref WorldBoundsType);
                    var isTransparent = (graphicsData.Flags & ChunkGraphicsDataFlags.Transparent) == ChunkGraphicsDataFlags.Transparent;

                    while(batchEnumerator.MoveNext())
                    {
                        var batch = batchEnumerator.Current;

                        if(batch.BatchID == BatchID.Null)
                        {
                            continue;
                        }

                        int instancesToDraw = 0;
                        meshInfoList.Clear();

                        for (int i = 0; i < batch.InstanceCount; i++)
                        {
                            if (checkVisibility(in instanceIndex, in visibility))
                            {
                                var meshInfo = meshInfos[instanceIndex];

                                if(meshInfo.HasValidMeshID)
                                {
                                    if (meshInfoList.Contains(meshInfo) == false)
                                    {
                                        meshInfoList.Add(meshInfo);
                                    }
                                    instancesToDraw++;

                                    if (isTransparent)
                                    {
                                        var bounds = boundsArray[instanceIndex];
                                        transparentInstanceList[transparentInstanceIndex] = new InstanceDrawInfo
                                        {
                                            BatchID = batch.BatchID,
                                            InstanceIndex = instanceIndex,
                                            WorldPosition = bounds.Value.Center,
                                            MaterialID = graphicsData.MaterialID,
                                            MeshID = meshInfo.MeshID,
                                            SubMeshIndex = meshInfo.SubMeshIndex,
                                        };
                                        transparentInstanceIndex++;
                                    }
                                }
                            }
                            instanceIndex++;
                        }

                        if (meshInfoList.Length > 0)
                        {
                            numOfVisibles += instancesToDraw;

                            if(isTransparent == false)
                            {
                                drawCommandCount += meshInfoList.Length;
                            }
                        }
                    }
                }

                if(transparentInstanceCount > 0)
                {
                    var drawInstanceInfoComparer = new InstanceDrawInfoComparer
                    {
                        CameraWorldPosition = CameraWorldPosition
                    };

                    transparentInstanceList.Sort(drawInstanceInfoComparer);

                    var currentDrawInfo = transparentInstanceList[0];
                    drawCommandCount++;

                    foreach(var drawInfo in transparentInstanceList)
                    {
                        if(drawInfo.BelongsToSameDrawCall(currentDrawInfo) == false)
                        {
                            currentDrawInfo = drawInfo;
                            drawCommandCount++;
                        }
                    }
                }

                if(drawCommandCount == 0)
                {
                    return;
                }
                
                // UnsafeUtility.Malloc() requires an alignment, so use the largest integer type's alignment
                // which is a reasonable default.
                int alignment = UnsafeUtility.AlignOf<long>();

                // Acquire a pointer to the BatchCullingOutputDrawCommands struct so we can easily
                // modify it directly.
                var drawCommands = (BatchCullingOutputDrawCommands*)CullingOutput.drawCommands.GetUnsafePtr();

                // Allocate memory for the output arrays. In a more complicated implementation the amount of memory
                // allocated could be dynamically calculated based on what we determined to be visible.
                // In this example, we will just assume that all of our instances are visible and allocate
                // memory for each of them. We need the following allocations:
                // - a single draw command (which draws kNumInstances instances)
                // - a single draw range (which covers our single draw command)
                // - kNumInstances visible instance indices.
                // The arrays must always be allocated using Allocator.TempJob.
                drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>() * drawCommandCount, alignment, Allocator.TempJob);
                drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
                drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(numOfVisibles * sizeof(int), alignment, Allocator.TempJob);
                drawCommands->drawCommandPickingInstanceIDs = null;

                drawCommands->drawCommandCount = drawCommandCount;
                drawCommands->drawRangeCount = 1;
                drawCommands->visibleInstanceCount = numOfVisibles;

                if(transparentInstanceCount != 0)
                {
                    drawCommands->instanceSortingPositions = (float*)UnsafeUtility.Malloc(transparentInstanceCount * sizeof(float) * 3, alignment, Allocator.TempJob);
                    drawCommands->instanceSortingPositionFloatCount = transparentInstanceCount * 3;
                }
                else
                {
                    drawCommands->instanceSortingPositions = null;
                    drawCommands->instanceSortingPositionFloatCount = 0;
                }

                // draw command index
                int dci = 0;
                uint visibleOffset = 0;

                // draw non-transparent instances
                for(int i=0; i<BatchChunks.Length; ++i)
                {
                    var chunk = BatchChunks[i];

                    if(chunk.Count == 0)
                    {
                        continue;
                    }

                    var graphicsData = chunk.GetChunkComponentData(ref BatchChunkDataTypeHandle);
                    var visibility = VisiblitySet[chunk.SequenceNumber];

                    if(visibility.IsAnyVisible == false)
                    {
                        continue;
                    }

                    var meshInfos = chunk.GetNativeArray(ref MeshInfoType);
                    var boundsArray = chunk.GetNativeArray(ref WorldBoundsType);
                    
                    var batchEnumerator = ChunkBatchDataMap.GetValuesForKey(chunk.SequenceNumber);
                    int instanceIndex = 0;

                    while(batchEnumerator.MoveNext())
                    {
                        var batch = batchEnumerator.Current;

                        if(batch.BatchID == BatchID.Null)
                        {
                            continue;
                        }

                        bool isTransparent = (graphicsData.Flags & ChunkGraphicsDataFlags.Transparent) == ChunkGraphicsDataFlags.Transparent;

                        if(isTransparent)
                        {
                            continue;
                        }

                        meshInfoList.Clear();

                        int instanceIndexForBatch = instanceIndex;
                        instanceIndex += batch.InstanceCount;

                        int tmpInstanceIndex = instanceIndexForBatch;
                        
                        for (int j = 0; j < batch.InstanceCount; j++)
                        {
                            if (checkVisibility(in tmpInstanceIndex, in visibility))
                            {
                                var meshInfo = meshInfos[tmpInstanceIndex];
                                if (meshInfo.HasValidMeshID && meshInfoList.Contains(meshInfo) == false)
                                {
                                    meshInfoList.Add(meshInfo);
                                }
                            }
                            tmpInstanceIndex++;
                        }

                        if (meshInfoList.Length == 0)
                        {
                            continue;
                        }

                        foreach (var currentMeshInfo in meshInfoList)
                        {
                            if(currentMeshInfo.HasValidMeshID == false)
                            {
                                continue;
                            }

                            int visibleCounter = 0;
                            tmpInstanceIndex = instanceIndexForBatch;

                            for (int j = 0; j < batch.InstanceCount; j++)
                            {
                                if (checkVisibility(in tmpInstanceIndex, in visibility))
                                {
                                    var instanceMeshInfo = meshInfos[tmpInstanceIndex];

                                    if (instanceMeshInfo.Equals(currentMeshInfo))
                                    {
                                        drawCommands->visibleInstances[visibleOffset + visibleCounter] = j;
                                        visibleCounter++;
                                    }
                                }
                                tmpInstanceIndex++;
                            }

                            // Configure our draw commands to draw instances
                            drawCommands->drawCommands[dci].visibleOffset = visibleOffset;
                            drawCommands->drawCommands[dci].visibleCount = (uint)visibleCounter;
                            drawCommands->drawCommands[dci].batchID = batch.BatchID;

                            drawCommands->drawCommands[dci].materialID = graphicsData.MaterialID;
                            drawCommands->drawCommands[dci].meshID = currentMeshInfo.MeshID;
                            drawCommands->drawCommands[dci].submeshIndex = currentMeshInfo.SubMeshIndex;
                            drawCommands->drawCommands[dci].splitVisibilityMask = 0xff;

                            drawCommands->drawCommands[dci].sortingPosition = 0;
                            drawCommands->drawCommands[dci].flags = 0;

                            dci++;
                            
                            visibleOffset += (uint)visibleCounter;
                        }
                    }
                }
                
                // draw transparent instances
                if(transparentInstanceCount > 0)
                {
                    int instanceCounter = 0;
                    int sortingPositionOffset = 0;
                    int tempSortingOffset = 0;

                    // trying to draw transparent objects in batches
                    for(int d=0; d < transparentInstanceCount; d++)
                    {
                        var drawInfo = transparentInstanceList[d];
                        drawCommands->visibleInstances[visibleOffset + instanceCounter] = drawInfo.InstanceIndex;
                        instanceCounter++;

                        var writeAddr = drawCommands->instanceSortingPositions + tempSortingOffset;
                        UnsafeUtility.CopyStructureToPtr(ref drawInfo.WorldPosition, writeAddr);
                        tempSortingOffset += 3;

                        if (d < transparentInstanceCount-1)
                        {
                            var nextInstance = transparentInstanceList[d + 1];

                            if(nextInstance.BelongsToSameDrawCall(drawInfo))
                            {
                                continue;
                            }
                        }

                        drawCommands->drawCommands[dci].visibleOffset = visibleOffset;
                        drawCommands->drawCommands[dci].visibleCount = (uint)instanceCounter;
                        drawCommands->drawCommands[dci].batchID = drawInfo.BatchID;

                        drawCommands->drawCommands[dci].materialID = drawInfo.MaterialID;
                        drawCommands->drawCommands[dci].meshID = drawInfo.MeshID;
                        drawCommands->drawCommands[dci].submeshIndex = drawInfo.SubMeshIndex;
                        drawCommands->drawCommands[dci].splitVisibilityMask = 0xff;

                        drawCommands->drawCommands[dci].sortingPosition = sortingPositionOffset;
                        drawCommands->drawCommands[dci].flags = BatchDrawCommandFlags.HasSortingPosition;

                        dci++;

                        visibleOffset += (uint)instanceCounter;
                        sortingPositionOffset += instanceCounter * 3;
                        instanceCounter = 0;
                    }
                }

                if (visibleOffset != numOfVisibles || drawCommandCount != dci)
                {
                    Debug.LogError($"visibleOffset: {visibleOffset}, numOfVisibles: {numOfVisibles}, drawCommandCount: {drawCommandCount}, dci: {dci}");
                }

                // Configure our single draw range to cover our draw commands which
                // is at offset 0
                drawCommands->drawRanges[0].drawCommandsBegin = 0;
                drawCommands->drawRanges[0].drawCommandsCount = (uint)drawCommandCount;

                // In this example we don't care about shadows or motion vectors, so we leave everything
                // to the default zero values, except the renderingLayerMask which we have to set to all ones
                // so the instances will be drawn regardless of mask settings when rendering.
                drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };
            }
        }

        public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            var batchChunkDataTypeHandle = GetComponentTypeHandle<ChunkGraphicsData>(true);
            var renderBoundsTypeHandle = GetComponentTypeHandle<WorldRenderBounds>(true);
            var lw2Type = GetComponentTypeHandle<LocalToWorld>(true);
            var lodRangeType = GetComponentTypeHandle<LODRange>(true);
            var meshInfoType = GetComponentTypeHandle<MeshInfo>(true);

            var chunkCount = batchedMeshesQuery.CalculateChunkCount();
            var visiblitySet = new NativeParallelHashMap<ulong, ChunkVisibility>(chunkCount, Allocator.TempJob);
            var lodParams = cullingContext.lodParameters;

            var lodDistanceScale = CalculateLodDistanceScale(lodParams.fieldOfView, QualitySettings.lodBias,
                lodParams.isOrthographic, lodParams.orthoSize);
            
            // to be more or less equal to the default URP renderer 
            lodDistanceScale *= 0.9653f;
            lodDistanceScale *= lodDistanceScale;

            var cullingJob = new CullingJob
            {
                CameraPosition = cullingContext.lodParameters.cameraPosition,
                LodDistanceScale = lodDistanceScale, 
                L2WType = lw2Type,
                LodRangeType = lodRangeType,
                CullingPlanes = cullingContext.cullingPlanes,
                RenderBoundsType = renderBoundsTypeHandle,
                ChunkVisibilitySet = visiblitySet.AsParallelWriter()
            };

            cullingJobDependency = cullingJob.ScheduleParallel(batchedMeshesQuery, cullingJobDependency);

            var batchChunks 
                = batchedMeshesQuery.ToArchetypeChunkListAsync(
                    Allocator.TempJob, 
                    cullingJobDependency, 
                    out cullingJobDependency);
                
            var drawCommandsJob = new DrawCommandsJob
            {
                CameraWorldPosition = cullingContext.lodParameters.cameraPosition,
                CullingOutput = cullingOutput,
                BatchChunks = batchChunks.AsDeferredJobArray(),
                ChunkBatchDataMap = chunkBatchDataMap,
                VisiblitySet = visiblitySet,
                BatchChunkDataTypeHandle = batchChunkDataTypeHandle,
                WorldBoundsType = renderBoundsTypeHandle,
                MeshInfoType = meshInfoType
            };
                
            cullingJobDependency = drawCommandsJob.Schedule(cullingJobDependency);
            
            cullingJobDependency = batchChunks.Dispose(cullingJobDependency);
            cullingJobDependency = visiblitySet.Dispose(cullingJobDependency);
            
            return cullingJobDependency;
        }

        public void LogInfo()
        {
            EntityManager.CompleteAllTrackedJobs();

            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Number of batches: {chunkBatchDataMap.Count()}");

            var batchChunks = batchedMeshesQuery.ToArchetypeChunkArray(Allocator.Temp);
            sb.AppendLine($"Number of graphics chunks: {batchChunks.Length}");

            int totalGraphicBufferSize = 0;
            int totalGraphicsBuffers = 0;

            foreach (var buffer in graphicBuffers)
            {
                totalGraphicsBuffers += buffer.Value.GetBufferCount();
                totalGraphicBufferSize += buffer.Value.GetBytesSizeOfAllBuffers();
            }

            int totalFreeGraphicBuffersSize = 0;
            int totalFreeGraphicBuffers = 0;

            foreach (var freeBufferIndex in freeGraphicBuffers)
            {
                var buffer = graphicBuffers[freeBufferIndex];

                totalFreeGraphicBuffersSize += buffer.GetBytesSizeOfAllBuffers();
                totalFreeGraphicBuffers += buffer.GetBufferCount();
            }

            sb.AppendLine($"Circular graphics buffer created: {graphicBuffers.Count}, total graphics buffers size: {totalGraphicBufferSize}, buffer count: {totalGraphicsBuffers}");
            sb.AppendLine($"Free circular graphics buffers: {totalFreeGraphicBuffers}, total free graphics buffers size: {totalFreeGraphicBuffersSize}, free buffer count: {totalFreeGraphicBuffers}");

            var graphicsDataType = GetComponentTypeHandle<ChunkGraphicsData>(true);
            var meshInfoType = GetComponentTypeHandle<MeshInfo>(true);

            sb.AppendLine("");
            sb.AppendLine("--- Registered materials: ---");
            foreach (var material in registeredMaterials)
            {
                int instanceCounter = 0;
                int chunkCounter = 0;

                foreach(var chunk in batchChunks)
                {
                    var graphicsData = chunk.GetChunkComponentData(ref graphicsDataType);

                    if(graphicsData.MaterialID == material.Value)
                    {
                        chunkCounter++;
                        instanceCounter += chunk.Count;
                    }
                }

                sb.AppendLine($"{material.Value.value} -> {material.Key.Result.name}, chunks: {chunkCounter}, instances: {instanceCounter}");
            }

            sb.AppendLine("");
            sb.AppendLine("--- Registered meshes ---");

            foreach(var mesh in registeredMeshes)
            {
                int instanceCounter = 0;

                foreach(var chunk in batchChunks)
                {
                    var meshInfos = chunk.GetNativeArray(ref meshInfoType);

                    foreach(var meshInfo in meshInfos)
                    {
                        if(meshInfo.MeshID == mesh.Value)
                        {
                            instanceCounter++;
                        }
                    }
                }

                sb.AppendLine($"{mesh.Value.value} -> {mesh.Key.Result.name}, instances: {instanceCounter}");
            }

            Debug.Log(sb.ToString());
        }
    }
}