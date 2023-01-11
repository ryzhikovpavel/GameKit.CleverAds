using System;
using System.Collections.Generic;
using CAS;
using GameKit.Ads;
using GameKit.Ads.Networks;
using GameKit.Ads.Units;
using UnityEngine;

namespace GameKit.CleverAds
{
    public class CasNetwork: IAdsNetwork
    {
        public static ILogger Log => Logger<CasNetwork>.Instance;
        private readonly Dictionary<Type, IAdUnit[]> _units = new Dictionary<Type, IAdUnit[]>();
        private IMediationManager _manager;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Registration()
        {
            if (Log.IsDebugAllowed) Log.Debug("Registration");
            Service<AdsMediator>.Instance.RegisterNetwork(new CasNetwork());
        }
        
        public TaskRoutine Initialize(bool trackingConsent, bool intrusiveAdUnits)
        {
            if (Log.IsDebugAllowed) Log.Debug("Initialize");
            // -- Privacy Laws (Optional):
            MobileAds.settings.userConsent = trackingConsent ? ConsentStatus.Accepted : ConsentStatus.Denied;
            MobileAds.settings.userCCPAStatus = trackingConsent ? CCPAStatus.OptInSale : CCPAStatus.OptOutSale;

            // -- Configuring CAS SDK (Optional):
            //MobileAds.settings.isExecuteEventsOnUnityThread = true;

            // -- Create manager:
            _manager = MobileAds.BuildManager().Initialize();

            // -- Get native CAS SDK version
            if (Log.IsInfoAllowed) Log.Info($"version {MobileAds.GetSDKVersion()}");

            if (_manager.IsEnabledAd(AdType.Rewarded))
                _units[typeof(IRewardedVideoAdUnit)] = new IAdUnit[] { new CasRewardedAdUnit(_manager) };

            if (_manager.IsEnabledAd(AdType.Interstitial) && intrusiveAdUnits)
                _units[typeof(IInterstitialAdUnit)] = new IAdUnit[] { new CasInterstitialAdUnit(_manager) };

            if (_manager.IsEnabledAd(AdType.Banner) && intrusiveAdUnits)
            {
                _units[typeof(ITopSmartBannerAdUnit)] = new IAdUnit[] { new CasSmartBannerAdUnit(_manager, AdPosition.TopCenter) };
                _units[typeof(IBottomSmartBannerAdUnit)] = new IAdUnit[] { new CasSmartBannerAdUnit(_manager, AdPosition.BottomCenter) };
            }

            foreach (var units in _units.Values)
            {
                foreach (IAdUnit adUnit in units)
                {
                    ((CasAdUnit)adUnit).Load();
                }
            }
            
            if (Debug.isDebugBuild)
                MobileAds.ValidateIntegration();
            
            return TaskRoutine.FromCompleted();
        }

        public bool IsSupported(Type type)
        {
            return _units.ContainsKey(type);
        }

        public IAdUnit[] GetUnits(Type type)
        {
            return _units[type];
        }
    }
}