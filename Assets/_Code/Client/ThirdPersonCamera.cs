using Arena.Client.ScriptViz;
using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
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
        [HideInAuthoring] public Entity Target;
        [HideInAuthoring] public float3 TargetPivotPositionPosition;
        [HideInAuthoring] public float3 Forward;
        public float CameraDistance;
        public float3 TargetOffset;
        public float CameraPitch;
        public float CameraYaw;
        public float MinCameraPitch;
        public float MaxCameraPitch;
        [HideInAuthoring]
        public uint AimPhysicsTags;
        public float TargetPivotTraceVerticalOffset;
        [HideInAuthoring]
        public uint CollisionPhysicsTags;
        public float CollisionTraceRadius;
        public float HorizontalViewSensitivity;
    }

    public class ThirdPersonCamera : MonoBehaviour
    {
        public LayerMask AimTraceLayers;
        public LayerMask CollisionTraceLayers;
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
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(RenderingSystem))]
    public partial class ThirdPersonCameraSystem : SystemBase
    {
        ThirdPersonCamera cameraPrefab;
        private EntityQuery sceneCameraSettingsQuery;
        private EntityQuery cameraQuery;
        private ComponentLookup<IgnoreCameraCollision> ignoreCollisionLookup;

        struct HitCollector : ICollector<ColliderCastHit>
        {
            public ComponentLookup<IgnoreCameraCollision> IgnoreLookup;
            public ColliderCastHit Hit;
            
            public bool AddHit(ColliderCastHit hit)
            {
                if (IgnoreLookup.HasComponent(hit.Entity))
                {
                    return false;
                }
                if (Hit.Entity == Entity.Null)
                {
                    Hit = hit;
                    return true;
                }

                if (Hit.Fraction < hit.Fraction)
                {
                    return false;
                }

                Hit = hit;
                return true;
            }

            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction { get; set; }
            public int NumHits { get; set; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            cameraPrefab = ClientGameSettings.Get.CameraPrefab.GetComponent<ThirdPersonCamera>();
            sceneCameraSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<SceneCameraSettings>());
            ignoreCollisionLookup = GetComponentLookup<IgnoreCameraCollision>(true);
            cameraQuery = GetEntityQuery(ComponentType.ReadOnly<ThirdPersonCamera>());
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
                    //Debug.Log("View dir " + viewDirection.Value);
                    var eulers = createdCamera.transform.eulerAngles;
                    //Debug.Log("Eulers " + eulers);
                    cameraData.CameraYaw = eulers.y;

                    cameraData.AimPhysicsTags = Utility.LayerMaskToCollidesWithMask(createdCamera.AimTraceLayers);
                    cameraData.CollisionPhysicsTags = Utility.LayerMaskToCollidesWithMask(createdCamera.CollisionTraceLayers);

                    EntityManager.SetComponentData(cameraEntity, cameraData);

                    EntityManager.AddComponentData(characterEntity, new AimHitPoint());
                    EntityManager.AddComponentData(characterEntity, new PlayerInput());

                    if (sceneCameraSettingsQuery.IsEmpty == false)
                    {
                        var settings = sceneCameraSettingsQuery.GetSingleton<SceneCameraSettings>();
                        if (settings.ChangeClearColor)
                        {
                            createdCamera.Camera.backgroundColor = settings.ClearColor;
                        }

                        if (settings.ChangeClearFlags)
                        {
                            createdCamera.Camera.clearFlags = settings.ClearFlags;
                        }

                        if (settings.ChangeFarPlane)
                        {
                            createdCamera.Camera.farClipPlane = settings.FarPlane;
                        }
                    }

                }).Run();
            
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity cameraEntity, ThirdPersonCamera camera, in CameraData cameraData) =>
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
                .ForEach((ref CameraData cameraData, ref LocalTransform transform) =>
                {
                    if(SystemAPI.HasComponent<CharacterAnimation>(cameraData.Target) == false)
                    {
                        return;
                    }

                    var animation = SystemAPI.GetComponent<CharacterAnimation>(cameraData.Target);

                    LocalToWorld targetL2W;

                    if (SystemAPI.HasComponent<LocalToWorld>(animation.AnimatorEntity) == false)
                    {
                        return;
                    }
                    targetL2W = SystemAPI.GetComponent<LocalToWorld>(animation.AnimatorEntity);

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

            ignoreCollisionLookup.Update(this);
            var ignoreLookup = ignoreCollisionLookup;

            Entities.ForEach((ref LocalTransform transform, in CameraData cameraData) =>
            {
                var collisionFilter = CollisionFilter.Default;
                
                collisionFilter.CollidesWith = cameraData.CollisionPhysicsTags;

                // TRACE COLLISION
                var traceStartPos = cameraData.TargetPivotPositionPosition + math.up() * cameraData.TargetPivotTraceVerticalOffset;
                var traceDir = transform.Position - traceStartPos;
                traceDir = math.normalizesafe(traceDir, -cameraData.Forward);

                Debug.DrawRay(traceStartPos, traceDir, Color.red);

                var hitCollector = new HitCollector
                {
                    IgnoreLookup = ignoreLookup,
                    MaxFraction = 1
                };

                if (physWorld.SphereCastCustom(traceStartPos, cameraData.CollisionTraceRadius, traceDir, cameraData.CameraDistance, ref hitCollector, collisionFilter, QueryInteraction.IgnoreTriggers))
                {
                    transform.Position = traceStartPos + traceDir * cameraData.CameraDistance * math.min(1, hitCollector.Hit.Fraction);
                }
                
                // TRACE AIM
                if(SystemAPI.HasComponent<AimHitPoint>(cameraData.Target))
                {
                    var traceEnd = transform.Position + cameraData.Forward * 1000.0f;
                
                    collisionFilter = CollisionFilter.Default;
                    collisionFilter.CollidesWith = cameraData.AimPhysicsTags;

                    var raycastInput = new RaycastInput
                    {
                        Start = transform.Position + cameraData.Forward * 0.01f,
                        End = traceEnd,
                        Filter = collisionFilter
                    };
                    
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
                    
                    // #if UNITY_EDITOR
                    // Debug.DrawLine(raycastInput.Start, raycastInput.End, Color.red);
                    // DebugDraw.DrawSphere(aimHitPoint.Value, 0.1f, Color.red);
                    // #endif
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

            var cmd = new EntityCommandBuffer(Allocator.Temp);
            
            Entities
                .WithoutBurst()
                .WithAll<CameraShakeRequest>()
                .ForEach((Entity entity) =>
                {
                    var camera = cameraQuery.GetSingleton<ThirdPersonCamera>();
                    camera.ShakeQuick();
                    cmd.DestroyEntity(entity);
                
                }).Run();
            
            cmd.Playback(EntityManager);
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
