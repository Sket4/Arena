using TzarGames.GameCore;

namespace Arena.Client.PreviewRendering
{
    public class PreviewRenderingObjectKeyValueStore : ObjectKeyValueStoreAuthoring
    {
        protected override bool ShouldBakeObjectEntry(ObjectKeyValueStore.ObjectEntry key)
        {
            if (key.Key is ItemKey == false)
            {
                return false;
            }
            return base.ShouldBakeObjectEntry(key);
        }
    }   
}
