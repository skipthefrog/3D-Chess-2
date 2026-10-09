using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Sign in with Apple (iPhone only; Plugins/iOS/AppleSignIn.mm). Gives back Apple's
/// identity token, which OnlineClient sends to the server to link or restore the account.
/// </summary>
public class AppleSignIn : MonoBehaviour
{
    private const string ReceiverName = "AppleSignInReceiver";
    private static Action<string, string> pending; // (token, error)

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void AppleSignIn_Start(string receiver);
    public static bool IsAvailable => true;
#else
    public static bool IsAvailable => false;
#endif

    /// <summary>Show Apple's sign-in sheet. Calls back with (identityToken, null) or (null, error).</summary>
    public static void Start(Action<string, string> done)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (GameObject.Find(ReceiverName) == null)
        {
            var go = new GameObject(ReceiverName);
            DontDestroyOnLoad(go);
            go.AddComponent<AppleSignIn>();
        }
        pending = done;
        AppleSignIn_Start(ReceiverName);
#else
        done(null, "Sign in with Apple is only available on iPhone");
#endif
    }

    public void OnAppleSignIn(string token) => Finish(token, null);
    public void OnAppleSignInError(string message) => Finish(null, message);

    private static void Finish(string token, string error)
    {
        var callback = pending;
        pending = null;
        callback?.Invoke(token, error);
    }
}
