using CAS;
using GameKit.Ads;
using GameKit.Ads.Units;

namespace GameKit.CleverAds
{
    internal class CasRewardedAdUnit: CasAdUnit, IRewardedVideoAdUnit
    {
        public CasRewardedAdUnit(IMediationManager manager) : base(manager, AdType.Rewarded)
        {
            manager.OnRewardedAdCompleted += OnRewardedSuccessful;
            manager.OnRewardedAdShown += OnAdDisplayed;
            manager.OnRewardedAdFailedToShow += OnAdFailedToShow;
            manager.OnRewardedAdClosed += OnAdClosed;
            manager.OnRewardedAdFailedToLoad += OnAdFailedToLoad;
            manager.OnRewardedAdLoaded += OnAdLoaded;
        }

        public bool IsEarned { get; set; }
        public IRewardAdInfo Reward { get; set; }

        public override void Show()
        {
            IsEarned = false;
            base.Show();
        }

        private void OnRewardedSuccessful()
        {
            IsEarned = true;
        }
    }
}