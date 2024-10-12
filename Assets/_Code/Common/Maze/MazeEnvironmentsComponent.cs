using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.Maze
{
    [System.Serializable]
    public struct MazeEnvironmentPrefabElement : IBufferElementData
    {
        public Entity Builder;
    }

    public struct MazeEnvironmentPrefabsTag : IComponentData {}

    [UseDefaultInspector]
    public class MazeEnvironmentsComponent : DynamicBufferBehaviour<MazeEnvironmentPrefabElement>
    {
        public MazeWorldBuilderComponent[] Environments;

        protected override void Bake<K>(ref DynamicBuffer<MazeEnvironmentPrefabElement> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if(Environments == null)
            {
                return;
            }

            foreach(var enc in Environments)
            {
                if(enc == null)
                {
                    continue;
                }
                serializedData.Add(new MazeEnvironmentPrefabElement
                {
                    Builder = baker.GetEntity(enc),
                });
            }

            baker.AddComponent(new MazeEnvironmentPrefabsTag());
        }
    }
}
