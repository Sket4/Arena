using TzarGames.GameCore.ScriptViz;
using UnityEngine;

namespace TzarGames.GameCore.Generated
{
	public static class TzarGamesGameCore_ScriptVizCommandsMapping
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		static unsafe void init()
		{
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.PlayAudioCommand), TzarGames.GameCore.ScriptViz.PlayAudioCommand.Exec);
		}
	}
}
