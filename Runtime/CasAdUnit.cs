using System.Threading.Tasks;
using CAS;
using GameKit.Ads;
using GameKit.Ads.Units;

namespace GameKit.CleverAds
{
    internal abstract class CasAdUnit : IAdUnit
    {
        public const int PauseDelay = 5;
        
        protected static ILogger Logger => Logger<CasNetwork>.Instance;
        protected readonly IMediationManager manager;
        protected AdType Type { get; }
        
        public string Name { get; }
        public AdUnitState State { get; set; }
        public string Error { get; protected set;}
        public IAdInfo Info { get; }

        protected CasAdUnit(IMediationManager manager, AdType type)
        {
            Name = GetType().Name;
            Info = new DefaultAdInfo();
            
            this.manager = manager;
            Type = type;
        }

        public virtual void Show()
        {
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is show");
            manager.ShowAd(Type);
        }

        public virtual void Release()
        {
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is release");
            State = AdUnitState.Empty;
            
            Load();
        }

        internal virtual void Load()
        {
            if (manager.IsReadyAd(Type))
            {
                State = AdUnitState.Loaded;
                return;
            }

            State = AdUnitState.Loading;
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is loading");
            manager.LoadAd(Type);
        }

        private async void WaitAndLoad()
        {
            await Task.Delay(PauseDelay * 1000, Loop.Token);
            if (Loop.Token.IsCancellationRequested) return;
            Load();
        }
        
        protected virtual void OnAdLoaded()
        {
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is loaded");
            if (State is (AdUnitState.Loading or AdUnitState.Error))
            {
                State = AdUnitState.Loaded;
                if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is loaded");
            }
            
            if (State == AdUnitState.Loaded) return;
            if (Logger.IsWarningAllowed) Logger.Warning($"Unit {Type} type loaded, but state is {State}");
        }

        protected virtual void OnAdFailedToLoad(AdError adError)
        {
            if (State != AdUnitState.Loading)
            {
                Logger.Warning($"Unit {Type} type failed to load, but state is {State}");
            }
            
            Error = adError.ToString();
            State = AdUnitState.Error;
            if (Logger.IsErrorAllowed) Logger.Error($"{Name} load failed with error: {Error}");
            WaitAndLoad();
        }

        protected virtual void OnAdClosed()
        { 
            State = AdUnitState.Closed;
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is closed");
        }

        protected virtual void OnAdClicked()
        {
            State = AdUnitState.Clicked;
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is clicked");
        }

        protected virtual void OnAdDisplayed()
        {
            State = AdUnitState.Displayed;
            if (Logger.IsDebugAllowed) Logger.Debug($"{Name} is displayed");
        }

        protected virtual void OnAdFailedToShow(string error)
        {
            Error = error;
            State = AdUnitState.Error;
            if (Logger.IsErrorAllowed) Logger.Error($"{Name} is show failed with error {Error}");
        }
    }
}