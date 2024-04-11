using TzarGames.GameFramework.UI;
using UnityEngine;

namespace Arena.WorldObserver
{
    public class WorldObserverUI : GameUIBase
    {
        [SerializeField]
        WorldObserver worldPrefab = default;

        [SerializeField]
        LevelSelectionUI selectionUI = default;

        [SerializeField]
        Scroller scroller = default;

        WorldObserver world;
        Client.ThirdPersonCamera lastMainCamera;

        [SerializeField]
        UnityEngine.Events.UnityEvent onLoadingStarted = default;

        bool singlePlayer = true;

        public void SetSinglePlayerMode(bool mode)
        {
            singlePlayer = mode;
        }

        public void LoadArea(GameLocationType area)
        {
            throw new System.NotImplementedException();
            
            //Arena.GameState.Instance.GotoArea(new Arena.AreaRequest { Area = area, Multiplayer = !singlePlayer });
            onLoadingStarted.Invoke();
        }

        protected override void OnVisible()
        {
            base.OnVisible();

            lastMainCamera = FindObjectOfType<Client.ThirdPersonCamera>();
            if(lastMainCamera != null)
            {
                lastMainCamera.gameObject.SetActive(false);
            }

            if(world == null)
            {
                world = Instantiate(worldPrefab);
            }

            if(world.gameObject.activeSelf == false)
            {
                world.gameObject.SetActive(true);
            }

            selectionUI.LevelSelectionCamera = world.Camera;
            scroller.Camera = world.Camera;
            scroller.BoundsMesh = world.BoundsMesh;
        }

        protected override void OnHidden()
        {
            base.OnHidden();

            if(world != null)
            {
                world.gameObject.SetActive(false);
            }

            if (lastMainCamera != null)
            {
                lastMainCamera.gameObject.SetActive(true);
                lastMainCamera = null;
            }
        }
    }
}
