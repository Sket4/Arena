using TzarGames.GameFramework;
using UnityEngine;

namespace Arena.Skills
{
    public class RadialAttackSMB : StateMachineBehaviour
	{
		[SerializeField]
		GameObject effect = default;

		Transform effectInstance;
		public float NormalizedTime { get; private set; }
		bool entered = false;

		public override void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateEnter (animator, stateInfo, layerIndex);
			//Debug.Log ("RSMB enter: " + stateInfo.normalizedTime);
			NormalizedTime = stateInfo.normalizedTime;
			entered = true;

			var parent = animator.transform;

			GameObject instance;

			if(Instantiator.IsPoolablePrefab(effect))
			{
				instance = Instantiator.InstantiateFromPool(effect, parent.position, parent.rotation, parent);
			}
			else
			{
				instance = Instantiate(effect, parent.position, parent.rotation, parent);
			}

			effectInstance = instance.transform;
		}

		public override void OnStateExit (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateExit (animator, stateInfo, layerIndex);
			//Debug.Log ("RSMB exit: " + stateInfo.normalizedTime);
			NormalizedTime = 0;
			entered = false;
			if (effectInstance != null) 
			{
				Destroy (effectInstance.gameObject);
				effectInstance = null;
			}
		}

		public override void OnStateUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			base.OnStateUpdate (animator, stateInfo, layerIndex);

			if (entered) 
			{
				NormalizedTime = Mathf.Clamp01(stateInfo.normalizedTime);
			} 
			else 
			{
				NormalizedTime = 0;
			}
		}
	}
}
