using UnityEngine;

namespace Arena
{
    public class MapBounds : MonoBehaviour
    {
        [SerializeField]
        Renderer boundsRenderer = default;
        Bounds bounds;

        private void Reset()
        {
            boundsRenderer = GetComponent<Renderer>();
        }

        private void Awake()
        {
            if(boundsRenderer != null)
            {
                bounds = boundsRenderer.bounds;
            }
        }

        public void CreateBoundsFromMinMax(Vector3 min, Vector3 max)
        {
            bounds = new Bounds();
            bounds.SetMinMax(min, max);
        }

        public Bounds GetBounds()
        {
            return bounds;
        }
    }
}
