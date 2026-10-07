#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// Adds a UIScene manifest to the exported Xcode project's Info.plist so iOS 27 accepts
/// the app. The scene delegate itself is Assets/Plugins/iOS/UnitySceneDelegate.mm.
/// </summary>
public static class iOSScenePostProcess
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict manifest = plist.root.CreateDict("UIApplicationSceneManifest");
        manifest.SetBoolean("UIApplicationSupportsMultipleScenes", false);
        PlistElementDict configurations = manifest.CreateDict("UISceneConfigurations");
        PlistElementArray roles = configurations.CreateArray("UIWindowSceneSessionRoleApplication");
        PlistElementDict config = roles.AddDict();
        config.SetString("UISceneConfigurationName", "Default Configuration");
        config.SetString("UISceneDelegateClassName", "UnitySceneDelegate");

        plist.WriteToFile(plistPath);
    }
}
#endif
