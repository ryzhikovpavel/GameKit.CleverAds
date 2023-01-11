﻿//
//  Clever Ads Solutions Unity Plugin
//
//  Copyright © 2022 CleverAdsSolutions. All rights reserved.
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CAS.Unity
{
    internal class CASSettingsClient : IAdsSettings, ITargetingOptions
    {
        public bool analyticsCollectionEnabled { get; set; }
        public int bannerRefreshInterval { get; set; }
        public int interstitialInterval { get; set; }
        public ConsentStatus userConsent { get; set; }
        public CCPAStatus userCCPAStatus { get; set; }
        public Audience taggedAudience { get; set; }
        public bool isDebugMode { get; set; }
        public bool isMutedAdSounds { get; set; }
        public LoadingManagerMode loadingMode { get; set; }
        public bool iOSAppPauseOnBackground { get; set; }
        public bool allowInterstitialAdsWhenVideoCostAreLower { get; set; }
        public bool trackLocationEnabled { get; set; }

        public Gender gender { get; set; }
        public int age { get; set; }

        public float lastInterImpressionTimestamp = float.MinValue;

        private List<string> _testDeviceIds = new List<string>();

        public List<string> GetTestDeviceIds()
        {
            return _testDeviceIds;
        }

        public void RestartInterstitialInterval()
        {
            lastInterImpressionTimestamp = Time.time;
        }

        public void SetTestDeviceIds( List<string> testDeviceIds )
        {
            _testDeviceIds = testDeviceIds;
        }

        public bool isExecuteEventsOnUnityThread
        {
            get { return CASFactory.IsExecuteEventsOnUnityThread(); }
            set { CASFactory.SetExecuteEventsOnUnityThread( value ); }
        }

    }
}
