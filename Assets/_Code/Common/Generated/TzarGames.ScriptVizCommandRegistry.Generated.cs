using TzarGames.GameCore.ScriptViz;
using UnityEngine;

namespace TzarGames.GameCore.Generated
{
	public static class TzarGames_ScriptVizCommandsMapping
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		static unsafe void init()
		{
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddIntCommand), TzarGames.GameCore.ScriptViz.AddIntCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddFloatCommand), TzarGames.GameCore.ScriptViz.AddFloatCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetDeltaTimeCommand), TzarGames.GameCore.ScriptViz.GetDeltaTimeCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.DestroyCommand), TzarGames.GameCore.ScriptViz.DestroyCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.LogCommand), TzarGames.GameCore.ScriptViz.LogCommand.Print);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetSimpleStructVariableCommand), TzarGames.GameCore.ScriptViz.GetSimpleStructVariableCommand.Get);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetConstantCommand), TzarGames.GameCore.ScriptViz.GetConstantCommand.Get);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetConstanEntityCommand), TzarGames.GameCore.ScriptViz.GetConstanEntityCommand.Get);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetEntityVariableCommand), TzarGames.GameCore.ScriptViz.GetEntityVariableCommand.Get);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetSimpleStructVariableCommand), TzarGames.GameCore.ScriptViz.SetSimpleStructVariableCommand.Set);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetSimpleStructVariableCommandFromConstant), TzarGames.GameCore.ScriptViz.SetSimpleStructVariableCommandFromConstant.Set);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetEntityVariableCommand), TzarGames.GameCore.ScriptViz.SetEntityVariableCommand.Set);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.EntityNullCheckCommand), TzarGames.GameCore.ScriptViz.EntityNullCheckCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.InstantiateCommand), TzarGames.GameCore.ScriptViz.InstantiateCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetTransformCommand), TzarGames.GameCore.ScriptViz.SetTransformCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetTargetCommand), TzarGames.GameCore.ScriptViz.SetTargetCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.ChangeDisabledState), TzarGames.GameCore.ScriptViz.ChangeDisabledState.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.ChangeColliderState), TzarGames.GameCore.ScriptViz.ChangeColliderState.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SendMessageCommand), TzarGames.GameCore.ScriptViz.SendMessageCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.BranchCommand), TzarGames.GameCore.ScriptViz.BranchCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.IntSwitchCommand), TzarGames.GameCore.ScriptViz.IntSwitchCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.CompareFloatCommand), TzarGames.GameCore.ScriptViz.CompareFloatCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.DelayCommand), TzarGames.GameCore.ScriptViz.DelayCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.SequenceCommand), TzarGames.GameCore.ScriptViz.Graph.SequenceCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.RandomIntCommand), TzarGames.GameCore.ScriptViz.Graph.RandomIntCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.RandomFloatCommand), TzarGames.GameCore.ScriptViz.Graph.RandomFloatCommand.Exec);
		}
	}
}
