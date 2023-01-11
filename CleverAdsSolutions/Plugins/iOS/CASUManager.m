//
//  CASUManager.m
//  CASUnityPlugin
//
//  Copyright © 2022 Clever Ads Solutions. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "CASUManager.h"
#import "CASUPluginUtil.h"

@implementation CASUManager

- (instancetype)initWithManager:(CASMediationManager *)manager forClient:(CASManagerClientRef _Nullable *)client {
    self = [super init];

    if (self) {
        self.casManager = manager;
        _client = client;
        _interCallback = [[CASUCallback alloc] initWithComplete:false];
        _interCallback.client = client;
        _rewardCallback = [[CASUCallback alloc] initWithComplete:true];
        _rewardCallback.client = client;
        _appReturnDelegate = [[CASUCallback alloc] initWithComplete:false];
        _appReturnDelegate.client = client;
    }

    return self;
}

- (void)presentInter {
    [_casManager presentInterstitialFromRootViewController:[CASUPluginUtil unityGLViewController]
                                                  callback:_interCallback];
}

- (void)presentReward {
    [_casManager presentRewardedAdFromRootViewController:[CASUPluginUtil unityGLViewController]
                                                callback:_rewardCallback];
}

- (void)setLastPageAdFor:(NSString *)content {
    self.casManager.lastPageAdContent = [CASLastPageAdContent createFrom:content];
}

- (void)onAdLoaded:(enum CASType)adType {
    // Callback called from any thread, so swith to UI thread for Unity.
    if (adType == CASTypeInterstitial) {
        [self.interCallback callInUITheradLoadedCallback];
    } else if (adType == CASTypeRewarded) {
        [self.rewardCallback callInUITheradLoadedCallback];
    }
}

- (void)onAdFailedToLoad:(enum CASType) adType withError:(NSString *)error {
    // Callback called from any thread, so swith to UI thread for Unity.
    if (adType == CASTypeInterstitial) {
        [self.interCallback callInUITheradFailedToLoadCallbackWithError:error];
    } else if (adType == CASTypeRewarded) {
        [self.rewardCallback callInUITheradFailedToLoadCallbackWithError:error];
    }
}

- (void)enableReturnAds {
    [_casManager enableAppReturnAdsWith:_appReturnDelegate];
}

- (void)disableReturnAds {
    [_casManager disableAppReturnAds];
}

- (void)skipNextAppReturnAd {
    [_casManager skipNextAppReturnAds];
}

@end
