using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using TzarGames.GameCore;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(KeyboardInputSystem))]
    public partial class TouchInputSystem : SystemBase
    {   
        TouchControlsBehaviour touchControls;
        
        protected override void OnUpdate()
        {
            if (touchControls == null)
            {
                touchControls = Object.FindObjectOfType<TouchControlsBehaviour>();
                if (touchControls == null)
                {
                    return;
                }
            }

            var horizontal = touchControls.Joystick.Horizontal;
            var vertical = touchControls.Joystick.Vertical;
            var viewScroll = touchControls.ViewScroll.Movement;

            Entities.ForEach((ref PlayerInput input) =>
            {
                input.Horizontal = horizontal;
                input.Vertical = vertical;
                input.ViewScroll = viewScroll;

            }).Run();
        }
    }
}
