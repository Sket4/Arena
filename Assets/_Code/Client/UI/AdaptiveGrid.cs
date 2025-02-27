using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    [ExecuteInEditMode]
    public class AdaptiveGrid : MonoBehaviour
    {
        public float TargetCellSize = 200;
        public GridLayoutGroup GridLayout;
        private RectTransform gridTransform;
        public Image TiledImage;
        public float TiledImageTargetPixelMult = 1;

        private void Reset()
        {
            GridLayout = GetComponent<GridLayoutGroup>();
        }

        private void Update()
        {
            if (GridLayout == false)
            {
                return;
            }
            if (gridTransform == false)
            {
                gridTransform = GridLayout.transform as RectTransform;
            }
            var rect = gridTransform.rect;
            var targetCellCount = (int)(rect.width / TargetCellSize);
            if (targetCellCount == 0)
            {
                GridLayout.cellSize = new Vector2(TargetCellSize, TargetCellSize);
                return;
            }
            var targetWidth = targetCellCount * TargetCellSize;
            var scale = rect.width / targetWidth;
            var gridCellSize = TargetCellSize * scale;
            if (math.abs(GridLayout.cellSize.x - gridCellSize) > math.EPSILON)
            {
                GridLayout.cellSize = new Vector2(TargetCellSize, TargetCellSize) * scale;    
            }

            if (TiledImage)
            {
                var timeSize = TiledImageTargetPixelMult / scale;
                
                if (math.abs(timeSize - TiledImage.pixelsPerUnitMultiplier) > math.EPSILON)
                {
                    TiledImage.pixelsPerUnitMultiplier = timeSize;
                }
            }
        }
    }
}
