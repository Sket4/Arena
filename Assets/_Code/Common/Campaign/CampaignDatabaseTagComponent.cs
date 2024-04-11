using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.CampaignTools
{
    public struct CampaignDatabaseTag : IComponentData
    {
    }

    public class CampaignDatabaseTagComponent : ComponentDataBehaviour<CampaignDatabaseTag>
    {
    }
}