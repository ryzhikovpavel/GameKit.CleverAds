﻿//
//  Clever Ads Solutions Unity Plugin
//
//  Copyright © 2022 CleverAdsSolutions. All rights reserved.
//

using UnityEngine;
using UnityEditor;

namespace CAS.AdObject
{
    internal class BaseAdObjectInspector : Editor
    {
        protected bool loadEventsFoldout;
        protected bool contentEventsFoldout;

        protected SerializedProperty managerIdProp;

        protected SerializedProperty onAdLoadedProp;
        protected SerializedProperty onAdFailedToLoadProp;
        protected SerializedProperty onAdShownProp;
        protected SerializedProperty onAdClickedProp;

        protected void OnEnable()
        {
            var obj = serializedObject;
            managerIdProp = obj.FindProperty( "managerId" );

            onAdLoadedProp = obj.FindProperty( "OnAdLoaded" );
            onAdFailedToLoadProp = obj.FindProperty( "OnAdFailedToLoad" );
            onAdShownProp = obj.FindProperty( "OnAdShown" );
            onAdClickedProp = obj.FindProperty( "OnAdClicked" );
        }

        public override void OnInspectorGUI()
        {
            var obj = serializedObject;
            obj.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField( managerIdProp );
            OnAdditionalPropertiesGUI();

            loadEventsFoldout = GUILayout.Toggle( loadEventsFoldout, "Load Ad callbacks", EditorStyles.foldout );
            if (loadEventsFoldout)
            {
                EditorGUILayout.PropertyField( onAdLoadedProp );
                EditorGUILayout.PropertyField( onAdFailedToLoadProp );
            }

            contentEventsFoldout = GUILayout.Toggle( contentEventsFoldout, "Content callbacks", EditorStyles.foldout );
            if (contentEventsFoldout)
                OnCallbacksGUI();

            OnFooterGUI();

            obj.ApplyModifiedProperties();
        }

        protected virtual void OnAdditionalPropertiesGUI() { }

        protected virtual void OnFooterGUI() { }

        protected virtual void OnCallbacksGUI()
        {
            EditorGUILayout.PropertyField( onAdShownProp );
            EditorGUILayout.PropertyField( onAdClickedProp );
        }
    }

    [CustomEditor( typeof( BannerAdObject ) )]
    internal class BannerAdObjectInspector : BaseAdObjectInspector
    {
        private SerializedProperty adPositionProp;
        private SerializedProperty adSizeProp;
        private SerializedProperty adOffsetProp;
        private SerializedProperty onAdHiddenProp;
        private BannerAdObject adView;
        private readonly string[] allowedPositions = new string[]{
            "Top Center",
            "Top Left",
            "Top Right",
            "Bottom Center",
            "Bottom Left",
            "Bottom Right"
        };

        private new void OnEnable()
        {
            base.OnEnable();
            var obj = serializedObject;
            adPositionProp = obj.FindProperty( "adPosition" );
            adOffsetProp = obj.FindProperty( "adOffset" );
            adSizeProp = obj.FindProperty( "adSize" );

            onAdHiddenProp = obj.FindProperty( "OnAdHidden" );
            adView = target as BannerAdObject;
        }

        protected override void OnAdditionalPropertiesGUI()
        {
            var isPlaying = Application.isPlaying;
            EditorGUI.BeginChangeCheck();
            adPositionProp.intValue = EditorGUILayout.Popup( "Ad Position", adPositionProp.intValue, allowedPositions );
            if (EditorGUI.EndChangeCheck())
            {
                adOffsetProp.vector2IntValue = Vector2Int.zero;
                if (isPlaying)
                    adView.SetAdPositionEnumIndex( adPositionProp.intValue );
            }

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup( adPositionProp.intValue != ( int )AdPosition.TopLeft );
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( adOffsetProp );
            GUILayout.Label( "DP", GUILayout.ExpandWidth( false ) );
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck() && isPlaying)
            {
                var newPos = adOffsetProp.vector2IntValue;
                adView.SetAdPosition( newPos.x, newPos.y );
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField( "Screen positioning coordinates are only available for the TopLeft position.", EditorStyles.wordWrappedMiniLabel );
            // Calling the calculation in the Editor results in incorrect data
            // because getting the screen size returns the size of the inspector.
            //if (isPlaying)
            //{
            //    EditorGUI.BeginDisabledGroup( true );
            //    EditorGUILayout.RectField( "Rect in pixels", adView.rectInPixels );
            //    EditorGUI.EndDisabledGroup();
            //}
            EditorGUI.indentLevel--;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField( adSizeProp );
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                adView.SetAdSizeEnumIndex( adSizeProp.intValue );
        }

