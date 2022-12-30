using CAS;
using GameKit.Ads.Units;

namespace GameKit.CleverAds
{
    internal class CasSmartBannerAdUnit : CasAdUnit, ITopSmartBannerAdUnit, IBottomSmartBannerAdUnit
    {
        private readonly IAdView _view;
        private readonly AdPosition _position;

        public CasSmartBannerAdUnit(IMediationManager manager, AdPosition position) : base(manager, AdType.Banner)
        {
            _view = manager.GetAdView(AdSize.SmartBanner);
            _position = position;
            _view.OnClicked += (v)=>OnAdClicked();
            _view.OnFailed += (v, e) => OnAdFailedToLoad(e);
            _view.OnLoaded += (v) => OnAdLoaded();
            _view.OnImpression += (v, d) => OnAdDisplayed();
        }

        public override void Show()
        {
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is show");
            _view.position = _position;
            _view.SetActive(true);
        }

        public void Hide()
        {
            manager.GetAdView(AdSize.SmartBanner).SetActive(false);
            OnAdClosed();
        }

        internal override void Load()
        {
            if (_view.isReady)
            {
                State = AdUnitState.Loaded;
                return;
            }

            State = AdUnitState.Loading;
            _view.Load();
        }
    }
}