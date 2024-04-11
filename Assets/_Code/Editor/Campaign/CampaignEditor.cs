using System;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arena.CampaignTools.Editor
{
    public class CampaignEditor : EditorWindow
    {
        private CampaignView graphView;
        [SerializeField]
        private Campaign selectedCampaignAsset = default;
        private VisualElement graphEditContainer;

        [MenuItem("Arena/Окрыть редактор графов кампаний")]
        public static void OpenGraphWindow()
        {
            var window = GetWindow<CampaignEditor>();
            window.Show();
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();

            graphView.BuildFromGraph(selectedCampaignAsset);

            updateToolbar();
        }

        void ConstructGraphView()
        {
            graphView = new CampaignView();
            graphView.name = "Редактор графа кампании";
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }

        void updateToolbar()
        {
            if(selectedCampaignAsset != null)
            {
                graphEditContainer.style.visibility = Visibility.Visible;
            }
            else
            {
                graphEditContainer.style.visibility = Visibility.Hidden;
            }
        }

        void GenerateToolbar()
        {
            var toolbar = new Toolbar();
            
            graphEditContainer = new VisualElement();
            graphEditContainer.contentContainer.style.flexDirection = FlexDirection.Row;
            //graphEditContainer.contentContainer.style.flexWrap = Wrap.;

            // separator
            var spacer = new ToolbarSpacer();
            spacer.style.minWidth = 50;
            
            var createStartNodeButton = new Button(() =>
            {
                graphView.CreateSceneNode(SceneNodeTypes.Start, true);
            });
            createStartNodeButton.text = "Добавить начальный узел";
            createStartNodeButton.style.backgroundColor = CampaignView.GetColorForType(SceneNodeTypes.Start); 
            
            // normal node button
            
            var createNodeButton = new Button(() =>
            {
                graphView.CreateSceneNode(SceneNodeTypes.Normal, true);
            });
            createNodeButton.style.backgroundColor = CampaignView.GetColorForType(SceneNodeTypes.Normal); 
            createNodeButton.text = "Добавить узел";
            
            // end node button
            
            var createEndNodeButton = new Button(() =>
            {
                graphView.CreateSceneNode(SceneNodeTypes.End, true);
            });
            createEndNodeButton.style.backgroundColor = CampaignView.GetColorForType(SceneNodeTypes.End);
            createEndNodeButton.text = "Добавить конечный узел";

            // separator
            var spacer2 = new ToolbarSpacer();
            spacer2.style.minWidth = 50;
            
            // save button
            var saveButton = new Button(() =>
            {
                graphView.SaveToAsset(selectedCampaignAsset);
                EditorUtility.SetDirty(selectedCampaignAsset);
                AssetDatabase.SaveAssets();
            });
            saveButton.style.backgroundColor = new Color(0.1f,0.5f,0.1f,1);
            saveButton.text = "Сохранить";
            
            // scene graph asset ref
            var assetField = new ObjectField();
            assetField.RegisterValueChangedCallback((evt) =>
            {
                selectedCampaignAsset = evt.newValue as Campaign;
                graphView.BuildFromGraph(selectedCampaignAsset);
                updateToolbar();
            });
            assetField.label = "Файл графа:";
            assetField.objectType = typeof(Campaign);
            assetField.value = selectedCampaignAsset;
            
            // add in order
            toolbar.Add(assetField);
            toolbar.Add(spacer);
            
            graphEditContainer.Add(createStartNodeButton);
            graphEditContainer.Add(createNodeButton);
            graphEditContainer.Add(createEndNodeButton);
            graphEditContainer.Add(spacer2);
            graphEditContainer.Add(saveButton);
            toolbar.Add(graphEditContainer);

            rootVisualElement.Add(toolbar);
        }

        private void OnDisable()
        {
            if(graphView != null)
            {
                rootVisualElement.Remove(graphView);
            }
        }
    }
}