        protected override void OnCallbacksGUI()
        {
            base.OnCallbacksGUI();
            EditorGUILayout.PropertyField( onAdHiddenProp );
        }

        protected override void OnFooterGUI()
        {
            EditorGUILayout.LabelField( "Use `gameObject.SetActive(visible)` method to show/hide banner ad.",
                EditorStyles.wordWrappedMiniLabel );
        }
    }

    [CustomEditor( typeof( InterstitialAdObject ) )]
    [CanEditMultipleObjects]
    internal class InterstitialAdObjectInspector : BaseAdObjectInspector
    {
        private SerializedProperty onAdFailedToShowProp;
        private SerializedProperty onAdClosedProp;

        private new void OnEnable()
        {
            base.OnEnable();
            var obj = serializedObject;
            onAdFailedToShowProp = obj.FindProperty( "OnAdFailedToShow" );
            onAdClosedProp = obj.FindProperty( "OnAdClosed" );
        }

        protected override void OnCallbacksGUI()
        {
            EditorGUILayout.PropertyField( onAdFailedToShowProp );
            base.OnCallbacksGUI();
            EditorGUILayout.PropertyField( onAdClosedProp );
        }

        protected override void OnFooterGUI()
        {
            EditorGUILayout.LabelField( "Call `Present()` method to show Interstitial Ad.",
                EditorStyles.wordWrappedMiniLabel );
        }
    }

    [CustomEditor( typeof( RewardedAdObject ) )]
    [CanEditMultipleObjects]
    internal class RewardedAdObjectInspector : BaseAdObjectInspector
    {
        private SerializedProperty onAdFailedToShowProp;
        private SerializedProperty onAdClosedProp;

        private SerializedProperty restartInterstitialIntervalProp;
        private SerializedProperty onRewardProp;

        private new void OnEnable()
        {
            base.OnEnable();
            var obj = serializedObject;
            restartInterstitialIntervalProp = obj.FindProperty( "restartInterstitialInterval" );
            onAdFailedToShowProp = obj.FindProperty( "OnAdFailedToShow" );
            onAdClosedProp = obj.FindProperty( "OnAdClosed" );
            onRewardProp = obj.FindProperty( "OnReward" );
        }

        protected override void OnAdditionalPropertiesGUI()
        {
            restartInterstitialIntervalProp.boolValue = EditorGUILayout.ToggleLeft(
                "Restart Interstitial Ad interval on rewarded ad closed",
                restartInterstitialIntervalProp.boolValue
            );
            EditorGUILayout.PropertyField( onRewardProp );
        }

        protected override void OnCallbacksGUI()
        {
            EditorGUILayout.PropertyField( onAdFailedToShowProp );
            base.OnCallbacksGUI();
            EditorGUILayout.PropertyField( onAdClosedProp );
        }

        protected override void OnFooterGUI()
        {
            EditorGUILayout.LabelField( "Call `Present()` method to show Rewarded Ad.",
                EditorStyles.wordWrappedMiniLabel );
        }
    }

    [CustomEditor( typeof( ReturnToPlayAdObject ) )]
    internal class ReturnToPlayAdObjectInspector : BaseAdObjectInspector
    {
        private SerializedProperty allowAdProp;
        private SerializedProperty onAdFailedToShowProp;
        private SerializedProperty onAdClosedProp;

        private new void OnEnable()
        {
            base.OnEnable();
            var obj = serializedObject;
            allowAdProp = obj.FindProperty( "_allowReturnToPlayAd" );
            onAdFailedToShowProp = obj.FindProperty( "OnAdFailedToShow" );
            onAdClosedProp = obj.FindProperty( "OnAdClosed" );
        }

        protected override void OnAdditionalPropertiesGUI()
        {
            EditorGUI.BeginChangeCheck();
            allowAdProp.boolValue = EditorGUILayout.ToggleLeft(
                "Allow ads to show on return to game",
                allowAdProp.boolValue
            );
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                ( ( ReturnToPlayAdObject )target ).allowReturnToPlayAd = allowAdProp.boolValue;
            }
        }

        protected override void OnCallbacksGUI()
        {
            EditorGUILayout.PropertyField( onAdFailedToShowProp );
            base.OnCallbacksGUI();
            EditorGUILayout.PropertyField( onAdClosedProp );
        }
    }
}