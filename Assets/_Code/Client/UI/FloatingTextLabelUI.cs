using TzarGames.Common.UI;
using UnityEngine;

namespace Arena.Client.UI
{
    public class FloatingTextLabelUI : FloatingLabelBaseUI
    {
        [SerializeField] private TextUI _textUi = default;
        
        public string Text
        {
            get { return _textUi.text; }
            set { _textUi.text = value; }
        }

        public Color Color
        {
            get { return _textUi.Color; }
            set { _textUi.Color = value; }
        }

        public override void OnPulledFromPool()
        {
            base.OnPulledFromPool();
            if (_textUi != null)
            {
                _textUi.enabled = true;
            }
        }

        public override void OnPushedToPool()
        {
            base.OnPushedToPool();
            if(_textUi != null)
            {
                _textUi.enabled = false;
            }
        }
    }
}
