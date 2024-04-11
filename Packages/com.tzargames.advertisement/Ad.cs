using TzarGames.Common;
using UnityEngine;

namespace TzarGames.Ads
{
    public class Ad : CommonAsset
	{
		[SerializeField] private string adId = default;
		
		public string AdId
		{
			get { return adId; }
		}
	}	
}
