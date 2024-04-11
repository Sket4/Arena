using TzarGames.GameFramework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class ActionDetectorUI : MonoBehaviour
    {
        [SerializeField] private Image icon = default;
        [SerializeField] private GameObject container = default;

        //private ActionObjectDetector detector = default;
        //private IObjectAction currentAction;
        private Entity playerCharacter;

        private void Awake()
        {
            container.SetActive(false);
        }

        public void SetPlayerOwner(Entity owner)
        {
            playerCharacter = owner;

            //detector = playerCharacter.ActionDetector;
            //detector.OnActionsChanged += DetectorOnOnActionsChanged;
            //updateActions(detector);
        }

        // private void DetectorOnOnActionsChanged(ActionObjectDetector obj)
        // {
        //     updateActions(obj);
        // }

        // void updateActions(ActionObjectDetector detector)
        // {
        //     var actions = detector.GetActions();
        //     if (actions == null || actions.Length == 0)
        //     {
        //         icon.sprite = null;
        //         container.SetActive(false);
        //         currentAction = null;
        //     }
        //     else
        //     {
        //         currentAction = actions[0];
        //         icon.sprite = currentAction.Icon;
        //         container.SetActive(true);
        //     }
        // }

        public void OnUsePressed()
        {
            Debug.LogError("Not implemented");
            //currentAction.Behaviour.DoAction(currentAction, playerCharacter);
            //updateActions(detector);
        }
    }
}
