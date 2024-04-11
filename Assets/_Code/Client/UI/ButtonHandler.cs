using UnityEngine;
using System.Collections;

namespace Arena.Client.UI
{
    public enum UIButtonState
    {
        Up,
        Down
    }

    public class ButtonHandler : MonoBehaviour
    {
        public UIButtonState State { get; private set; }
        ushort frameNumber;
        ushort downStateFrameNumber;
        Coroutine upStateCoroutine;

        private void OnDisable()
        {
            if(upStateCoroutine != null)
            {
                StopCoroutine(upStateCoroutine);
                upStateCoroutine = null;
            }
            State = UIButtonState.Up;
        }

        public void SetDownState()
        {
            State = UIButtonState.Down;
            downStateFrameNumber = frameNumber;
        }

        public void SetUpState()
        {
            if(downStateFrameNumber == frameNumber)
            {
                //Debug.Log("frame click");
                upStateCoroutine = StartCoroutine(lateUpStateFunc());
                return;
            }
            State = UIButtonState.Up;
        }

        IEnumerator lateUpStateFunc()
        {
            yield return new WaitForEndOfFrame();
            State = UIButtonState.Up;
            upStateCoroutine = null;
        }

        void Update()
        {
            if(frameNumber+1 == ushort.MaxValue)
            {
                frameNumber = 0;
            }
            frameNumber++;
        }
    }
}
