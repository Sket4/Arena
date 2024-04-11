using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Entities;

namespace Arena
{
    [System.Serializable]
    [Sync]
	public struct CharacterClassData : IComponentData
	{
        public CharacterClass Value;
	}

    public class CharacterClassComponent : ComponentDataBehaviour<CharacterClassData>
    {
    }
}
