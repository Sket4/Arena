using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class SwitcherUI : MonoBehaviour
    {
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMPro.TextMeshProUGUI label;
        [SerializeField] private TMPro.TextMeshProUGUI text;
        [SerializeField] private Image image;
        
        public UnityEvent OnPrev;
        public UnityEvent OnNext;
        
        public string Text
        {
            get => text.text;
            set => text.text = value;
        }

        public Color Color
        {
            get
            {
                return text.color;
            }
            set
            {
                if(text.gameObject.activeInHierarchy)
                    text.color = value;
                if(image.gameObject.activeInHierarchy)
                    image.color = value;
            }
        }

        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        private void Start()
        {
            prevButton.onClick.AddListener(() =>
            {
                OnPrev.Invoke();
            });
            nextButton.onClick.AddListener(() =>
            {
                OnNext.Invoke();
            });
        }
    }
}
