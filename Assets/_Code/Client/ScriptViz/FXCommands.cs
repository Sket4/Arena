using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using TzarGames.Rendering;
using Unity.Burst;
using Unity.Entities;

namespace Arena.Client.ScriptViz
{
    [System.Serializable]
    [FriendlyName("Particle system emission state")]
    public class SetParticleSystemEmissionStateNode : ComponentOperationNode<ParticleSystemEmissionState>
    {
    }

    [BurstCompile]
    public struct CameraShakeCommand : IScriptVizCommand
    {
        public bool Fake;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var evt = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.AddComponent(context.SortIndex, evt, new CameraShakeRequest());
        }
    }

    public struct CameraShakeRequest : IComponentData
    {
    }

    [FriendlyName("Camera shake")]
    public class CameraShakeNode : CommandNode
    {
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new CameraShakeCommand();
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Camera shake";
        }
    }
}
