using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
namespace Arena.Client
{
    [BurstCompile]
    partial struct UpdateJob : IJobEntity
    {
        [ReadOnly] public NativeArray<ArchetypeChunk> GenerationSettingsChunks;
        [ReadOnly] public NativeArray<ArchetypeChunk> CellChunks;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<TerrainDetailGenerationSettings> SettingsType;
        [ReadOnly] public SharedComponentTypeHandle<TerrainDetailCellSharedData> SharedCellDataType;
        [ReadOnly] public ComponentTypeHandle<TerrainDetailCell> CellType;
        [ReadOnly] public ComponentLookup<TerrainPhysicsMaterial> TerrainMaterialLookup;

        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;

        public Message DestroyMessage;
        public EntityArchetype MessageArchetype;
        public EntityCommandBuffer.ParallelWriter Commands;
        
        public void Execute(Entity targetEntity, [ChunkIndexInQuery] int sortIndex, in LocalTransform targetTransform, in TerrainDetailTargetLastPosition targetLastPosition, in TerrainDetailTarget targetData)
        {
            var playerPos = targetTransform.Position;
            var snapPos = float2.zero;
            snapPos.x = math.floor(playerPos.x / targetData.DetailCheckDistanceTreshold + math.EPSILON);
            snapPos.x *= targetData.DetailCheckDistanceTreshold;
            snapPos.y = math.floor(playerPos.z / targetData.DetailCheckDistanceTreshold + math.EPSILON);
            snapPos.y *= targetData.DetailCheckDistanceTreshold;

            var lastPositionDistance = math.distance(snapPos, targetLastPosition.Value);
            
            if (lastPositionDistance < 0.001f)
            {
                return;
            }
            Commands.SetComponent(sortIndex, targetEntity, new TerrainDetailTargetLastPosition
            {
                Value = snapPos
            });

            foreach (var settingsChunk in GenerationSettingsChunks)
            {
                var settingsArray = settingsChunk.GetNativeArray(ref SettingsType);
                var settingsEntities = settingsChunk.GetNativeArray(EntityType);

                for (var index = 0; index < settingsArray.Length; index++)
                {
                    var settings = settingsArray[index];
                    var settingsEntity = settingsEntities[index];
                    var radius = settings.SpawnRadius;
                    
                    // проверка на дистанцию
                    foreach (var cellChunk in CellChunks)
                    {
                        var cellSharedData = cellChunk.GetSharedComponent(SharedCellDataType);
                                    
                        if (cellSharedData.Equals(settingsEntity) == false)
                        {
                            continue;
                        }

                        var cells = cellChunk.GetNativeArray(ref CellType);
                        var cellEntities = cellChunk.GetNativeArray(EntityType);

                        for (var i = 0; i < cells.Length; i++)
                        {
                            var cell = cells[i];
                            
                            var distance = math.distance(cell.WorldPosition, playerPos.xz);
                            var destroy = false;
                            
                            if (distance > radius)
                            {
                                destroy = true;
                            }
                            else
                            {
                                if (TerrainDetailCell.CheckDensity(cell.Hash, settings.Density) == false)
                                {
                                    destroy = true;
                                }
                            }
                            
                            if (destroy)
                            {
                                var messageEntity = Commands.CreateEntity(sortIndex, MessageArchetype);
                                Commands.SetComponent(sortIndex, messageEntity, DestroyMessage);
                                var messageTargets = Commands.SetBuffer<Targets>(sortIndex, messageEntity);
                                messageTargets.Add(new Targets(cellEntities[i]));
                                Commands.AddComponent(sortIndex, cellEntities[i], new DestroyTimer(1));
                            }
                        }
                    }
                    
                    if (settings.CellSize < math.EPSILON
                        || settings.SpawnRadius < math.EPSILON
                        || settings.Prefab == Entity.Null)
                    {
                        continue;
                    }

                    
                    var cellSize = settings.CellSize;
                    var halfCellSize = cellSize * 0.5f;

                    // draw center
                    //Debug.DrawRay(playerPos, math.up(), Color.red);
                    //DebugDraw.Circle(playerPos, radius, math.up(), Color.yellow, 32);

                    var diameter = radius * 2;
                    var maxCells = (int)(math.round(diameter / cellSize) + float.Epsilon) + 1;
                    
                    var playerCellPosition = playerPos;
                    playerCellPosition.x = ((int)math.round(playerCellPosition.x / cellSize)) * cellSize;
                    playerCellPosition.z = ((int)math.round(playerCellPosition.z / cellSize)) * cellSize;
                    playerCellPosition.y = 0;

                    // draw grid
                    //var shift = radius + halfCellSize;

                    // var ray = new float3(0, 0, 1) * (diameter + cellSize);
                    //
                    // for (int x = 0; x < maxCells + 1; x++)
                    // {
                    //     var coord = new float3(x * cellSize - shift, 0, -shift);
                    //     coord += playerCellPosition;
                    //     Debug.DrawRay(coord, ray, Color.blue);
                    // }
                    //
                    // ray = new float3(1, 0, 0) * (diameter + cellSize);
                    //
                    // for (int z = 0; z < maxCells + 1; z++)
                    // {
                    //     var coord = new float3(-shift, 0, z * cellSize - shift);
                    //     coord += playerCellPosition;
                    //     Debug.DrawRay(coord, ray, Color.blue);
                    // }

                    for (int x = 0; x < maxCells; x++)
                    {
                        for (int z = 0; z < maxCells; z++)
                        {
                            var coord = new float3(x * cellSize, 0, z * cellSize);
                            coord.x -= radius;
                            coord.z -= radius;

                            coord += playerCellPosition;

                            var distance = math.distance(coord.xz, playerPos.xz);

                            if (distance > radius)
                            {
                                //Debug.DrawRay(coord, math.up() * 0.1f, Color.red);
                            }
                            else
                            {
                                var hash = coord.xz.GetHashCode();
                                var hashUint = UnsafeUtility.As<int, uint>(ref hash);

                                if (TerrainDetailCell.CheckDensity(hashUint, settings.Density, out var random) == false)
                                {
                                    continue;
                                }

                                var disp = random.NextFloat2(new float2(-1), new float2(1));
                                disp *= settings.RelaxFactor * halfCellSize;

                                var displacedCoord = coord;
                                displacedCoord.xz += disp;

                                // ячейка должна быть заспаунена, проверяем, была ли она создана до этого
                                var isCreated = false;
                                
                                foreach (var cellChunk in CellChunks)
                                {
                                    var cellSharedData = cellChunk.GetSharedComponent(SharedCellDataType);
                                    
                                    if (cellSharedData.Equals(settingsEntity) == false)
                                    {
                                        continue;
                                    }

                                    var cells = cellChunk.GetNativeArray(ref CellType);

                                    foreach (var cell in cells)
                                    {
                                        if (cell.WorldPosition.Equals(coord.xz))
                                        {
                                            isCreated = true;
                                            break;
                                        }
                                    }

                                    if (isCreated)
                                    {
                                        break;
                                    }
                                }

                                if (isCreated == false)
                                {
                                    var traceStart = new float3(displacedCoord.x,
                                        playerPos.y + settings.TraceVerticalOffset, displacedCoord.z);
                                    var traceEnd = new float3(displacedCoord.x,
                                        playerPos.y - settings.TraceVerticalOffset, displacedCoord.z);

                                    var raycastInput = new RaycastInput
                                    {
                                        Start = traceStart,
                                        End = traceEnd,
                                        Filter = settings.TraceLayers
                                    };
                                    
                                    //Debug.DrawLine(traceStart, traceEnd, Color.cyan, 5);
                                    
                                    if (PhysicsWorld.CastRay(raycastInput, out var hit) && TerrainMaterialLookup.TryGetComponent(hit.Entity, out var terrainMat))
                                    {
                                        var layer = terrainMat.WorldPositionToLayer(hit.Position, out var strength);
                                        var strengthPass = strength >= settings.MinimalLayerStrength;
                                        
                                        if (strengthPass && (settings.PhysicsMaterialTags & layer) > 0)
                                        {
                                            var cellInstance = Commands.Instantiate(sortIndex, settings.Prefab);
                                            var dir = random.NextFloat2Direction();
                                    
                                            var rotation = quaternion.LookRotation(new float3(dir.x, 0, dir.y), math.up());
                                            Commands.SetComponent(sortIndex, cellInstance, new TerrainDetailCell
                                            {
                                                WorldPosition = coord.xz,
                                                Hash = hashUint
                                            });
                                        
                                            Commands.SetSharedComponent(sortIndex, cellInstance, new TerrainDetailCellSharedData(settingsEntity));
                                            var scale = random.NextFloat(settings.MinScale, settings.MaxScale);
                                            scale *= strength;
                                            Commands.SetComponent(sortIndex, cellInstance, LocalTransform.FromPositionRotationScale(hit.Position, rotation, scale));    
                                        
                                            //Debug.DrawRay(coord, math.up() * 0.1f, Color.green);
                                            //Debug.DrawRay(displacedCoord, math.up() * 0.1f, Color.yellow);
                                            //Debug.DrawLine(coord, displacedCoord, Color.yellow);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    [BurstCompile]
    [ClientSystem]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    //[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct TerrainDetailSystem : ISystem
    {
        private EntityQuery settingsQuery;
        private EntityQuery cellQuery;
        private EntityTypeHandle entityType;
        private ComponentTypeHandle<TerrainDetailCell> cellType;
        private ComponentTypeHandle<TerrainDetailGenerationSettings> settingsType;
        private SharedComponentTypeHandle<TerrainDetailCellSharedData> cellSharedDataType;
        private ComponentLookup<TerrainPhysicsMaterial> terrainMaterialLookup;
        EntityArchetype messageArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            settingsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<TerrainDetailGenerationSettings>());

            cellQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<TerrainDetailCell>(), ComponentType.ReadOnly<TerrainDetailCellSharedData>()
                },
                None = new [] { ComponentType.ReadOnly<DestroyTimer>() }
            });

            messageArchetype = state.EntityManager.CreateArchetype(typeof(Message), typeof(Targets));
            
            entityType = state.GetEntityTypeHandle();
            cellType = state.GetComponentTypeHandle<TerrainDetailCell>(true);
            cellSharedDataType = state.GetSharedComponentTypeHandle<TerrainDetailCellSharedData>();
            settingsType = state.GetComponentTypeHandle<TerrainDetailGenerationSettings>(true);
            terrainMaterialLookup = state.GetComponentLookup<TerrainPhysicsMaterial>(true);
         
            state.RequireForUpdate<GameCommandBufferSystem.Singleton>();
            state.RequireForUpdate(settingsQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var commands = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>().CreateCommandBuffer(state.World.Unmanaged);
            
            var settingsChunks = settingsQuery.ToArchetypeChunkListAsync(Allocator.TempJob, state.Dependency,
                out var playerDeps);
            state.Dependency = playerDeps;

            var cellChunks = cellQuery.ToArchetypeChunkListAsync(Allocator.TempJob, state.Dependency, out var cellDeps);
            state.Dependency = cellDeps;
            
            entityType.Update(ref state);
            cellType.Update(ref state);
            cellSharedDataType.Update(ref state);
            settingsType.Update(ref state);
            terrainMaterialLookup.Update(ref state);
            
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            var job = new UpdateJob
            {
                Commands = commands.AsParallelWriter(),
                GenerationSettingsChunks = settingsChunks.AsDeferredJobArray(),
                SettingsType = settingsType,
                CellChunks = cellChunks.AsDeferredJobArray(),
                EntityType = entityType,
                SharedCellDataType = cellSharedDataType,
                CellType = cellType,
                PhysicsWorld = physicsWorld,
                TerrainMaterialLookup = terrainMaterialLookup,
                MessageArchetype = messageArchetype,
                DestroyMessage = ArenaMessages.DestroyMessage
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency = settingsChunks.Dispose(state.Dependency);
            state.Dependency = cellChunks.Dispose(state.Dependency);
        }
    }
}
