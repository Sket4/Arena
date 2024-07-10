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
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.GameProgressFlagCheckCommand), Arena.ScriptViz.GameProgressFlagCheckCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.AddGameProgressFlagCommand), Arena.ScriptViz.AddGameProgressFlagCommand.Execute);
		}
	}
}
