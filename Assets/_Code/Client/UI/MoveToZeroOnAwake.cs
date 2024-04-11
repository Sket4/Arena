using UnityEngine;

namespace FallGame.Client.UI
{
    public class MoveToZeroOnAwake : MonoBehaviour
    {
        [SerializeField]
        RectTransform rectTransform;

        void Reset()
        {
            rectTransform = transform as RectTransform;
        }

        void Awake()
        {
            rectTransform.anchoredPosition3D = Vector3.zero;
        }
    }
}
