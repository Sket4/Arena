using UnityEngine;

namespace Arena.WorldObserver
{
    public class WorldObserver : MonoBehaviour
    {
        [SerializeField]
        Camera observerCamera = default;

        [SerializeField]
        Renderer boundsMesh = default;

        public Camera Camera { get { return observerCamera; } }
        public Renderer BoundsMesh { get { return boundsMesh; } }
    }
}
