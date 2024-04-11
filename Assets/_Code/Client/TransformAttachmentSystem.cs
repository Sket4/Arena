using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace TzarGames.GameCore
{
    [System.Serializable]
    public struct TransformAttachment : IComponentData
    {
        public Entity Parent;
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class TransformAttachmentSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                //.WithStructuralChanges()
                .ForEach((ref LocalTransform transform, in TransformAttachment attachment) => 
            {
                //var parentTranslation = GetComponent<Translation>(attachment.Parent);
                var parentL2W = SystemAPI.GetComponent<LocalToWorld>(attachment.Parent);
                
                transform.Position = parentL2W.Position;
                transform.Rotation = parentL2W.Rotation;

            }).Schedule();
        }
    }
}
