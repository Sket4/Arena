using UnityEngine;
using TzarGames.Common.UI;
using TzarGames.GameFramework;
using TzarGames.GameFramework.UI;
using Unity.Entities;

namespace Arena.Client.UI
{
    public class XpPointsButtonUI : GameUIBase
    {
        [SerializeField]
        TextUI pointsCount = default;
        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
        }

        void Update()
        {
            if(OwnerEntity == Entity.Null)
            {
                return;
            }

            throw new System.NotImplementedException();
            //var points = playerCharacter.PlayerTemplateInstance.AvailableUpgradePoints;

            //if (points <= 0)
            //{
            //    if(pointsCount.enabled)
            //    {
            //        pointsCount.enabled = false;    
            //    }
            //}
            //else
            //{
            //    if (pointsCount.enabled == false)
            //    {
            //        pointsCount.enabled = true;
            //    }
            //    pointsCount.text = string.Format("+{0}", points);    
            //}
        }
    }
}
