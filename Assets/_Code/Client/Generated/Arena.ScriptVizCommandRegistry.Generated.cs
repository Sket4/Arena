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
		}
	}
}
