using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Object = System.Object;

namespace DGX.SRP
{
    static class SpaceFillingCurves
    {
        // "Insert" a 0 bit after each of the 16 low bits of x.
        // Ref: https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/
        static uint Part1By1(uint x)
        {
            x &= 0x0000ffff;                  // x = ---- ---- ---- ---- fedc ba98 7654 3210
            x = (x ^ (x <<  8)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
            x = (x ^ (x <<  4)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
            x = (x ^ (x <<  2)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
            x = (x ^ (x <<  1)) & 0x55555555; // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
            return x;
        }

        // Inverse of Part1By1 - "delete" all odd-indexed bits.
        // Ref: https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/
        static uint Compact1By1(uint x)
        {
            x &= 0x55555555;                  // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
            x = (x ^ (x >>  1)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
            x = (x ^ (x >>  2)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
            x = (x ^ (x >>  4)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
            x = (x ^ (x >>  8)) & 0x0000ffff; // x = ---- ---- ---- ---- fedc ba98 7654 3210
            return x;
        }

        public static uint EncodeMorton2D(uint2 coord)
        {
            return (Part1By1(coord.y) << 1) + Part1By1(coord.x);
        }

        public static uint2 DecodeMorton2D(uint code)
        {
            return math.uint2(Compact1By1(code >> 0), Compact1By1(code >> 1));
        }
    }
    
    public struct ReflectionProbeManager : IDisposable
    {
        private ulong version;
        public ulong Version => version;
        int2 m_Resolution;
        RenderTexture m_AtlasTexture0;
        RenderTexture m_AtlasTexture1;
        BuddyAllocator m_AtlasAllocator;
        List<CachedProbe> m_Cache;
        List<int> m_NeedsUpdate;

        // Pre-allocated arrays for filling constant buffers
        //Vector4[] m_BoxMax;
        //Vector4[] m_BoxMin;
        //Vector4[] m_ProbePosition;
        Vector4[] m_MipScaleOffset;

        // There is a global max of 7 mips in Unity.
        const int k_MaxMipCount = 7;
        const string k_ReflectionProbeAtlasName = "URP Reflection Probe Atlas";

        private const int MAX_COUNT = 32;

        unsafe struct CachedProbe
        {
            public int id;
            public uint updateCount;
            public Hash128 imageContentsHash;
            public int size;
            public int mipCount;
            // One for each mip.
            public fixed int dataIndices[k_MaxMipCount];
            public fixed int levels[k_MaxMipCount];
            public Texture texture;
            public ReflectionProbe probe;
            public int lastUsed;
            public Vector4 hdrData;
        }

        static class ShaderProperties
        {
            public static readonly int BoxMin = Shader.PropertyToID("tg_ReflProbes_BoxMin");
            public static readonly int BoxMax = Shader.PropertyToID("tg_ReflProbes_BoxMax");
            public static readonly int ProbePosition = Shader.PropertyToID("tg_ReflProbes_ProbePosition");
            public static readonly int MipScaleOffset = Shader.PropertyToID("tg_ReflProbes_MipScaleOffset");
            public static readonly int Count = Shader.PropertyToID("tg_ReflProbes_Count");
            public static readonly int Atlas = Shader.PropertyToID("tg_ReflProbes_Atlas");
        }

        public RenderTexture atlasRT => m_AtlasTexture0;

        public static ReflectionProbeManager Create()
        {
            var instance = new ReflectionProbeManager();
            instance.Init();
            return instance;
        }

        public int GetReflectionProbeIndex(ReflectionProbe probe)
        {
            for (var index = 0; index < m_Cache.Count; index++)
            {
                var cachedProbe = m_Cache[index];
                if (cachedProbe.probe == probe)
                {
                    return index;
                }
            }
            return -1;
        }

        void Init()
        {
            var maxProbes = MAX_COUNT;

            // m_Resolution = math.min((int)reflectionProbeResolution, SystemInfo.maxTextureSize);
            m_Resolution = 1;
            var format = GraphicsFormat.B10G11R11_UFloatPack32;
            if (!SystemInfo.IsFormatSupported(format, FormatUsage.Render)) { format = GraphicsFormat.R16G16B16A16_SFloat; }
            m_AtlasTexture0 = new RenderTexture(new RenderTextureDescriptor
            {
                width = m_Resolution.x,
                height = m_Resolution.y,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                graphicsFormat = format,
                useMipMap = false,
                msaaSamples = 1
            });
            m_AtlasTexture0.name = k_ReflectionProbeAtlasName;
            m_AtlasTexture0.filterMode = FilterMode.Bilinear;
            m_AtlasTexture0.hideFlags = HideFlags.HideAndDontSave;
            m_AtlasTexture0.Create();

            m_AtlasTexture1 = new RenderTexture(m_AtlasTexture0.descriptor);
            m_AtlasTexture1.name = k_ReflectionProbeAtlasName;
            m_AtlasTexture1.filterMode = FilterMode.Bilinear;
            m_AtlasTexture1.hideFlags = HideFlags.HideAndDontSave;

            // The smallest allocatable resolution we want is 4x4. We calculate the number of levels as:
            // log2(max) - log2(4) = log2(max) - 2
            m_AtlasAllocator = new BuddyAllocator(math.floorlog2(SystemInfo.maxTextureSize) - 2, 2);
            m_Cache = new List<CachedProbe>(maxProbes);
            m_NeedsUpdate = new List<int>(maxProbes);

            //m_BoxMax = new Vector4[maxProbes];
            //m_BoxMin = new Vector4[maxProbes];
            //m_ProbePosition = new Vector4[maxProbes];
            m_MipScaleOffset = new Vector4[maxProbes * 7];
        }

        public unsafe void UpdateGpuData(CommandBuffer cmd, ref CullingResults cullResults)
        {
            var probes = cullResults.visibleReflectionProbes;
            var frameIndex = Time.renderedFrameCount;

            // Populate list of probes we need to remove to avoid modifying dictionary while iterating.
            for (var index = m_Cache.Count - 1; index >= 0; index--)
            {
                var cachedProbe = m_Cache[index];
                // Evict probe if not used for more than 1 frame, if the texture no longer exists, or if the size changed.
                if (
                    //Math.Abs(cachedProbe.lastUsed - frameIndex) > 1
                    !cachedProbe.probe || // new
                    !cachedProbe.texture ||
                    cachedProbe.size != cachedProbe.texture.width)
                {
                    m_Cache.RemoveAt(index);
                    
                    for (var i = 0; i < k_MaxMipCount; i++)
                    {
                        if (cachedProbe.dataIndices[i] != -1)
                            m_AtlasAllocator.Free(
                                new BuddyAllocation(cachedProbe.levels[i], cachedProbe.dataIndices[i]));
                    }
                }
            }

            var requiredAtlasSize = math.int2(0, 0);

            var visibleProbeCount = probes.Length;
            for (var probeIndex = 0; probeIndex < visibleProbeCount; probeIndex++)
            {
                var probe = probes[probeIndex];

                if (probe.reflectionProbe == false)
                {
                    continue;
                }

                var texture = probe.texture;
                var id = probe.reflectionProbe.GetInstanceID();
                
                CachedProbe cachedProbe = default;
                int cachedIndex = -1;

                for (var index = 0; index < m_Cache.Count; index++)
                {
                    var cached = m_Cache[index];
                    if (cached.id == id)
                    {
                        cachedIndex = index;
                        cachedProbe = cached;
                        break;
                    }
                }

                var wasCached = cachedIndex != -1;

                if (!texture)
                {
                    continue;
                }

                if (!wasCached)
                {
                    cachedProbe.size = texture.width;
                    var mipCount = math.ceillog2(cachedProbe.size * 4) + 1;
                    var level = m_AtlasAllocator.levelCount + 2 - mipCount;
                    cachedProbe.mipCount = math.min(mipCount, k_MaxMipCount);
                    cachedProbe.texture = texture;
                    cachedProbe.probe = probe.reflectionProbe;
                    cachedProbe.id = probe.reflectionProbe.GetInstanceID();

                    var mip = 0;
                    for (; mip < cachedProbe.mipCount; mip++)
                    {
                        // Clamp to maximum level. This is relevant for 64x64 and lower, which will have valid content
                        // in 1x1 mip. The octahedron size is double the face size, so that ends up at 2x2. Due to
                        // borders the final mip must be 4x4 as that leaves 2x2 texels for the octahedron.
                        var mipLevel = math.min(level + mip, m_AtlasAllocator.levelCount - 1);
                        if (!m_AtlasAllocator.TryAllocate(mipLevel, out var allocation)) break;
                        // We split up the allocation struct because C# cannot do struct fixed arrays :(
                        cachedProbe.levels[mip] = allocation.level;
                        cachedProbe.dataIndices[mip] = allocation.index;
                        var scaleOffset = (int4)(GetScaleOffset(mipLevel, allocation.index, true, false) * m_Resolution.xyxy);
                        requiredAtlasSize = math.max(requiredAtlasSize, scaleOffset.zw + scaleOffset.xy);
                    }

                    // Check if we ran out of space in the atlas.
                    if (mip < cachedProbe.mipCount)
                    {
                        for (var i = 0; i < mip; i++) m_AtlasAllocator.Free(new BuddyAllocation(cachedProbe.levels[i], cachedProbe.dataIndices[i]));
                        for (var i = 0; i < k_MaxMipCount; i++) cachedProbe.dataIndices[i] = -1;
                        continue;
                    }

                    for (; mip < k_MaxMipCount; mip++)
                    {
                        cachedProbe.dataIndices[mip] = -1;
                    }
                }

                var needsUpdate = !wasCached || cachedProbe.updateCount != texture.updateCount;
#if UNITY_EDITOR
                needsUpdate |= cachedProbe.imageContentsHash != texture.imageContentsHash;
#endif
                needsUpdate |= cachedProbe.hdrData != probe.hdrData;    // The probe needs update if the runtime intensity multiplier changes

                if (needsUpdate)
                {
                    cachedProbe.updateCount = texture.updateCount;
#if UNITY_EDITOR
                    cachedProbe.imageContentsHash = texture.imageContentsHash;
#endif
                    if (wasCached)
                    {
                        m_NeedsUpdate.Add(cachedIndex);
                    }
                    else
                    {
                        m_NeedsUpdate.Add(m_Cache.Count);
                    }
                }

                // If the probe is set to be updated every frame, we assign the last used frame to -1 so it's evicted in next frame.
                if (probe.reflectionProbe.refreshMode == ReflectionProbeRefreshMode.EveryFrame)
                    cachedProbe.lastUsed = -1;
                else
                    cachedProbe.lastUsed = frameIndex;

                cachedProbe.hdrData = probe.hdrData;

                if (wasCached)
                {
                    m_Cache[cachedIndex] = cachedProbe;
                }
                else
                {
                    m_Cache.Add(cachedProbe);
                }
            }

            // Grow the atlas if it's not big enough to contain the current allocations.
            if (math.any(m_Resolution < requiredAtlasSize))
            {
                requiredAtlasSize = math.max(m_Resolution, math.ceilpow2(requiredAtlasSize));
                var desc = m_AtlasTexture0.descriptor;
                desc.width = requiredAtlasSize.x;
                desc.height = requiredAtlasSize.y;
                if (m_AtlasTexture1.IsCreated())
                {
                    m_AtlasTexture1.Release();    
                }
                m_AtlasTexture1.width = requiredAtlasSize.x;
                m_AtlasTexture1.height = requiredAtlasSize.y;
                m_AtlasTexture1.Create();

                if (m_AtlasTexture0.width != 1)
                {
                    if (SystemInfo.copyTextureSupport != CopyTextureSupport.None)
                    {
                        Graphics.CopyTexture(m_AtlasTexture0, 0, 0, 0, 0, m_Resolution.x, m_Resolution.y, m_AtlasTexture1, 0, 0, 0, 0);
                    }
                    else
                    {
                        Graphics.Blit(m_AtlasTexture0, m_AtlasTexture1, (float2)m_Resolution / requiredAtlasSize, Vector2.zero);
                    }
                }

                m_AtlasTexture0.Release();
                (m_AtlasTexture0, m_AtlasTexture1) = (m_AtlasTexture1, m_AtlasTexture0);
                m_Resolution = requiredAtlasSize;
            }

            for (var index = 0; index < m_Cache.Count; index++)
            {
                var probe = m_Cache[index];
                //m_BoxMax[dataIndex] = new Vector4(probe.bounds.max.x, probe.bounds.max.y, probe.bounds.max.z, probe.blendDistance);
                //m_BoxMin[dataIndex] = new Vector4(probe.bounds.min.x, probe.bounds.min.y, probe.bounds.min.z, probe.importance);
                //m_ProbePosition[dataIndex] = new Vector4(probe.localToWorldMatrix.m03, probe.localToWorldMatrix.m13, probe.localToWorldMatrix.m23, (probe.isBoxProjection ? 1 : -1) * (cachedProbe.mipCount));
                for (var i = 0; i < probe.mipCount; i++)
                    m_MipScaleOffset[index * k_MaxMipCount + i] = GetScaleOffset(probe.levels[i],
                        probe.dataIndices[i], false, false);
            }

            //using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.UpdateReflectionProbeAtlas)))
            {
                cmd.SetRenderTarget(m_AtlasTexture0);

                foreach (var probeId in m_NeedsUpdate)
                {
                    var cachedProbe = m_Cache[probeId];
                    for (var mip = 0; mip < cachedProbe.mipCount; mip++)
                    {
                        var level = cachedProbe.levels[mip];
                        var dataIndex = cachedProbe.dataIndices[mip];
                        // If we need to y-flip we will instead flip the atlas since that is updated less frequent and then the lookup should be correct.
                        // By doing this we won't have to y-flip the lookup in the shader code. 
                        var scaleBias = GetScaleOffset(level, dataIndex, true, !SystemInfo.graphicsUVStartsAtTop);
                        var sizeWithoutPadding = (1 << (m_AtlasAllocator.levelCount + 1 - level)) - 2;
                        Blitter.BlitCubeToOctahedral2DQuadWithPadding(cmd, cachedProbe.texture, new Vector2(sizeWithoutPadding, sizeWithoutPadding), scaleBias, mip, true, 2, cachedProbe.hdrData);
                    }
                }

                //cmd.SetGlobalVectorArray(ShaderProperties.BoxMin, m_BoxMin);
                //cmd.SetGlobalVectorArray(ShaderProperties.BoxMax, m_BoxMax);
                //cmd.SetGlobalVectorArray(ShaderProperties.ProbePosition, m_ProbePosition);
                cmd.SetGlobalVectorArray(ShaderProperties.MipScaleOffset, m_MipScaleOffset);
                cmd.SetGlobalFloat(ShaderProperties.Count, m_Cache.Count);
                cmd.SetGlobalTexture(ShaderProperties.Atlas, m_AtlasTexture0);
            }

            if (m_NeedsUpdate.Count > 0)
            {
                version++;
                //Debug.Log($"version {version}");
            }
            m_NeedsUpdate.Clear();
        }

        float4 GetScaleOffset(int level, int dataIndex, bool includePadding, bool yflip)
        {
            // level = m_AtlasAllocator.levelCount + 2 - (log2(size) + 1) <=>
            // log2(size) + 1 = m_AtlasAllocator.levelCount + 2 - level <=>
            // log2(size) = m_AtlasAllocator.levelCount + 1 - level <=>
            // size = 2^(m_AtlasAllocator.levelCount + 1 - level)
            var size = (1 << (m_AtlasAllocator.levelCount + 1 - level));
            var coordinate = SpaceFillingCurves.DecodeMorton2D((uint)dataIndex);
            var scale = (size - (includePadding ? 0 : 2)) / ((float2)m_Resolution);
            var bias = ((float2) coordinate * size + (includePadding ? 0 : 1)) / (m_Resolution);
            if (yflip) bias.y = 1.0f - bias.y - scale.y;
            return math.float4(scale, bias);
        }

        public void Dispose()
        {
            if (m_AtlasTexture0)
            {
                m_AtlasTexture0.Release();
            }
            UnityEngine.Object.DestroyImmediate(m_AtlasTexture0);
            UnityEngine.Object.DestroyImmediate(m_AtlasTexture1);

            this = default;
        }
    }
}
