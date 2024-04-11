using Arena.Client;
using TzarGames.Common.Events;
using UnityEngine;
using Arena;

namespace Arena.WorldObserver
{
    [System.Serializable]
    public class AreaEvent : UnityEngine.Events.UnityEvent<GameLocationType>
    {
    }

    public class LevelSelectionUI : MonoBehaviour
    {
        [SerializeField]
        LevelSelectionLabelUI levelLabelPrefab = default;

        [SerializeField]
        RectTransform levelLabelContainer = default;

        Label[] labels;

        public Camera LevelSelectionCamera;

        [SerializeField]
        StringEvent onLabelSelected = default;

        [SerializeField]
        StringEvent onCurrentLabelSelected = default;

        [SerializeField]
        AreaEvent onLoadLevel = default;

        private Label selectedLevel;

        private void Start()
        {
            var gameState = GameState.Instance;
            labels = FindObjectsOfType<Label>();

            foreach (var l in labels)
            {
                var newLabel = Instantiate(levelLabelPrefab);
                newLabel.transform.SetParent(levelLabelContainer);
                newLabel.TargetCamera = LevelSelectionCamera;
                newLabel.LabelWorldTransform = l.WorldTransform;
                newLabel.Text = l.LocalizedName;
                newLabel.OnPressed += OnLabelClicked;
                newLabel.LabelInfo = l;
                l.LabelUI = newLabel;

                if(gameState != null)
                {
                    throw new System.NotImplementedException();
                    
                    // if(gameState.CurrentAreaRequest.Area == l.Area)
                    // {
                    //     newLabel.Active = true;
                    // }
                    // else
                    // {
                    //     newLabel.Active = false;
                    // }
                }
                else
                {
                    if(l.Area == GameLocationType.Arena)
                    {
                        newLabel.Active = true;
                    }
                    else
                    {
                        newLabel.Active = false;
                    }
                }
            }
        }

        void OnLabelClicked(Label labelInfo)
        {
            selectedLevel = labelInfo;

            foreach(var l in labels)
            {
                l.LabelUI.Active = labelInfo == l;
            }

            GameLocationType currentArea = GameLocationType.Arena;

            if(GameState.Instance != null)
            {
                throw new System.NotImplementedException();
                //currentArea = GameState.Instance.CurrentAreaRequest.Area;
            }

            if(labelInfo.Area == currentArea)
            {
                onCurrentLabelSelected.Invoke(labelInfo.LocalizedName);
            }
            else
            {
                onLabelSelected.Invoke(labelInfo.LocalizedName);
            }
        }

        public void LoadCurrentLevel()
        {
            var gameState = GameState.Instance;

            Debug.Log("Trying to load level " + selectedLevel.LocalizedName);

            onLoadLevel.Invoke(selectedLevel.Area);
        }

        private void Update()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var l = label.LabelUI;

                var screenPoint = l.TargetCamera.WorldToScreenPoint(l.LabelWorldTransform.position);
                l.CachedTransform.position = screenPoint;

                //var worldDir = l.LabelWorldTransform.forward;
                //var dirToCamera = -l.TargetCamera.transform.forward;

                //var dot = Vector3.Dot(worldDir, dirToCamera);

                //dot = Mathf.Clamp01(dot);
                //canvasGroup.alpha = dot;
            }
        }
    }
}
