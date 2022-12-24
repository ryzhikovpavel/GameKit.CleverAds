using System;
using CAS;
using GameKit.Ads.Units;

namespace GameKit.CleverAds
{
    internal class CasInterstitialAdUnit : CasAdUnit, IInterstitialAdUnit
    {
        public CasInterstitialAdUnit(IMediationManager manager) : base(manager, AdType.Interstitial)
        {
            manager.OnInterstitialAdClicked += OnAdClicked;
            manager.OnInterstitialAdShown += OnAdDisplayed;
            manager.OnInterstitialAdFailedToShow += OnAdFailedToShow;
            manager.OnInterstitialAdClosed += OnAdClosed;
            manager.OnInterstitialAdFailedToLoad += OnAdFailedToLoad;
            manager.OnInterstitialAdLoaded += OnAdLoaded;
        }
    }
}