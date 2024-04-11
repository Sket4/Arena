using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [System.Serializable]
    public struct SpawnPointObjectPrefabReference : IBufferElementData
    {
        public Entity Prefab;
    }

    [UseDefaultInspector]
    public class SpawnPointPrefabsComponent : DynamicBufferBehaviour<SpawnPointObjectPrefabReference>, IComponentHelpProvider
    {
        public ObjectKey[] Prefabs;

        static string helpText = null;
        public string GetHelpText()
        {
            if(helpText == null)
            {
                helpText = new TzarGames.GameCore.Tools.HelpFormatHelper
                {
                    MainDescription = "������ ��������, ������� ����� ���� ���������� � ������ �����",
                    Parameters = new TzarGames.GameCore.Tools.ParameterInfo[]
                    {
                        new TzarGames.GameCore.Tools.ParameterInfo
                        {
                            Description = "������ ��������, ������� ����� ���� ���������� � ������ �����",
                            Name = nameof(Prefabs),
                        }
                    }
                }.ToString();
            }
            return helpText;
        }

        protected override void Bake<K>(ref DynamicBuffer<SpawnPointObjectPrefabReference> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if(Prefabs == null)
            {
                return;
            }

            foreach(var prefab in Prefabs)
            {
                serializedData.Add(new SpawnPointObjectPrefabReference
                {
                    Prefab = baker.ConvertObjectKey(prefab)
                });
            }
        }
    }
}
