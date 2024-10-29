using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TzarGames.GameFramework.UI;
using TzarGames.GameFramework;
using Unity.Entities;

namespace Arena.Client.UI
{
    public class MapScrollUI : GameUIBase, IDragHandler
    {
        [SerializeField]
        RectTransform mapRect = default;

        [SerializeField]
        RawImage mapImage = default;

        Map map;

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            map = FindObjectOfType<PlayerCharacterUI>().GetOrCreateMapCamera(ownerEntity, manager);
            mapImage.texture = map.CameraTexture;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var rectWidth = mapRect.rect.width * mapImage.canvas.scaleFactor;
            var delta = -eventData.delta;
            var cameraSize = map.Camera.orthographicSize * 2;

            map.MoveHorizontally(delta.x / rectWidth * cameraSize, delta.y / rectWidth * cameraSize);
        }
    }
}
