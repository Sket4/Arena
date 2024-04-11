using UnityEngine;

namespace Arena
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private Transform followTarget = default;
        [SerializeField] private float minimapSize = 18;
        [SerializeField] private float extendedSize = 36;
        [SerializeField] private float height = 50;
        
        [SerializeField] private Camera mapCamera = default;

        MapBounds mapBounds;

        Vector3 displacement;
        Transform cachedTransform;

        public float Height
        {
            get
            {
                return height;
            }
        }

        public Camera Camera
        {
            get
            {
                return mapCamera;
            }
        }

        void Start()
        {
            cachedTransform = transform;
            if(cachedTransform.parent != null)
            {
                cachedTransform.SetParent(null);
            }
            mapBounds = FindObjectOfType<MapBounds>();

            displacement = new Vector3(0, height, 0);
            SetMinimapMode();

            Debug.LogError("Not implemented");
            //if (EndlessGameManager.LocalPlayerCharacter != null)
            //{
            //    var e = mapCamera.transform.eulerAngles;
            //    e.y = EndlessGameManager.LocalPlayerCharacter.PlayerCamera.CameraYaw;
            //    mapCamera.transform.eulerAngles = e;
            //}
        }

        private void Update()
        {
            var yaw = cachedTransform.eulerAngles.y;
            var yawRot = Quaternion.Euler(0, yaw, 0);

            var rotDisp = yawRot * displacement;
            var newPos = followTarget.position + rotDisp;

            if(mapBounds != null)
            {
                var tmp = newPos;
                if(clampBounds(ref newPos))
                {
                    rotDisp = rotDisp + (newPos - tmp);
                    rotDisp = Quaternion.Inverse(yawRot) * rotDisp;
                    displacement = rotDisp;
                }
            }

            cachedTransform.position = newPos;
        }

        [ContextMenu("Minimap mode")]
        public void SetMinimapMode()
        {
            mapCamera.orthographicSize = minimapSize;
            ResetHorizontalDisplacement();
        }

        [ContextMenu("Extended mode")]
        public void SetExtendedMode()
        {
            mapCamera.orthographicSize = extendedSize;
        }        

        public void MoveHorizontally(float deltaX, float deltaZ)
        {
            if(mapBounds == null)
            {
                return;
            }
            displacement.x += deltaX;
            displacement.z += deltaZ;
        }

        public void ResetHorizontalDisplacement()
        {
            displacement.x = 0;
            displacement.z = 0;
        }

        bool clampBounds(ref Vector3 pos)
        {
            bool changed = false;
            var bounds = mapBounds.GetBounds();
            var min = bounds.min;
            var max = bounds.max;

            if(pos.x < min.x)
            {
                pos.x = min.x;
                changed = true;
            }
            else if(pos.x > max.x)
            {
                pos.x = max.x;
                changed = true;
            }

            if (pos.z < min.z)
            {
                pos.z = min.z;
                changed = true;
            }
            else if (pos.z > max.z)
            {
                pos.z = max.z;
                changed = true;
            }
            return changed;
        }
    }
}
