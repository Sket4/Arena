using DGX.SRP;
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
        private CameraRenderSettingsData cameraRenderSettings;
        private CameraRenderIntervalMode lastIntervalMode;

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            map = FindObjectOfType<PlayerCharacterUI>().GetOrCreateMapCamera(ownerEntity, manager);
            mapImage.texture = map.CameraTexture;
            cameraRenderSettings = map.Camera.GetComponent<CameraRenderSettings>().Settings;
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            lastIntervalMode = cameraRenderSettings.IntervalMode;
            cameraRenderSettings.IntervalMode = CameraRenderIntervalMode.None;
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            cameraRenderSettings.IntervalMode = lastIntervalMode;
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
