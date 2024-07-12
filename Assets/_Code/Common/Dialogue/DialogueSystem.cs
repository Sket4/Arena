using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using Unity.Entities;
using UnityEngine;

namespace Arena.Dialogue
{
    public struct DialogueAnswerSignal : IComponentData
    {
        public Entity ScriptVizEntity;
        public Address CommandAddress;
    }
    
    [RequireMatchingQueriesForUpdate]
    public partial class DialogueSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            Entities.ForEach((Entity entity, int entityInQueryIndex, in DialogueAnswerSignal signal) =>
            {
                commands.DestroyEntity(entityInQueryIndex, entity);

                var aspect = SystemAPI.GetAspect<ScriptVizAspect>(signal.ScriptVizEntity);
                            
                var codeBytes = SystemAPI.GetBuffer<CodeDataByte>(aspect.CodeInfo.ValueRO.CodeDataEntity);
                var constEntityVarData = SystemAPI.GetBuffer<ConstantEntityVariableData>(aspect.CodeInfo.ValueRO.CodeDataEntity);

                using (var contextHandle = new ContextDisposeHandle(codeBytes, constEntityVarData, ref aspect, ref commands, entityInQueryIndex, deltaTime))
                {
                    if (signal.CommandAddress.IsInvalid)
                    {       
                        Debug.LogError($"invalid command address {signal.CommandAddress.Value}");
                        return;
                    }
                    contextHandle.Execute(signal.CommandAddress);  
                }

            }).Run();
        }
    }
}
