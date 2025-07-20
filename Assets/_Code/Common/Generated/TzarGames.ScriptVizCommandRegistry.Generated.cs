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
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.LerpFloat3Command), TzarGames.GameCore.ScriptViz.LerpFloat3Command.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.LerpQuaternionCommand), TzarGames.GameCore.ScriptViz.LerpQuaternionCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.MultiplyQuatWithFloat3Command), TzarGames.GameCore.ScriptViz.MultiplyQuatWithFloat3Command.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.QuaternionForwardCommand), TzarGames.GameCore.ScriptViz.QuaternionForwardCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Float3DecomposeCommand), TzarGames.GameCore.ScriptViz.Float3DecomposeCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Float3ComposeCommand), TzarGames.GameCore.ScriptViz.Float3ComposeCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.LookRotationCommand), TzarGames.GameCore.ScriptViz.LookRotationCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.QuaternionAxisAngleCommand), TzarGames.GameCore.ScriptViz.QuaternionAxisAngleCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddIntCommand), TzarGames.GameCore.ScriptViz.AddIntCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddFloat3Command), TzarGames.GameCore.ScriptViz.AddFloat3Command.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.MultiplyQuaternionCommand), TzarGames.GameCore.ScriptViz.MultiplyQuaternionCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddFloatCommand), TzarGames.GameCore.ScriptViz.AddFloatCommand.Add);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.MultiplyFloatCommand), TzarGames.GameCore.ScriptViz.MultiplyFloatCommand.Add);
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
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.EntityCompareCommand), TzarGames.GameCore.ScriptViz.EntityCompareCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.EntityNullCheckCommand), TzarGames.GameCore.ScriptViz.EntityNullCheckCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.InstantiateCommand), TzarGames.GameCore.ScriptViz.InstantiateCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetTransformCommand), TzarGames.GameCore.ScriptViz.SetTransformCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetTargetCommand), TzarGames.GameCore.ScriptViz.SetTargetCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.ChangeDisabledState), TzarGames.GameCore.ScriptViz.ChangeDisabledState.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.EntityFromObjectKey), TzarGames.GameCore.ScriptViz.EntityFromObjectKey.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.ChangeColliderState), TzarGames.GameCore.ScriptViz.ChangeColliderState.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SendMessageCommand), TzarGames.GameCore.ScriptViz.SendMessageCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.CreateMessageWithIntCommand), TzarGames.GameCore.ScriptViz.CreateMessageWithIntCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SendMessageToSelfCommand), TzarGames.GameCore.ScriptViz.SendMessageToSelfCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.BranchCommand), TzarGames.GameCore.ScriptViz.BranchCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.IntSwitchCommand), TzarGames.GameCore.ScriptViz.IntSwitchCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.CompareFloatCommand), TzarGames.GameCore.ScriptViz.CompareFloatCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.CompareIntCommand), TzarGames.GameCore.ScriptViz.CompareIntCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.CompareMessagesCommand), TzarGames.GameCore.ScriptViz.CompareMessagesCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.RegisterMessageListenerCommand), TzarGames.GameCore.ScriptViz.RegisterMessageListenerCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.DelayCommand), TzarGames.GameCore.ScriptViz.DelayCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetSelfCommand), TzarGames.GameCore.ScriptViz.GetSelfCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.GetTransformCommand), TzarGames.GameCore.ScriptViz.GetTransformCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.NotCommand), TzarGames.GameCore.ScriptViz.NotCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.WriteTransformCommand), TzarGames.GameCore.ScriptViz.WriteTransformCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.TryGetTransformFromLookupCommand), TzarGames.GameCore.ScriptViz.TryGetTransformFromLookupCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.SequenceCommand), TzarGames.GameCore.ScriptViz.Graph.SequenceCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.RandomIntCommand), TzarGames.GameCore.ScriptViz.Graph.RandomIntCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.RandomFloatCommand), TzarGames.GameCore.ScriptViz.Graph.RandomFloatCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<Unity.Transforms.Parent>), TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<Unity.Transforms.Parent>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.IsInStateCheckCommand), TzarGames.GameCore.ScriptViz.Graph.IsInStateCheckCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Graph.GoToStateAction), TzarGames.GameCore.ScriptViz.Graph.GoToStateAction.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.GoToPositionCommand), TzarGames.GameCore.ScriptViz.Commands.GoToPositionCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.StopGoToPositionCommand), TzarGames.GameCore.ScriptViz.Commands.StopGoToPositionCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.ModifyHealthCommand), TzarGames.GameCore.ScriptViz.Commands.ModifyHealthCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.UseAbilityCommand), TzarGames.GameCore.ScriptViz.Commands.UseAbilityCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.SetForceCommand), TzarGames.GameCore.ScriptViz.Commands.SetForceCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.AddOrRemoveModificatorCommand<TzarGames.GameCore.SpeedModificator,TzarGames.GameCore.SpeedModificatorChangeRequest>), TzarGames.GameCore.ScriptViz.Commands.AddOrRemoveModificatorCommand<TzarGames.GameCore.SpeedModificator,TzarGames.GameCore.SpeedModificatorChangeRequest>.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.AddOrRemoveModificatorCommand<TzarGames.GameCore.DamageModificator,TzarGames.GameCore.DamageModificatorChangeRequest>), TzarGames.GameCore.ScriptViz.Commands.AddOrRemoveModificatorCommand<TzarGames.GameCore.DamageModificator,TzarGames.GameCore.DamageModificatorChangeRequest>.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetComponentEnabledCommand<TzarGames.GameCore.MoveAroundPoint>), TzarGames.GameCore.ScriptViz.SetComponentEnabledCommand<TzarGames.GameCore.MoveAroundPoint>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetComponentEnabledCommand<TzarGames.GameCore.InteractiveObject>), TzarGames.GameCore.ScriptViz.SetComponentEnabledCommand<TzarGames.GameCore.InteractiveObject>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetComponentEnabledCommand<TzarGames.GameCore.FollowTarget>), TzarGames.GameCore.ScriptViz.SetComponentEnabledCommand<TzarGames.GameCore.FollowTarget>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetComponentOperationCommand<TzarGames.GameCore.AttachToTarget>), TzarGames.GameCore.ScriptViz.SetComponentOperationCommand<TzarGames.GameCore.AttachToTarget>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddComponentOperationCommand<TzarGames.GameCore.AttachToTarget>), TzarGames.GameCore.ScriptViz.AddComponentOperationCommand<TzarGames.GameCore.AttachToTarget>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<TzarGames.GameCore.AttachToTarget>), TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<TzarGames.GameCore.AttachToTarget>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.SetComponentOperationCommand<TzarGames.GameCore.FollowTargetMode>), TzarGames.GameCore.ScriptViz.SetComponentOperationCommand<TzarGames.GameCore.FollowTargetMode>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.AddComponentOperationCommand<TzarGames.GameCore.FollowTargetMode>), TzarGames.GameCore.ScriptViz.AddComponentOperationCommand<TzarGames.GameCore.FollowTargetMode>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<TzarGames.GameCore.FollowTargetMode>), TzarGames.GameCore.ScriptViz.RemoveComponentOperationCommand<TzarGames.GameCore.FollowTargetMode>.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.AddOrRemoveItemCommand), TzarGames.GameCore.ScriptViz.Commands.AddOrRemoveItemCommand.Execute);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.FriendshipCheckCommand), TzarGames.GameCore.ScriptViz.Commands.FriendshipCheckCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.GetTargetHealthCommand), TzarGames.GameCore.ScriptViz.Commands.GetTargetHealthCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.ScriptViz.Commands.GetViewDirectionCommand), TzarGames.GameCore.ScriptViz.Commands.GetViewDirectionCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.Abilities.RequestAbilityStopCommand), TzarGames.GameCore.Abilities.RequestAbilityStopCommand.Exec);
			ScriptVizCommandRegistry.RegisterCommand(typeof(TzarGames.GameCore.Abilities.GetDurationCommand), TzarGames.GameCore.Abilities.GetDurationCommand.Exec);
		}
	}
}
