using System.Collections;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Debugging
{
    public class DebugSwissKnife : MonoBehaviour
    {
        public bool EnableRenderingAssetLoadLogging;
        public bool LogRenderInfoRepeating;
        
        private void Start()
        {
            if (LogRenderInfoRepeating)
            {
                StartCoroutine(logRenderInfo());
            }

            if (EnableRenderingAssetLoadLogging)
            {
                StartCoroutine(waitForRenderingSystem((rs) =>
                {
                    rs.LogAssetLoadingData = true;
                }));
            }
        }

        IEnumerator waitForRenderingSystem(System.Action<RenderingSystem> callback)
        {
            RenderingSystem renderingSystem = null;

            while (renderingSystem == null)
            {
                foreach (var world in World.All)
                {
                    renderingSystem = world.GetExistingSystemManaged<RenderingSystem>();

                    if (renderingSystem != null)
                    {
                        callback(renderingSystem);
                        break;
                    }
                }
                yield return null;
            }
        }

        IEnumerator logRenderInfo()
        {
            RenderingSystem system = null;
            
            yield return waitForRenderingSystem((renderingSystem) =>
            {
                system = renderingSystem;
            });
            
            while (true)
            {
                yield return new WaitForSeconds(5);
                system.LogInfo();
            }
        }
    }
}
