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
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.ShowMessageCommand), Arena.ScriptViz.ShowMessageCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.HideMessageCommand), Arena.ScriptViz.HideMessageCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.GetMainCharacterCommand), Arena.ScriptViz.GetMainCharacterCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.QuestActiveCheckCommand), Arena.ScriptViz.QuestActiveCheckCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.GameProgressFlagCheckCommand), Arena.ScriptViz.GameProgressFlagCheckCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.SetGameProgressQuestCommand), Arena.ScriptViz.SetGameProgressQuestCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.AddGameProgressFlagCommand), Arena.ScriptViz.AddGameProgressFlagCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.SetGameProgressKeyCommand), Arena.ScriptViz.SetGameProgressKeyCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.GetProgressKeyValueCommand), Arena.ScriptViz.GetProgressKeyValueCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.GetGameProgressCommand), Arena.ScriptViz.GetGameProgressCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.SetBaseLocationCommand), Arena.ScriptViz.SetBaseLocationCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.StartQuestCommand), Arena.ScriptViz.StartQuestCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.ActivateZoneRequestCommand), Arena.ScriptViz.ActivateZoneRequestCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.ScriptViz.MoveOnPathCommand), Arena.ScriptViz.MoveOnPathCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.Dialogue.ShowDialogueCommand), Arena.Dialogue.ShowDialogueCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(Arena.Dialogue.StartDialogueCommand), Arena.Dialogue.StartDialogueCommand.Exec);
		}
	}
}
