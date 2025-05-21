using System.Collections;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private float minimapSize = 18;
        [SerializeField] private float extendedSize = 36;
        [SerializeField] private float height = 50;
        [SerializeField] private int hdMapTextureSize = 1024;
        
        [SerializeField] private Camera mapCamera;

        [SerializeField] private float hardwareInputMoveSpeed = 1;
        //[SerializeField] private Axis2DInput moveInputAction;
        
        [SerializeField] private int mapTextureSize = 512;

        private EntityManager entityManager;
        private Entity targetEntity;
        private Entity mapEntity;
        public RenderTexture CameraTexture { get; private set; }

        Vector3 displacement;
        Transform cachedTransform;
        private bool isInExtendedMode = false;
        private Bounds mapBounds;
        private bool hasMapBounds = false;

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

        public void SetBounds(Bounds bounds)
        {
            mapBounds = bounds;
            hasMapBounds = true;
        }

        private void Update()
        {
            if(entityManager.Exists(targetEntity) == false 
               || entityManager.HasComponent<ViewDirection>(targetEntity) == false)
            {
                return;
            }
            // if (isInExtendedMode && moveInputAction != null)
            // {
            //     var move = moveInputAction.Get;
            //     displacement.x += move.x * hardwareInputMoveSpeed;
            //     displacement.z += move.y * hardwareInputMoveSpeed;    
            // }

            float3 cameraEulers;
            
            if (isInExtendedMode)
            {
                cameraEulers = cachedTransform.eulerAngles;
                cameraEulers.y = 0;
                cachedTransform.eulerAngles = cameraEulers;
            }
            else
            {
                var targetViewDir = entityManager.GetComponentData<ViewDirection>(targetEntity).Value;
                var viewDirRot = quaternion.LookRotation(targetViewDir, math.up());
                var viewDirEulers = math.Euler(viewDirRot);
                cameraEulers = cachedTransform.eulerAngles;
                cameraEulers.y = math.TODEGREES * viewDirEulers.y;
                cachedTransform.eulerAngles = cameraEulers;
                cameraEulers.y = viewDirEulers.y;    
            }
            
            var yawRot = quaternion.Euler(0, cameraEulers.y, 0);

            var rotDisp = math.mul(yawRot, displacement);
            var targetPosition = entityManager.GetComponentData<LocalTransform>(targetEntity).Position;
            
            var newPos = targetPosition + rotDisp;

            if(isInExtendedMode && hasMapBounds)
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
            isInExtendedMode = false;
            mapCamera.orthographicSize = minimapSize;
            ResetHorizontalDisplacement();
        }

        [ContextMenu("Extended mode")]
        public void SetExtendedMode()
        {
            mapCamera.orthographicSize = extendedSize;
            isInExtendedMode = true;
        }        

        public void MoveHorizontally(float deltaX, float deltaZ)
        {
            if(hasMapBounds == false)
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

        bool clampBounds(ref float3 pos)
        {
            bool changed = false;
            var min = mapBounds.min;
            var max = mapBounds.max;

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
        
        public void Setup(Entity targetEntity, EntityManager em)
        {
            entityManager = em;
            this.targetEntity = targetEntity;
            
            cachedTransform = transform;
            if(cachedTransform.parent != null)
            {
                cachedTransform.SetParent(null);
            }

            displacement = new Vector3(0, height, 0);
            SetMinimapMode();
            
            Camera.transform.rotation = Quaternion.LookRotation(Vector3.down);
            
            // var targetTransform = em.GetComponentData<LocalTransform>(this.targetEntity);
            // var eulers = math.Euler(targetTransform.Rotation);
            // eulers.y = cameraYaw; // TODO
            // mapCamera.transform.eulerAngles = eulers;
            
            CameraTexture = RenderTexture.GetTemporary(mapTextureSize, mapTextureSize);
            mapCamera.targetTexture = CameraTexture;

            mapEntity = em.CreateEntity(typeof(Map));
            em.AddComponentObject(mapEntity, this);

            using (var boundsQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapBounds>()))
            {
                if (boundsQuery.HasSingleton<MapBounds>())
                {
                    hasMapBounds = true;
                    var bounds = boundsQuery.GetSingleton<MapBounds>();
                    mapBounds = bounds.Bounds;
                    
                    Debug.Log("map bounds has been set");
                }
            }

            // if(EndlessGameState.IsMobilePlatform == false)
            // {
            //     rt.width = hdMapTextureSize;
            //     rt.height = hdMapTextureSize;
            //     rt.antiAliasing = 8;
            // }
        }

        private void OnDestroy()
        {
            if (CameraTexture != null)
            {
                RenderTexture.ReleaseTemporary(CameraTexture);
                CameraTexture = null;
            }

            if (entityManager.Equals(default) == false && entityManager.Exists(mapEntity))
            {
                entityManager.DestroyEntity(mapEntity);
            }
        }
    }
}
