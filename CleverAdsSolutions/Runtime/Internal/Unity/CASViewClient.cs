﻿//
//  Clever Ads Solutions Unity Plugin
//
//  Copyright © 2022 CleverAdsSolutions. All rights reserved.
//

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace CAS.Unity
{

    internal class CASViewClient : IAdView
    {
        private static bool _emulateTabletScreen = false;

        private readonly CASManagerClient _manager;
        private bool _active = false;
        private bool _loaded = false;
        private bool _waitPresentEvent = true;
        private bool _waitImpressionEvent = false;
        private int _positionX = 0;
        private int _positionY = 0;
        private AdError _lastError = AdError.Internal;
        private AdPosition _position = AdPosition.BottomCenter;

        public event CASViewEvent OnLoaded;
        public event CASViewEventWithError OnFailed;
        public event CASViewEventWithMeta OnImpression;
        public event CASViewEvent OnClicked;
        public event CASViewEventWithMeta OnPresented;
        public event CASViewEvent OnHidden;

        public IMediationManager manager { get { return _manager; } }
        public AdSize size { get; private set; }
        public Rect rectInPixels { get; private set; }
        public int refreshInterval { get; set; }

        public bool isReady
        {
            get { return _loaded && _manager.IsEnabledAd( AdType.Banner ); }
        }

        internal CASViewClient( CASManagerClient manager, AdSize size )
        {
            _manager = manager;
            this.size = size;
            if (_manager.isAutolod)
                Load();
            refreshInterval = MobileAds.settings.bannerRefreshInterval;
        }

        public void Dispose()
        {
            _manager.viewFactory.RemoveAdViewFromFactory( this );
        }

        public void DisableRefresh()
        {
            refreshInterval = 0;
        }

        public void Load()
        {
            if (_manager.IsEnabledAd( AdType.Banner ))
            {
                if (!_loaded)
                    _manager.Post( CallAdLoaded, 0.5f );
                return;
            }
            _lastError = AdError.ManagerIsDisabled;
            _manager.Post( CallAdFailed );
        }

        public void SetActive( bool active )
        {
            if (active)
            {
                if (!_manager.IsEnabledAd( AdType.Banner ))
                {
                    _lastError = AdError.ManagerIsDisabled;
                    _manager.Post( CallAdFailed );
                    return;
                }
            }
            _active = active;

            if (!active)
            {
                rectInPixels = Rect.zero;
                if (!_waitPresentEvent)
                {
                    _waitPresentEvent = true;
                    _manager.Post( CallAdHidden );
                }
            }
        }

        public AdPosition position
        {
            get
            {
                return _position;
            }
            set
            {
                SetPosition( value, 0, 0 );
            }
        }

        public void SetPosition( int x, int y )
        {
            SetPosition( AdPosition.TopLeft, x, y );
        }

        private void SetPosition( AdPosition position, int x, int y )
        {
            if (position == AdPosition.Undefined)
                return;
            if (position != _position || x != _positionX || y != _positionY)
            {
                _manager.Log( "Banner position changed to " + position.ToString() + " with offset: x=" + x + ", y=" + y );
                _position = position;
                _positionX = x;
                _positionY = y;
            }
        }

        public void OnGUIAd( GUIStyle style )
        {
            if (_active && isReady)
            {
                if (_waitPresentEvent)
                {
                    _waitPresentEvent = false;
                    _manager.Post( CallAdPresented );
                }
                rectInPixels = CalculateAdRectOnScreen();
                var rect = new Rect( rectInPixels );
                var totalHeight = rect.height;
                rect.height = totalHeight * 0.65f;
                if (GUI.Button( rect, "CAS " + size.ToString() + " Ad", style ))
                    _manager.Post( CallAdClicked );

                rect.y += rect.height;
                rect.height = totalHeight * 0.35f;

                _emulateTabletScreen = GUI.Toggle( rect, _emulateTabletScreen,
                    _emulateTabletScreen ? "Switch to phone" : "Switch to tablet", style );
            }
        }

        private void CallAdLoaded()
        {
            _loaded = true;
            _waitImpressionEvent = true;
            if (OnLoaded != null)
                OnLoaded( this );
        }

        private void CallAdFailed()
        {
            if (OnFailed != null)
                OnFailed( this, _lastError );
            if (!_waitPresentEvent)
            {
                _waitPresentEvent = true;
                _manager.Post( CallAdHidden );
            }
        }

        private void CallAdPresented()
        {
            var impression = new CASImpressionClient( AdType.Banner );
            if (OnPresented != null)
                OnPresented( this, impression );
            if (_waitImpressionEvent && OnImpression != null)
            {
                _waitImpressionEvent = false;
                OnImpression( this, impression );
            }
        }

        private void CallAdClicked()
        {
            if (OnClicked != null)
                OnClicked( this );
        }

        private void CallAdHidden()
        {
            if (OnHidden != null)
                OnHidden( this );
        }

        private Rect CalculateAdRectOnScreen()
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            var safeArea = Screen.safeArea;
            const float phoneScale = 640;
            const float tabletScale = 1024;
            float scale = Mathf.Max( screenWidth, screenHeight ) / ( _emulateTabletScreen ? tabletScale : phoneScale );
            bool isPortrait = screenWidth < screenHeight;

            AdSize targetSize;
            if (size == AdSize.SmartBanner)
                targetSize = _emulateTabletScreen ? AdSize.Leaderboard : AdSize.Banner;
            else
                targetSize = size;

            Rect result = new Rect();
            switch (targetSize)
            {
                case AdSize.AdaptiveBanner:
                    result.width = Mathf.Min( screenWidth, 728.0f * scale );
                    result.height = ( _emulateTabletScreen ? 90.0f : 50.0f ) * scale;
                    break;
                case AdSize.Leaderboard:
                    result.width = 728.0f * scale;
                    result.height = 90.0f * scale;
                    break;
                case AdSize.MediumRectangle:
                    result.width = 300.0f * scale;
                    result.height = 250.0f * scale;
                    break;
                case AdSize.AdaptiveFullWidth:
                    result.width = screenWidth;
                    result.height = ( _emulateTabletScreen ? 90.0f : 50.0f ) * scale;
                    break;
                case AdSize.ThinBanner:
                    result.width = screenWidth;
                    if (_emulateTabletScreen)
                        result.height = ( isPortrait ? 90.0f : 50.0f ) * scale;
                    else
                        result.height = ( isPortrait ? 50.0f : 32.0f ) * scale;
                    break;
                default:
                    result.width = 320.0f * scale;
                    result.height = 50.0f * scale;
                    break;
            }

            var maxYPos = screenHeight - result.height - ( screenHeight - safeArea.yMax );
            var maxXPos = screenWidth - result.width - ( screenWidth - safeArea.xMax );
            switch (_position)
            {
                case AdPosition.TopCenter:
                case AdPosition.TopLeft:
                case AdPosition.TopRight:
                    result.y = _positionY * scale;
                    if (maxYPos < result.y)
                        result.y = maxYPos;
                    if (result.y < safeArea.y)
                        result.y = safeArea.y;
                    else if (isPortrait) // For Portrait orientation add offset to simulate Safe area
                        result.y = Math.Max( result.y, scale * 20.0f );
                    break;
                default:
                    result.y = maxYPos;
                    break;
            }
            switch (_position)
            {
                case AdPosition.BottomLeft:
                case AdPosition.TopLeft:
                    result.x = _positionX * scale;
                    if (result.x < safeArea.x)
                        result.x = safeArea.x;
                    if (maxXPos < result.x)
                        result.x = maxXPos;
                    break;
                case AdPosition.BottomCenter:
                case AdPosition.TopCenter:
                    result.x = safeArea.width * 0.5f + safeArea.x - result.width * 0.5f;
                    break;
                default:
                    result.x = maxXPos;
                    break;
            }
            return result;
        }
    }

    internal class CASViewFactoryImpl : CASViewFactory
    {
        private CASManagerClient manager;

        public event CASTypedEvent OnLoadedAd;
        public event CASTypedEventWithError OnFailedToLoadAd;

        internal CASViewFactoryImpl( CASManagerClient manager )
        {
            this.manager = manager;
        }

        protected override IAdView CreateAdView( AdSize size )
        {
            return new CASViewClient( manager, size );
        }

        public void OnGUIAd( GUIStyle style )
        {
            for (int i = 0; i < adViews.Count; i++)
            {
                ( (CASViewClient)adViews[i] ).OnGUIAd( style );
            }
        }

        public void LoadCreatedViews()
        {
            for (int i = 0; i < adViews.Count; i++)
                adViews[i].Load();
        }

        public override void OnLoadedCallback( AdType type )
        {
            if (OnLoadedAd != null)
                OnLoadedAd( type );
        }

        public override void OnFailedCallback( AdType type, AdError error )
        {
            if (OnFailedToLoadAd != null)
                OnFailedToLoadAd( type, error.GetMessage() );
        }
    }

    [Serializable]
    internal class CASFullscreenView
    {
        public event Action OnAdLoaded;
        public event CASEventWithAdError OnAdFailedToLoad;
        public event Action OnAdShown;
        public event CASEventWithMeta OnAdOpening;
        public event CASEventWithError OnAdFailedToShow;
        public event Action OnAdClicked;
        public event Action OnAdClosed;
        public event Action OnAdCompleted;

        public bool active = false;
        public bool loaded = false;

        [SerializeField]
        private AdError lastError;
        private CASManagerClient manager;
        private AdType type;

        internal CASFullscreenView( CASManagerClient manager, AdType type )
        {
            this.manager = manager;
            this.type = type;
        }

        public void Load()
        {
            if (manager.IsEnabledAd( type ))
            {
                if (!loaded)
                    manager.Post( CallAdLoaded, 1.0f );
                return;
            }
            lastError = AdError.ManagerIsDisabled;
            manager.Post( CallAdLoadFail );
        }

        public void Show()
        {
            if (manager.isFullscreenAdVisible)
            {
                lastError = AdError.AlreadyDisplayed;
                manager.Post( CallAdShowFail );
            }
            else if (!manager.IsEnabledAd( type ))
            {
                lastError = AdError.ManagerIsDisabled;
                manager.Post( CallAdShowFail );
            }
            else if (type == AdType.Interstitial
                && manager._settings.lastInterImpressionTimestamp + manager._settings.interstitialInterval > Time.time)
            {
                lastError = AdError.IntervalNotYetPassed;
                manager.Post( CallAdShowFail );
            }
            else if (!loaded)
            {
                lastError = AdError.NoFill;
                manager.Post( CallAdShowFail );
            }
            else
            {
                active = true;
                manager.Post( CallAdPresent );
            }
        }

        public virtual void OnGUIAd( GUIStyle style )
        {
            if (!active)
                return;

            if (type == AdType.Interstitial
                && GUI.Button( new Rect( 0, 0, Screen.width, Screen.height ), "Close\n\nCAS Interstitial Ad", style ))
            {
                manager.Post( OnAdClicked );
                manager._settings.lastInterImpressionTimestamp = Time.time;
                active = false;
                ReloadAdAfterImpression();
                manager.Post( CallAdClosed );
            }

            if (type == AdType.Rewarded)
            {
                float width = Screen.width;
                float halfHeight = Screen.height * 0.5f;
                GUI.enabled = loaded;
                bool isClosed = GUI.Button( new Rect( 0, 0, width, halfHeight ),
                    "Close\nCAS Rewarded Video Ad", style );
                bool isCompleted = GUI.Button( new Rect( 0, halfHeight, width, halfHeight ),
                    "Complete\nCAS Rewarded Video Ad", style );
                if (isClosed || isCompleted)
                {
                    ReloadAdAfterImpression();
                    if (isCompleted)
                    {
                        manager.Post( OnAdClicked );
                        manager.Post( OnAdCompleted );
                        // Delayed OnAdClosed after OnAdComplete to simulate real behaviour.
                        manager.Post( CallAdClosed, UnityEngine.Random.Range( 0.3f, 1.0f ) );
                    }
                    else
                    {
                        manager.Post( CallAdClosed );
                    }
                }
                GUI.enabled = true;
            }
        }

        private void ReloadAdAfterImpression()
        {
            loaded = false;
            if (manager.isAutolod)
            {
                Load();
                return;
            }
            lastError = AdError.NotReady;
            manager.Post( CallAdLoadFail );
        }

        private void CallAdLoaded()
        {
            loaded = true;
            if (OnAdLoaded != null)
                OnAdLoaded();
            manager.viewFactory.OnLoadedCallback( type );
        }

        private void CallAdLoadFail()
        {
            if (OnAdFailedToLoad != null)
                OnAdFailedToLoad( lastError );
            manager.viewFactory.OnFailedCallback( type, lastError );
        }

        private void CallAdShowFail()
        {
            if (OnAdFailedToShow != null)
                OnAdFailedToShow( lastError.GetMessage() );
        }

        private void CallAdPresent()
        {
            if (OnAdShown != null)
                OnAdShown();
            if (OnAdOpening != null)
                OnAdOpening( new CASImpressionClient( type ) );
        }

        private void CallAdClosed()
        {
            active = false;
            if (OnAdClosed != null)
                OnAdClosed();
        }
    }
}
#endif