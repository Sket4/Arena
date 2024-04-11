using UnityEngine;

namespace Arena.Client.UI
{
    public class FloatingLabelBaseUI : MonoBehaviour, TzarGames.GameFramework.IPoolable
    {
        public Transform Transform;

        [SerializeField] private Animation _animation = default;
        [SerializeField] private UnityEngine.UI.Graphic graphic;

        public UnityEngine.UI.Graphic Graphic
        {
            get
            {
                return graphic;
            }
        }

        private void Reset()
        {
            Transform = transform;
        }

        public virtual void Show()
        {
            if (_animation != null)
            {
                _animation.Play();
            }
        }

        public virtual void OnPushedToPool()
        {
            if(_animation != null)
            {
                _animation.enabled = false;
            }
        }

        public virtual void OnPulledFromPool()
        {
            if (_animation != null)
            {
                _animation.enabled = true;
            }
        }
    }
}
