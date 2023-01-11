//
//  CSAUTypes.h
//  CASUnityPlugin
//
//  Copyright © 2022 Clever Ads Solutions. All rights reserved.
//


/// Type representing a Unity MediationManager ad client.
typedef const void * CASManagerClientRef;
/// Type representing a CASMediationManager type
typedef const void * CASUManagerRef;
/// Type representing a CASManagerBuilder type
typedef const void * CASManagerBuilderRef;

/// Type representing a Unity AdView ad client.
typedef const void * CASViewClientRef;
/// Type representing a CASUView type
typedef const void * CASUViewRef;

/// Type representing a NSObject<CASStatusHandler> type
typedef const void * CASImpressionRef;

// MARK: - CAS Mediation Manager callbacks
typedef void (*CASUInitializationCompleteCallback)(CASManagerClientRef *manager, const char *error, BOOL withConsent, BOOL isTestMode);

typedef void (*CASUDidLoadedAdCallback)(CASManagerClientRef *manager);
typedef void (*CASUDidFailedAdCallback)(CASManagerClientRef *manager, NSInteger error);

typedef void (*CASUWillPresentAdCallback)(CASManagerClientRef *manager, CASImpressionRef impression);
typedef void (*CASUDidShowAdFailedWithErrorCallback)(CASManagerClientRef *manager, const char *error);
typedef void (*CASUDidClickedAdCallback)(CASManagerClientRef *manager);
typedef void (*CASUDidCompletedAdCallback)(CASManagerClientRef *manager);
typedef void (*CASUDidClosedAdCallback)(CASManagerClientRef *manager);

// MARK: - CAS AdView callbacks
typedef void (*CASUViewDidLoadCallback)(CASViewClientRef *view);
typedef void (*CASUViewDidFailedCallback)(CASViewClientRef *view, NSInteger error);
typedef void (*CASUViewWillPresentCallback)(CASViewClientRef *view, CASImpressionRef impression);
typedef void (*CASUViewDidClickedCallback)(CASViewClientRef *view);
typedef void (*CASUViewDidRectCallback)(CASViewClientRef *view, float x, float y, float width, float height);

typedef void (*CASUATTCompletion)(NSUInteger status);
