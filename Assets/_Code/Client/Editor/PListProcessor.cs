#if UNITY_IOS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS_I2Loc.Xcode;
using UnityEngine;

public static class PListProcessor
{
    const string FirebaseEnableAnalyticsKey = "FIREBASE_ANALYTICS_COLLECTION_ENABLED";
    const string FirebaseMessagingAutoInitKey = "FirebaseMessagingAutoInitEnabled";
    const string FacebookAutoLogEventsKey = "FacebookAutoLogAppEventsEnabled";

    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        // Get plist
        string plistPath = pathToBuiltProject + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Get root
        PlistElementDict rootDict = plist.root;

        setBoolean(rootDict, FirebaseEnableAnalyticsKey, false);
        setBoolean(rootDict, FirebaseMessagingAutoInitKey, false);
        setBoolean(rootDict, FacebookAutoLogEventsKey, false);

        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());
    }

    static void setBoolean(PlistElementDict dict, string key, bool val)
    {
        Debug.LogFormat("Modifying plist: set {0} to {1}", key, val);
        dict.SetBoolean(key, val);
    }
}
#endif