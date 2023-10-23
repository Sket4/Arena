using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace TzarGames.Renderer
{
    public class CircularGraphicsBuffer : IDisposable
    {
        private GraphicsBuffer[] buffers;
        private int maxBufferCount;
        private int bufferIndex;

        public CircularGraphicsBuffer(GraphicsBuffer.Target target, GraphicsBuffer.UsageFlags usafeFlags, int count, int stride)
        {
            maxBufferCount = NumFramesInFlight;
            buffers = new GraphicsBuffer[maxBufferCount];

            for (int i = 0; i < maxBufferCount; i++)
            {
                var buffer = new GraphicsBuffer(target, usafeFlags, count, stride);
                buffers[i] = buffer;
            }
        }

        public int GetBytesSizePerBuffer() => buffers[0].count * buffers[0].stride;
        public int GetBytesSizeOfAllBuffers() => GetBytesSizePerBuffer() * buffers.Length;

        public GraphicsBuffer SwitchToNextBuffer()
        {
            bufferIndex++;
            if (bufferIndex == maxBufferCount)
            {
                bufferIndex = 0;
            }
            return buffers[bufferIndex];
        }

        public GraphicsBuffer GetCurrent()
        {
            return buffers[bufferIndex];
        }
        
        internal static int NumFramesInFlight
        {
            get
            {
                // The number of frames in flight at the same time
                // depends on the Graphics device that we are using.
                // This number tells how long we need to keep the buffers
                // for a given frame alive. For example, if this is 4,
                // we can reclaim the buffers for a frame after 4 frames have passed.
                int numFrames = 0;

                switch (SystemInfo.graphicsDeviceType)
                {
                    case GraphicsDeviceType.Vulkan:
                    case GraphicsDeviceType.Direct3D11:
                    case GraphicsDeviceType.Direct3D12:
                    case GraphicsDeviceType.PlayStation4:
                    case GraphicsDeviceType.PlayStation5:
                    case GraphicsDeviceType.XboxOne:
                    case GraphicsDeviceType.GameCoreXboxOne:
                    case GraphicsDeviceType.GameCoreXboxSeries:
                    case GraphicsDeviceType.OpenGLCore:

                    // OpenGL ES 2.0 is no longer supported in Unity 2023.1 and later
#if !UNITY_2023_1_OR_NEWER
                    case GraphicsDeviceType.OpenGLES2:
#endif

                    case GraphicsDeviceType.OpenGLES3:
                    case GraphicsDeviceType.PlayStation5NGGC:
                        numFrames = 3;
                        break;
                    case GraphicsDeviceType.Switch:
                    case GraphicsDeviceType.Metal:
                    default:
                        numFrames = 4;
                        break;
                }

                // Use at least as many frames as the quality settings have, but use a platform
                // specific lower limit in any case.
                numFrames = math.max(numFrames, QualitySettings.maxQueuedFrames);

                return numFrames;
            }
        }

        public void Dispose()
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }
        }

        internal int GetBufferCount()
        {
            return buffers.Length;
        }

        internal IEnumerable<GraphicsBuffer> GetBuffers()
        {
            return buffers;
        }
    }
}
