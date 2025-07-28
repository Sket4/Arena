using TzarGames.GameCore.ScriptViz;
using UnityEngine;

namespace TzarGames.GameCore.Generated
{
	public static class Arena_ScriptVizCommandsMapping
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		static unsafe void init()
		{
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.Client.ScriptViz.PlayAnimationCommand), Arena.Client.ScriptViz.PlayAnimationCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.Client.ScriptViz.PlayCharacterAnimationCommand), Arena.Client.ScriptViz.PlayCharacterAnimationCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.Client.ScriptViz.StopCharacterAnimationCommand), Arena.Client.ScriptViz.StopCharacterAnimationCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetComponentOperationCommand<TzarGames.Rendering.ParticleSystemEmissionState>), TzarGames.GameCore.ScriptViz.SetComponentOperationCommand<TzarGames.Rendering.ParticleSystemEmissionState>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddComponentOperationCommand<TzarGames.Rendering.ParticleSystemEmissionState>), TzarGames.GameCore.ScriptViz.AddComponentOperationCommand<TzarGames.Rendering.ParticleSystemEmissionState>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<TzarGames.Rendering.ParticleSystemEmissionState>), TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<TzarGames.Rendering.ParticleSystemEmissionState>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.Client.ScriptViz.CameraShakeCommand), Arena.Client.ScriptViz.CameraShakeCommand.Exec);
		}
	}
}
