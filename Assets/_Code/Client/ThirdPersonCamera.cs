using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Arena.Client
{
    class ThirdPersonCameraReference : ICleanupComponentData
    {
        public ThirdPersonCamera Camera;
    }

    [System.Serializable]
    public struct CameraData : IComponentData
    {
        [System.NonSerialized] public Entity Target;
        [System.NonSerialized] public float3 TargetPivotPositionPosition;
        [System.NonSerialized] public float3 Forward;
        public float CameraDistance;
        public float3 TargetOffset;
        public float CameraPitch;
        public float CameraYaw;
        public float MinCameraPitch;
        public float MaxCameraPitch;
        public PhysicsCategoryTags AimPhysicsTags;
        public float TargetPivotTraceVerticalOffset;
        public PhysicsCategoryTags CollisionPhysicsTags;
        public float CollisionTraceRadius;
        public float HorizontalViewSensitivity;
    }

    public class ThirdPersonCamera : MonoBehaviour
    {
        public CameraData Settings = new CameraData
        {
            CameraDistance = 7,
            TargetOffset = math.up(),
            CameraPitch = 60,
            CameraYaw = 0,
            MinCameraPitch = 0,
            MaxCameraPitch = 85,
            CollisionTraceRadius = 0.25f,
            TargetPivotTraceVerticalOffset = 0.5f
        };

        public Cinemachine.CinemachineBrain CineBrain = default;

        [SerializeField]
        Cinemachine.CinemachineVirtualCamera cineCamera = default;


        private bool shaking = false;
        private float shakeStartTime = 0;
        private float shakeDuration = 0.5f;
        private float shakeSpeed = 4.0f;
        private float shakeMagnitude = 0.2f;
        private float shakeRandomStart = 0;

        public Camera Camera
        {
            get { return cachedCamera; }
        }

        public Transform CachedTransform { get; private set; }
        private Camera cachedCamera;

        private void Awake()
        {
            CachedTransform = transform;
            cachedCamera = GetComponent<Camera>();
        }

        public void DoLateUpdate()
        {
            if (shaking)
            {
                updateShake();
            }
        }

        //void OnDrawGizmos()
        //{
        //    var start = target.position + offset;
        //    var end = start - (updatedRotation * Vector3.forward) * cameraOffset;
        //    Gizmos.DrawLine(start, end);
        //}
        
        public void ShakeQuick()
        {
            shaking = true;
            shakeStartTime = Time.time;
            shakeRandomStart = Random.Range(750, 1000.0f) * Mathf.Sign(Random.Range(-1, 1));
            shakeDuration = 0.5f;
            shakeMagnitude = 0.1f;
            shakeSpeed = 5;
        }

        public void Shake(float duration, float speed, float magnitude)
        {
            shaking = true;
            shakeStartTime = Time.time;
            shakeRandomStart = Random.Range(750, 1000.0f) * Mathf.Sign(Random.Range(-1, 1));
            shakeDuration = duration;
            shakeMagnitude = magnitude;
            shakeSpeed = speed;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Shake")]
        void testShake()
        {
            Shake(0.75f, 5, 0.5f);
        }
#endif

        // —-----------------------------------------------------------------------
        void updateShake() 
        {
            float elapsed = Time.time - shakeStartTime;

            if (elapsed < shakeDuration) 
            {
                var originalPosition = CachedTransform.position;
                
                float percentComplete = elapsed / shakeDuration; 

// We want to reduce the shake from full power to 0 starting half way through
                float damper = 1.0f - Mathf.Clamp(2.0f * percentComplete - 1.0f, 0.0f, 1.0f);

// Calculate the noise parameter starting randomly and going as fast as speed allows
                float alpha = shakeRandomStart + shakeSpeed * percentComplete;

// map noise to [-1, 1]
                var px = Mathf.PerlinNoise(alpha, 0);
                var py = Mathf.PerlinNoise(0, alpha);
                float x = px * 2.0f - 1.0f;
                float y = py * 2.0f - 1.0f;

                x *= shakeMagnitude * damper;
                y *= shakeMagnitude * damper;

                var disp = new Vector3(x,y,0);
                disp = CachedTransform.rotation * disp;
                
                CachedTransform.position = originalPosition + disp;
            }
            else
            {
                shaking = false;
            }

            //cachedTransform.position = originalCamPos;
        }
    }

    

    [DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    //[UpdateBefore(typeof(CharacterControlSystem))]
    public partial class ThirdPersonCameraSystem : SystemBase
    {
        ThirdPersonCamera cameraPrefab;

        protected override void OnCreate()
        {
            base.OnCreate();
            cameraPrefab = ClientGameSettings.Get.CameraPrefab.GetComponent<ThirdPersonCamera>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Entities
                .WithoutBurst()
                .ForEach((ThirdPersonCamera camera) =>
                {
                    if (camera != null && camera)
                    {
                        Object.Destroy(camera.gameObject);
                    }
                }).Run();
        }

        protected override void OnUpdate()
        {
            var physWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<ThirdPersonCamera>()
                .ForEach((Entity entity, ThirdPersonCameraReference reference) =>
                {
                    Object.Destroy(reference.Camera);
                    EntityManager.RemoveComponent<ThirdPersonCameraReference>(entity);

                }).Run();

            Entities
                .WithChangeFilter<PlayerController>()
                .WithNone<PlayerInput>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity characterEntity, in PlayerController pc, in ViewDirection viewDirection) =>
                {
                    var player = EntityManager.GetComponentData<Player>(pc.Value);

                    if (player.ItsMe == false)
                    {
                        return;
                    }

                        //var pivot = new GameObject("Camera pivot");
                        //pivot.AddComponent<GameObjectEntityComponent>();
                        //var pivotTransform = pivot.transform;
                        //var lp = new LocalToParent
                        //{
                        //    Value = float4x4.TRS(new float3(), quaternion.identity, new float3(1))
                        //};

                        //var cameraPivotEntity = EntityManager.CreateEntity();

                        //Utility.AddGameObjectToEntity(pivot, EntityManager, cameraPivotEntity);

                        //EntityManager.AddComponentData(cameraPivotEntity, lp);
                        //EntityManager.AddComponentData(cameraPivotEntity, new LocalToWorld { Value = float4x4.identity });
                        //EntityManager.AddComponentData(cameraPivotEntity, new SyncTransformTag());
                        //EntityManager.AddComponentData(cameraPivotEntity, new Parent { Value = characterEntity });
                        //EntityManager.AddComponentData(cameraPivotEntity, new LinkEntityRequest { TargetOwner = characterEntity });

                        //pivot = EntityManager.GetComponentObject<Transform>(cameraPivotEntity).gameObject;

                        var createdCamera = Object.Instantiate(cameraPrefab);
                    var cameraEntity = EntityManager.CreateEntity(
                        typeof(ThirdPersonCamera),
                        typeof(ThirdPersonCameraReference),
                        typeof(CameraData),
                        typeof(LocalTransform),
                        typeof(LocalToWorld)
                    //typeof(Parent),
                    //typeof(LocalToParent),
                    );

                    EntityManager.SetComponentData(cameraEntity, new ThirdPersonCameraReference { Camera = createdCamera });
                    EntityManager.AddComponentObject(cameraEntity, createdCamera);

                    //var l2p = new LocalToParent { Value = float4x4.TRS(new float3(0,1,0), quaternion.identity, new float3(1)) };
                    //EntityManager.SetComponentData(cameraEntity, l2p);
                    //EntityManager.SetComponentData(cameraEntity, new Parent { Value = characterEntity });

                    // CINECAMERA
                    //createdCamera.cineCamera.LookAt = pivotTransform;
                    //createdCamera.cineCamera.Follow = pivotTransform;
                    // CINECAMERA END

                    var cameraData = createdCamera.Settings;
                    cameraData.Target = characterEntity;
                    createdCamera.transform.forward = viewDirection.Value;
                    Debug.Log("View dir " + viewDirection.Value);
                    var eulers = createdCamera.transform.eulerAngles;
                    Debug.Log("Eulers " + eulers);
                    cameraData.CameraYaw = eulers.y;

                    EntityManager.SetComponentData(cameraEntity, cameraData);

                    EntityManager.AddComponentData(characterEntity, new AimHitPoint());
                    EntityManager.AddComponentData(characterEntity, new PlayerInput());

                }).Run();
            
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity cameraEntity, ThirdPersonCamera camera, in CameraData cameraData, in LocalTransform transform) =>
                {
                    if (camera == false || camera.CachedTransform == false)
                    {
                        return;
                    }

                    if (EntityManager.Exists(cameraData.Target) == false
                        || EntityManager.HasComponent<CharacterAnimation>(cameraData.Target) == false)
                    {
                        EntityManager.RemoveComponent<ThirdPersonCamera>(cameraEntity);
                        Object.Destroy(camera.gameObject);
                        EntityManager.DestroyEntity(cameraEntity);
                        return;
                    }

                }).Run();

            

            Entities
                .ForEach((Entity cameraEntity, ref CameraData cameraData, ref LocalTransform transform) =>
                {
                    if(SystemAPI.HasComponent<CharacterAnimation>(cameraData.Target) == false)
                    {
                        return;
                    }

                    var animation = SystemAPI.GetComponent<CharacterAnimation>(cameraData.Target);

                    LocalToWorld targetL2W;

                    if (SystemAPI.HasComponent<LocalToWorld>(animation.AnimatorEntity))
                    {
                        targetL2W = SystemAPI.GetComponent<LocalToWorld>(animation.AnimatorEntity);
                    }
                    else
                    {
                        targetL2W = SystemAPI.GetComponent<LocalToWorld>(cameraData.Target);
                    }

                    var viewDirection = SystemAPI.GetComponent<ViewDirection>(cameraData.Target);
                    var input = SystemAPI.GetComponent<PlayerInput>(cameraData.Target);


                    cameraData.TargetPivotPositionPosition = targetL2W.Position + cameraData.TargetOffset;

                    cameraData.CameraYaw += input.ViewScroll.x * cameraData.HorizontalViewSensitivity;
                    cameraData.CameraPitch -= input.ViewScroll.y * cameraData.HorizontalViewSensitivity;
                    UpdateCamera(ref cameraData, ref transform);
                    

                    cameraData.Forward = math.forward(transform.Rotation);

                    viewDirection.Value = cameraData.Forward;

                    SetComponent(cameraData.Target, viewDirection);

                }).Run();

            Entities.ForEach((ref LocalTransform transform, in CameraData cameraData) =>
            {
                // TRACE AIM
                var traceEnd = transform.Position + cameraData.Forward * 1000.0f;
                var collisionFilter = CollisionFilter.Default;
                collisionFilter.CollidesWith = cameraData.AimPhysicsTags.Value;

                var raycastInput = new RaycastInput
                {
                    Start = transform.Position,
                    End = traceEnd,
                    Filter = collisionFilter
                };

                if(SystemAPI.HasComponent<AimHitPoint>(cameraData.Target))
                {
                    AimHitPoint aimHitPoint = default;

                    if (physWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit hit))
                    {
                        aimHitPoint.Value = hit.Position;
                    }
                    else
                    {
                        aimHitPoint.Value = traceEnd;
                    }

                    SystemAPI.SetComponent(cameraData.Target, aimHitPoint);
                    //DebugDraw.DrawSphere(aimHitPoint.Value, 0.1f, Color.red);
                }

                collisionFilter = CollisionFilter.Default;
                collisionFilter.CollidesWith = cameraData.CollisionPhysicsTags.Value;

                // TRACE COLLISION
                var traceStartPos = cameraData.TargetPivotPositionPosition + math.up() * cameraData.TargetPivotTraceVerticalOffset;
                var traceDir = transform.Position - traceStartPos;
                traceDir = math.normalizesafe(traceDir, -cameraData.Forward);

                Debug.DrawRay(traceStartPos, traceDir, Color.red);

                if (physWorld.SphereCast(traceStartPos, cameraData.CollisionTraceRadius, traceDir, cameraData.CameraDistance, out ColliderCastHit collisionHit, collisionFilter, QueryInteraction.IgnoreTriggers))
                {
                    transform.Position = traceStartPos + traceDir * cameraData.CameraDistance * collisionHit.Fraction;
                }

            }).Run();
            
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((ThirdPersonCamera camera, in LocalTransform transform) =>
                {
                    if (camera == false || camera.CachedTransform == false)
                    {
                        return;
                    }
                    
                    camera.CachedTransform.position = transform.Position;
                    camera.CachedTransform.rotation = transform.Rotation;
                    camera.DoLateUpdate();

                }).Run();
        }

        static void UpdateCamera(ref CameraData cameraData, ref LocalTransform transform)
        {
            Vector3 pos;
            Quaternion rot;

            cameraData.CameraPitch = Mathf.Clamp(cameraData.CameraPitch, cameraData.MinCameraPitch, cameraData.MaxCameraPitch);

            rot = Quaternion.AngleAxis(cameraData.CameraYaw, Vector3.up);
            rot *= Quaternion.AngleAxis(cameraData.CameraPitch, Vector3.right);

            var look = rot * Vector3.forward;
            pos = cameraData.TargetPivotPositionPosition - (cameraData.CameraDistance * (float3)look);

            transform.Rotation = rot;
            transform.Position = pos;
        }
    }
}
