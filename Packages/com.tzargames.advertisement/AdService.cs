using System;
using TzarGames.Common;
using UnityEngine;

namespace TzarGames.Ads
{
    public abstract class AdService : CommonAsset, IAdsService
    {
        public event Action<IAdsService> OnAdStarted;
        public event Action<IAdsService> OnAdSkippedOrFinished;
        
        public abstract bool IsReady(Ad ad);
        public abstract void Show(Ad ad, Action<ShowResult> callback);
        public abstract void Initialize();
        public abstract void RequestAd(Ad ad);

        protected void NotifyAdStarted()
        {
            try
            {
                if (OnAdStarted != null) OnAdStarted(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected void NotifyAdSkippedOrFinished()
        {
            try
            {
                if (OnAdSkippedOrFinished != null) OnAdSkippedOrFinished(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
