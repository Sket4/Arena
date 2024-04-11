using System.Collections;
using UnityEngine;

namespace Arena
{
    public class MapCameraRender : MonoBehaviour
    {
        [SerializeField]
        Camera mapCamera = default;

        [SerializeField]
        int fps = 10;

        private void Start()
        {
            StartCoroutine(renderLoop());
        }

        IEnumerator renderLoop()
        {
            float interval = 1.0f / fps;

#if UNITY_EDITOR
            int lastFps = fps;
#endif

            while(true)
            {
                mapCamera.Render();
                yield return new WaitForSeconds(interval);

#if UNITY_EDITOR
                if(fps != lastFps)
                {
                    lastFps = fps;
                    interval = 1.0f / fps;
                }
#endif
            }
        }
    }
}
