using UnityEngine;

namespace Arena
{
    public class PowerAttackSMB : StateMachineBehaviour 
	{
		[SerializeField]
		GameObject effect = default;

		Transform effectInstance;

		public override void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateEnter (animator, stateInfo, layerIndex);
			var instance = Instantiate (effect);
			effectInstance = instance.transform;
			effectInstance.SetParent (animator.transform);
			effectInstance.localPosition = Vector3.zero;
			effectInstance.localRotation = Quaternion.identity;
		}

		public override void OnStateExit (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateExit (animator, stateInfo, layerIndex);
			if (effectInstance != null) 
			{
				Destroy (effectInstance.gameObject);
				effectInstance = null;
			}
		}
	}
}
