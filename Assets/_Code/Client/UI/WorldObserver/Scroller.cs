using UnityEngine;
using UnityEngine.EventSystems;

namespace Arena.WorldObserver
{
    public class Scroller : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        float groundHeight = 0;

        public Camera Camera;
        public Renderer BoundsMesh;

        Transform cameraTransform;
        Vector3 maxCorner;
        Vector3 minCorner;

        private void Start()
        {
            cameraTransform = Camera.transform;
            var bounds = BoundsMesh.bounds;
            maxCorner = bounds.max;
            minCorner = bounds.min;

            var eventSystem = FindObjectOfType<EventSystem>();
            if(eventSystem == null)
            {
                var es = new GameObject("Event system");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                es.AddComponent<BaseInput>();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            var prevPos = eventData.position - eventData.delta;
            var currentRay = Camera.ScreenPointToRay(prevPos);
            var currentPoint = traceGround(currentRay.origin, currentRay.direction, groundHeight);

            var ray = Camera.ScreenPointToRay(eventData.position);
            var targetPoint = traceGround(ray.origin, ray.direction, groundHeight);

            cameraTransform.position += currentPoint - targetPoint;

            currentRay = Camera.ScreenPointToRay(new Vector3(Camera.pixelWidth * 0.5f, Camera.pixelHeight * 0.5f, 0));
            currentPoint = traceGround(currentRay.origin, currentRay.direction, groundHeight);

            var clampedPoint = currentPoint;

            clampedPoint.x = Mathf.Clamp(clampedPoint.x, minCorner.x, maxCorner.x);
            clampedPoint.z = Mathf.Clamp(clampedPoint.z, minCorner.z, maxCorner.z);

            cameraTransform.position += clampedPoint - currentPoint;
        }

        static Vector3 traceGround(Vector3 origin, Vector3 direction, float groundHeight)
        {
            var groundPlaneNormal = Vector3.up;
            var dot_rn = Vector3.Dot(origin, groundPlaneNormal);
            var dot_ln = Vector3.Dot(direction, groundPlaneNormal);
            var d = -groundPlaneNormal.y * groundHeight; //-n1x0 - n2y0 - n3z0
            return origin + direction * -((dot_rn + d) / dot_ln);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //
        }
    }
}

