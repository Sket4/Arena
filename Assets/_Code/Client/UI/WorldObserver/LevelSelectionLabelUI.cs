using UnityEngine;

namespace Arena.WorldObserver
{
    public class LevelSelectionLabelUI : MonoBehaviour
    {
        [SerializeField]
        Sprite activeSprite = default;

        [SerializeField]
        Sprite defaultSprite = default;

        //[SerializeField]
        //CanvasGroup canvasGroup = default;

        [SerializeField]
        UnityEngine.UI.Image image = default;

        [SerializeField]
        TzarGames.Common.UI.TextUI text = default;

        public event System.Action<Label> OnPressed;

        public Transform LabelWorldTransform { get; set; }
        public Camera TargetCamera { get; set; }
        public Label LabelInfo { get; set; }

        public bool Active
        {
            get
            {
                return image.sprite == activeSprite;
            }
            set
            {
                image.sprite = value ? activeSprite : defaultSprite;
            }
        }

        public string Text
        {
            get
            {
                return text.text;
            }
            set
            {
                text.text = value;
            }
        }

        public Transform CachedTransform { get; private set; }
        
        private void Start()
        {
            CachedTransform = transform;
        }

        public void NotifyClicked()
        {
            if(OnPressed != null)
            {
                OnPressed(LabelInfo);
            }
        }
    }
}
