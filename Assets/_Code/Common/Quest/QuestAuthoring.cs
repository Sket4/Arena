using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.Quests
{
    [Serializable]
    public struct QuestData : IComponentData
    {
        public PrefabID GameSceneID;
    }
    
    [UseDefaultInspector]
    public class QuestAuthoring : ObjectKeyGenericComponent<QuestKey>
    {
        public GameSceneKey GameScene;
        
        protected override void Bake<T>(T baker)
        {
            base.Bake(baker);

            if (ID != null)
            {
                baker.AddComponent(new PrefabID(ID.Id));    
            }
            
            baker.AddComponent(new QuestData
            {
                GameSceneID = GameScene != null ? new PrefabID(GameScene.Id) : default
            });
        } 
    }
}
