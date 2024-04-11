using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Collections;
using Unity.Entities;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(ApplyDamageSystem))]
    [UpdateBefore(typeof(DestroyHitSystem))]
    public partial class HitSyncSystem : GameSystemBase, IRpcProcessor
    {
        public NetworkIdentity NetIdentity { get; set; }
        bool isServer = false;
        EntityQuery hitsQuery = default;
        const int MaxHitsToSend = 32;
        NativeList<HitInfo> hitInfoBuffer = default;
        EntityArchetype hitArchetype = default;

        public struct HitInfo
        {
            public FixedFloat3_8byte HitPosition;
            public ushort Damage;
            public bool IsCritical;
        }

        public HitSyncSystem(bool isServer)
        {
            this.isServer = isServer;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            hitsQuery = GetEntityQuery(ComponentType.ReadOnly<Hit>(), ComponentType.ReadOnly<Damage>());
            hitInfoBuffer = new NativeList<HitInfo>(MaxHitsToSend, Allocator.Persistent);

            if(isServer == false)
            {
                hitArchetype = EntityManager.CreateArchetype(typeof(Hit), typeof(Damage), typeof(CriticalHitState));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            hitInfoBuffer.Dispose();
        }

        protected override void OnSystemUpdate()
        {
            if(isServer)
            {
                onServerUpdate();
            }
        }

        void onServerUpdate()
        {
            if(hitsQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            var hitChunks = CreateArchetypeChunkArrayWithUpdateAllocator(hitsQuery);
            var hitType = GetComponentTypeHandle<Hit>(true);
            var damageType = GetComponentTypeHandle<Damage>(true);
            var critStateType = GetComponentTypeHandle<CriticalHitState>(true);

            Entities
                .WithReadOnly(hitChunks)
                .WithDisposeOnCompletion(hitChunks)
                .WithoutBurst()
                .ForEach((in NetworkPlayer player, in ControlledCharacter controlledCharacter) =>
            {
                hitInfoBuffer.Clear();

                foreach(var chunk in hitChunks)
                {
                    var hits = chunk.GetNativeArray(hitType);
                    var damages = chunk.GetNativeArray(damageType);
                    
                    NativeArray<CriticalHitState> critStates = default;
                    if(chunk.Has(critStateType))
                    {
                        critStates = chunk.GetNativeArray(critStateType);
                    }

                    for(int i=0; i<hits.Length; i++)
                    {
                        var hit = hits[i];

                        if(hit.Instigator != controlledCharacter.Entity)
                        {
                            continue;
                        }

                        bool isCritical = critStates.IsCreated ? critStates[i].IsCritical : false;

                        var newHit = new HitInfo
                        {
                            Damage = (ushort)damages[i].Value,
                            HitPosition = new FixedFloat3_8byte(hit.Position),
                            IsCritical = isCritical
                        };
                        hitInfoBuffer.Add(newHit);

                        if(hitInfoBuffer.Length == MaxHitsToSend)
                        {
                            break;
                        }
                    }

                    if(hitInfoBuffer.Length == MaxHitsToSend)
                    {
                        break;
                    }
                }

                if(hitInfoBuffer.Length == 0)
                {
                    return;
                }

                //UnityEngine.Debug.Log($"Sending {hitInfoBuffer.Length} hits");
                this.RPCWithNativeArray(SendHitsToClient, player, hitInfoBuffer.AsArray());

            }).Run();
        }

        [RemoteCall(canBeCalledByNonOwner:false, canBeCalledFromClient:false)]
        public void SendHitsToClient(NativeArray<HitInfo> hitInfos, EntityCommandBuffer commands)
        {
            if(TryGetSingletonEntity<LocalPlayerTag>(out Entity localPlayerEntity) == false)
            {
                UnityEngine.Debug.Log("discarding received hits - no local player found");
                return;
            }
            var controllerCharacter = GetComponent<ControlledCharacter>(localPlayerEntity);

            foreach(var hit in hitInfos)
            {
                var hitEntity = commands.CreateEntity(hitArchetype);
                commands.SetComponent(hitEntity, new Hit
                {
                    Position = hit.HitPosition.ToFloat3(),
                    Instigator = controllerCharacter.Entity
                });

                commands.SetComponent(hitEntity, new Damage { Value = hit.Damage });
                commands.SetComponent(hitEntity, new CriticalHitState { IsCritical = hit.IsCritical });
            }
        }
    }
}

