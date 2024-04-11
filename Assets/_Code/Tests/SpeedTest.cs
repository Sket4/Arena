using UnityEngine;

namespace TzarGames.GameCore.Tests
{
    public class SpeedTest : MonoBehaviour
    {
        Vector3 prevPos;
        float currentSpeed;

        private void Start()
        {
            prevPos = transform.position;
        }

        void LateUpdate()
        {
            currentSpeed = (transform.position - prevPos).magnitude / Time.deltaTime;
            prevPos = transform.position;
        }

        private void OnGUI()
        {
            GUILayout.Label($"Текущая скорость: {currentSpeed}");
        }
    }
}
