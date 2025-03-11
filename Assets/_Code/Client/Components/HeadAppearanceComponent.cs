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
        public Entity ClothHeadCollider;
        public Entity ClothNeckCollider;
        
        public Entity HairInstance;
    }
    
    [UseDefaultInspector]
    public class HeadAppearanceComponent : ComponentDataBehaviour<HeadAppearance>
    {
        public Transform HairSocket;
        public GameObject ClothHeadCollider;
        public GameObject ClothNeckCollider;
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
            serializedData.ClothHeadCollider = baker.GetEntity(ClothHeadCollider);
            serializedData.ClothNeckCollider = baker.GetEntity(ClothNeckCollider);
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
