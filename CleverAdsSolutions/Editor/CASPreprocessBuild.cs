﻿//
//  Clever Ads Solutions Unity Plugin
//
//  Copyright © 2022 CleverAdsSolutions. All rights reserved.
//

#if UNITY_ANDROID || UNITY_IOS || CASDeveloper
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System;
using Utils = CAS.UEditor.CASEditorUtils;
using System.Text;
using UnityEngine.Networking;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace CAS.UEditor
{
#if UNITY_2018_1_OR_NEWER
    internal class CASPreprocessBuild : IPreprocessBuildWithReport
#else
    public class CASPreprocessBuild : IPreprocessBuild
#endif
    {
        private const string casTitle = "CAS Configure project";
        #region IPreprocessBuild
        public int callbackOrder { get { return -25000; } }

#if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild( BuildReport report )
        {
            BuildTarget target = report.summary.platform;
#else
        public void OnPreprocessBuild( BuildTarget target, string path )
        {
#endif
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
                return;
            try
            {
                var editorSettings = CASEditorSettings.Load();
                if (editorSettings.buildPreprocessEnabled)
                {
                    ConfigureProject( target, editorSettings );
#if UNITY_2019_1_OR_NEWER
                }
            }
            finally
            {
                // Unity 2020 does not replace progress bars at the start of a build.
                EditorUtility.ClearProgressBar();
            }
#else
                    EditorUtility.DisplayProgressBar( "Hold on", "Prepare components...", 0.95f );
                }
            }
            catch (Exception e)
            {
                // If no errors are found then there is no need to clear the progress for the user.
                EditorUtility.ClearProgressBar();
                throw e;
            }
#endif
        }
        #endregion

        public static void ConfigureProject( BuildTarget target, CASEditorSettings editorSettings )
        {
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
                return;

            var settings = Utils.GetSettingsAsset( target, false );
            if (!settings)
                Utils.StopBuildWithMessage( "Settings asset not found. Please use menu Assets > CleverAdsSolutions > Settings " +
                    "to create and set settings for build.", target );

            var deps = DependencyManager.Create( target, Audience.Mixed, true );
            if (!Utils.IsBatchMode())
            {
                var newCASVersion = Utils.GetNewVersionOrNull( Utils.gitUnityRepo, MobileAds.wrapperVersion, false );
                if (newCASVersion != null)
                    Utils.DialogOrCancelBuild( "There is a new version " + newCASVersion + " of the CAS Unity available for update.", target );

                if (deps != null)
                {
                    if (!deps.installedAny)
                        Utils.StopBuildWithMessage( "Dependencies of native SDK were not found. " +
                        "Please use 'Assets > CleverAdsSolutions > Settings' menu to integrate solutions or any SDK separately.", target );

                    if (deps.IsNewerVersionFound())
                        Utils.DialogOrCancelBuild( "There is a new versions of the native dependencies available for update." +
                            "Please use 'Assets > CleverAdsSolutions >Settings' menu to update.", target );
                }
            }

            if (settings.managersCount == 0 || string.IsNullOrEmpty( settings.GetManagerId( 0 ) ))
                StopBuildIDNotFound( target );

            string admobAppId = UpdateRemoteSettingsAndGetAppId( settings, target, deps );

            if (target == BuildTarget.Android)
                ConfigureAndroid( settings, editorSettings, admobAppId );
            else if (target == BuildTarget.iOS)
                ConfigureIOS();

            if (settings.IsTestAdMode() && !EditorUserBuildSettings.development)
                Debug.LogWarning( Utils.logTag + "Test Ads Mode enabled! Make sure the build is for testing purposes only!\n" +
                    "Use 'Assets > CleverAdsSolutions > Settings' menu to disable Test Ad Mode." );
            else
                Debug.Log( Utils.logTag + "Project configuration completed" );
        }

        private static void ConfigureIOS()
        {
#if UNITY_IOS || CASDeveloper
            if (!Utils.GetIOSResolverSetting<bool>( "PodfileStaticLinkFrameworks" ))
            {
                Utils.DialogOrCancelBuild( "Please enable 'Add use_frameworks!' and 'Link frameworks statically' found under " +
                        "'Assets -> External Dependency Manager -> iOS Resolver -> Settings' menu.\n" +
                        "Failing to do this step may result in undefined behavior of the plugin and doubled import of frameworks." );
            }

#if !UNITY_2020_1_OR_NEWER
            var iosVersion = PlayerSettings.iOS.targetOSVersionString;
            if (iosVersion.StartsWith( "9." ) || iosVersion.StartsWith( "10." ))
            {
                Utils.DialogOrCancelBuild( "CAS required a higher minimum deployment target. Set iOS 11.0 and continue?", BuildTarget.NoTarget );
                PlayerSettings.iOS.targetOSVersionString = "11.0";
            }
#endif
#endif
        }

        private static void ConfigureAndroid( CASInitSettings settings, CASEditorSettings editorSettings, string admobAppId )
        {
#if UNITY_ANDROID || CASDeveloper
            EditorUtility.DisplayProgressBar( casTitle, "Validate CAS Android Build Settings", 0.8f );

            const string deprecatedPluginPath = "Assets/Plugins/CAS";
            if (Directory.Exists( deprecatedPluginPath ))
                AssetDatabase.MoveAssetToTrash( deprecatedPluginPath );

            const string deprecatedConfigFileInRes = Utils.androidResSettingsPath + ".json";
            if (File.Exists( deprecatedConfigFileInRes ))
                AssetDatabase.MoveAssetToTrash( deprecatedConfigFileInRes );

#if !UNITY_2021_2_OR_NEWER
            // 19 - AndroidSdkVersions.AndroidApiLevel19
            // Deprecated in Unity 2021.2
            if (PlayerSettings.Android.minSdkVersion < ( AndroidSdkVersions )19)
            {
                Utils.DialogOrCancelBuild( "CAS required a higher minimum SDK API level. Set SDK level 19 (KitKat) and continue?", BuildTarget.NoTarget );
                PlayerSettings.Android.minSdkVersion = ( AndroidSdkVersions )19;
            }
#endif

#if !UNITY_2019_1_OR_NEWER
            // 0 = AndroidBuildSystem.Internal
            // Deprecated in Unity 2019
            if (EditorUserBuildSettings.androidBuildSystem == 0)
            {
                Utils.DialogOrCancelBuild( "Unity Internal build system no longer supported. Set Gradle build system and continue?", BuildTarget.NoTarget );
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            }
#endif

            HashSet<string> promoAlias = new HashSet<string>();
            if (editorSettings.generateAndroidQuerriesForPromo)
            {
                for (int i = 0; i < settings.managersCount; i++)
                    Utils.GetCrossPromoAlias( BuildTarget.Android, settings.GetManagerId( i ), promoAlias );
            }

            UpdateAndroidPluginManifest( admobAppId, promoAlias, editorSettings );

            CASPreprocessGradle.Configure( editorSettings );
#endif
        }

        private static string UpdateRemoteSettingsAndGetAppId( CASInitSettings settings, BuildTarget platform, DependencyManager deps )
        {
            string appId = null;
            string updateSettingsError = "";
            for (int i = 0; i < settings.managersCount; i++)
            {
                var managerId = settings.GetManagerId( i );
                if (managerId == null || managerId.Length < 5)
                    continue;
                try
                {
                    string newAppId = DownloadRemoteSettings( managerId, platform, settings, deps );
                    if (!string.IsNullOrEmpty( appId ) || string.IsNullOrEmpty( newAppId ))
                        continue;
                    if (newAppId.Contains( '~' ))
                    {
                        appId = newAppId;
                        continue;
                    }
                    if (i == 0)
                    {
                        Debug.LogError( Utils.logTag + "CAS id [" + managerId +
                            "] has an error in server settings. Please contact support!" );
                    }
                }
                catch (Exception e)
                {
                    updateSettingsError = e.Message;
                }
            }
            if (!string.IsNullOrEmpty( appId ) || settings.IsTestAdMode())
                return appId;

            const string title = "Update CAS remote settings";
            int dialogResponse = 0;
            var targetId = settings.GetManagerId( 0 );

            var message = updateSettingsError +
                "\nPlease try using a real identifier in the first place else contact support." +
                "\n- Warning! -" +
                "\n1. Continue build the app for release with current settings can reduce monetization revenue." +
                "\n2. When build to testing your app, make sure you use Test Ads mode rather than live ads. " +
                "Failure to do so can lead to suspension of your account.";

            Debug.LogError( Utils.logTag + message );
            if (!Utils.IsBatchMode())
                dialogResponse = EditorUtility.DisplayDialogComplex( title, message,
                    "Continue", "Cancel Build", "Select settings file" );

            if (dialogResponse == 0)
            {
                var cachePath = Utils.GetNativeSettingsPath( platform, targetId );
                if (File.Exists( cachePath ))
                    return Utils.GetAdmobAppIdFromJson( File.ReadAllText( cachePath ) );
                return null;
            }
            if (dialogResponse == 1)
            {
                Utils.StopBuildWithMessage( "Build canceled", BuildTarget.NoTarget );
                return null;
            }
            return Utils.SelectSettingsFileAndGetAppId( targetId, platform );
        }

        private static void UpdateAndroidPluginManifest( string admobAppId, HashSet<string> queries, CASEditorSettings settings )
        {
            const string metaAdmobApplicationID = "com.google.android.gms.ads.APPLICATION_ID";
            const string metaAdmobDelayInit = "com.google.android.gms.ads.DELAY_APP_MEASUREMENT_INIT";

            XNamespace ns = "http://schemas.android.com/apk/res/android";
            XNamespace nsTools = "http://schemas.android.com/tools";
            XName nameAttribute = ns + "name";
            XName valueAttribute = ns + "value";

            string manifestPath = Path.GetFullPath( Utils.androidLibManifestPath );

            CreateAndroidLibIfNedded();

            if (string.IsNullOrEmpty( admobAppId ))
                admobAppId = Utils.androidAdmobSampleAppID;

            try
            {
                var document = new XDocument(
                    new XDeclaration( "1.0", "utf-8", null ),
                    new XComment( "This file is automatically generated by CAS Unity plugin from `Assets > CleverAdsSolutions > Android Settings`" ),
                    new XComment( "Do not modify this file. YOUR CHANGES WILL BE ERASED!" ) );
                var elemManifest = new XElement( "manifest",
                    new XAttribute( XNamespace.Xmlns + "android", ns ),
                    new XAttribute( XNamespace.Xmlns + "tools", nsTools ),
                    new XAttribute( "package", "com.cleversolutions.ads.unitycas" ),
                    new XAttribute( ns + "versionName", MobileAds.wrapperVersion ),
                    new XAttribute( ns + "versionCode", 1 ) );
                document.Add( elemManifest );

                var delayInitState = settings.delayAppMeasurementGADInit ? "true" : "false";

                var elemApplication = new XElement( "application" );

                var elemAppIdMeta = new XElement( "meta-data",
                        new XAttribute( nameAttribute, metaAdmobApplicationID ),
                        new XAttribute( valueAttribute, admobAppId ) );
                elemApplication.Add( elemAppIdMeta );

                var elemDelayInitMeta = new XElement( "meta-data",
                        new XAttribute( nameAttribute, metaAdmobDelayInit ),
                        new XAttribute( valueAttribute, delayInitState ) );
                elemApplication.Add( elemDelayInitMeta );

                var elemUsesLibrary = new XElement( "uses-library",
                    new XAttribute( ns + "required", "false" ),
                    new XAttribute( nameAttribute, "org.apache.http.legacy" ) );
                elemApplication.Add( elemUsesLibrary );
                elemManifest.Add( elemApplication );

                var elemInternetPermission = new XElement( "uses-permission",
                    new XAttribute( nameAttribute, "android.permission.INTERNET" ) );
                elemManifest.Add( elemInternetPermission );

                var elemNetworkPermission = new XElement( "uses-permission",
                    new XAttribute( nameAttribute, "android.permission.ACCESS_NETWORK_STATE" ) );
                elemManifest.Add( elemNetworkPermission );

                var elemWIFIPermission = new XElement( "uses-permission",
                    new XAttribute( nameAttribute, "android.permission.ACCESS_WIFI_STATE" ) );
                elemManifest.Add( elemWIFIPermission );

                var elemAdIDPermission = new XElement( "uses-permission",
                    new XAttribute( nameAttribute, "com.google.android.gms.permission.AD_ID" ) );
                if (settings.permissionAdIdRemoved)
                    elemAdIDPermission.SetAttributeValue( nsTools + "node", "remove" );
                elemManifest.Add( elemAdIDPermission );

                if (queries.Count > 0)
                {
                    var elemQueries = new XElement( "queries" );
                    elemQueries.Add( new XComment( "CAS Cross promotion" ) );
                    foreach (var item in queries)
                    {
                        elemQueries.Add( new XElement( "package",
                            new XAttribute( nameAttribute, item ) ) );
                    }
                    elemManifest.Add( elemQueries );
                }

                var exist = File.Exists( Utils.androidLibManifestPath );
                // XDocument required absolute path
                document.Save( manifestPath );
                // But Unity not support absolute path
                if (!exist)
                    AssetDatabase.ImportAsset( Utils.androidLibManifestPath );
            }
            catch (Exception e)
            {
                Debug.LogException( e );
            }
        }

        private static void CreateAndroidLibIfNedded()
        {
            const string libResFolder = Utils.androidLibFolderPath + "/res/xml";
            if (!AssetDatabase.IsValidFolder( libResFolder ))
            {
                Directory.CreateDirectory( libResFolder );
                AssetDatabase.ImportAsset( libResFolder );
            }

            if (!File.Exists( Utils.androidLibPropertiesPath ))
            {
                const string pluginProperties =
                    "# This file is automatically generated by CAS Unity plugin.\n" +
                    "# Do not modify this file -- YOUR CHANGES WILL BE ERASED!\n" +
                    "android.library=true\n" +
                    "target=android-29\n";
                File.WriteAllText( Utils.androidLibPropertiesPath, pluginProperties );
                AssetDatabase.ImportAsset( Utils.androidLibPropertiesPath );
            }

            if (!File.Exists( Utils.androidLibNetworkConfigPath ))
            {
                const string networkSecurity =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                    "<network-security-config>\n" +
                    "    <!-- The Meta AN SDK use 127.0.0.1 as a caching proxy to cache media files in the SDK -->\n" +
                    "    <domain-config cleartextTrafficPermitted=\"true\">\n" +
                    "        <domain includeSubdomains=\"true\">127.0.0.1</domain>\n" +
                    "    </domain-config>\n" +
                    "</network-security-config>";
                File.WriteAllText( Utils.androidLibNetworkConfigPath, networkSecurity );
                AssetDatabase.ImportAsset( Utils.androidLibNetworkConfigPath );
            }
        }

        private static string DownloadRemoteSettings( string managerID, BuildTarget platform, CASInitSettings settings, DependencyManager deps )
        {
            const string title = "Update CAS remote settings";

            var editorSettings = CASEditorSettings.Load();

            #region Create request URL
            #region Hash
            var managerIdBytes = new UTF8Encoding().GetBytes( managerID );
            var suffix = new byte[] { 48, 77, 101, 68, 105, 65, 116, 73, 111, 78, 104, 65, 115, 72 };
            if (platform == BuildTarget.iOS)
                suffix[0] = 49;
            var sourceBytes = new byte[managerID.Length + suffix.Length];
            Array.Copy( managerIdBytes, 0, sourceBytes, 0, managerIdBytes.Length );
            Array.Copy( suffix, 0, sourceBytes, managerIdBytes.Length, suffix.Length );

            var hashBytes = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash( sourceBytes );
            StringBuilder hashBuilder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
                hashBuilder.Append( Convert.ToString( hashBytes[i], 16 ).PadLeft( 2, '0' ) );
            var hash = hashBuilder.ToString().PadLeft( 32, '0' );
            #endregion

            var urlBuilder = new StringBuilder( "https://psvpromo.psvgamestudio.com/Scr/cas.php?platform=" )
                .Append( platform == BuildTarget.Android ? 0 : 1 )
                .Append( "&bundle=" ).Append( UnityWebRequest.EscapeURL( managerID ) )
                .Append( "&hash=" ).Append( hash )
                .Append( "&lang=" ).Append( SystemLanguage.English )
                .Append( "&appDev=2" )
                .Append( "&appV=" ).Append( PlayerSettings.bundleVersion )
                .Append( "&coppa=" ).Append( ( int )settings.defaultAudienceTagged )
                .Append( "&adTypes=" ).Append( ( int )settings.allowedAdFlags )
                .Append( "&nets=" ).Append( DependencyManager.GetActiveMediationPattern( deps ) )
                .Append( "&orient=" ).Append( Utils.GetOrientationId() )
                .Append( "&framework=Unity_" ).Append( Application.unityVersion );
            if (deps != null)
            {
                var buildCode = deps.GetInstalledBuildCode();
                if (buildCode > 0)
                    urlBuilder.Append( "&sdk=" ).Append( buildCode );
            }
            if (string.IsNullOrEmpty( editorSettings.mostPopularCountryOfUsers ))
                urlBuilder.Append( "&country=" ).Append( "US" );
            else
                urlBuilder.Append( "&country=" ).Append( editorSettings.mostPopularCountryOfUsers );
            if (platform == BuildTarget.Android)
                urlBuilder.Append( "&appVC=" ).Append( PlayerSettings.Android.bundleVersionCode );

            #endregion

            using (var loader = UnityWebRequest.Get( urlBuilder.ToString() ))
            {
                try
                {
                    loader.SendWebRequest();
                    while (!loader.isDone)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar( title, managerID,
                            Mathf.Repeat( ( float )EditorApplication.timeSinceStartup * 0.2f, 1.0f ) ))
                        {
                            loader.Dispose();
                            throw new Exception( "Update CAS Settings canceled" );
                        }
                    }
                    if (string.IsNullOrEmpty( loader.error ))
                    {
                        var content = loader.downloadHandler.text.Trim();
                        if (string.IsNullOrEmpty( content ))
                            throw new Exception( "ManagerID [" + managerID + "] is not registered in CAS." );

                        EditorUtility.DisplayProgressBar( title, "Write CAS settings", 0.7f );
                        var data = JsonUtility.FromJson<AdmobAppIdData>( content );
                        Utils.WriteToFile( content, Utils.GetNativeSettingsPath( platform, managerID ) );
                        return data.admob_app_id;
                    }
                    throw new Exception( "Server response " + loader.responseCode + ": " + loader.error );
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private static void StopBuildIDNotFound( BuildTarget target )
        {
            Utils.StopBuildWithMessage( "Settings not found manager ids for " + target.ToString() +
                " platform. For a successful build, you need to specify at least one ID" +
                " that you use in the project. To test integration, you can use test mode with 'demo' manager id.", target );
        }
    }
}
#endif