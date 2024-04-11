using UnityEngine;

namespace Arena.WorldObserver
{
    public class Label : MonoBehaviour
    {
        public GameLocationType Area;
        public TzarGames.Common.LocalizedStringAsset LocalizedName;
        public Transform WorldTransform;

        [System.NonSerialized]
        public LevelSelectionLabelUI LabelUI;

        private void Reset()
        {
            WorldTransform = transform;
        }
    }
}
