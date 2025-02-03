using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct HeadAppearance : IComponentData
    {
        public Entity HairSocketEntity;
        public Entity HeadModel;
        public Entity BrowsModel;
        public Entity EyesModel;
    }
    
    [UseDefaultInspector]
    public class HeadAppearanceComponent : ComponentDataBehaviour<HeadAppearance>
    {
        public Transform HairSocket;
        public Renderer HeadModel;
        public Renderer BrowsModel;
        public Renderer EyesModel;

        protected override void Bake<K>(ref HeadAppearance serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.HairSocketEntity = baker.GetEntity(HairSocket);
            serializedData.BrowsModel = baker.GetEntity(BrowsModel);
            serializedData.HeadModel = baker.GetEntity(HeadModel);
            serializedData.EyesModel = baker.GetEntity(EyesModel);
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
