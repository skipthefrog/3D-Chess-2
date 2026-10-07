using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// A text WebSocket that works on every platform the game ships to:
/// iPhone uses Apple's native NSURLSessionWebSocketTask (Plugins/iOS/AppleWebSocket.mm),
/// because Unity's managed ClientWebSocket can't connect over https on iOS.
/// Everywhere else (editor, WebGL, desktop) uses NativeWebSocket.
/// Events are raised on the main thread.
/// </summary>
public class GameSocket
{
    public event Action OnOpen;
    public event Action<string> OnMessage;
    public event Action<string> OnError;
    public event Action<string> OnClose;

    public bool IsOpen { get; private set; }

    private readonly string url;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void AppleSocket_Open(int socketId, string url, string receiver);
    [DllImport("__Internal")] private static extern void AppleSocket_Send(int socketId, string text);
    [DllImport("__Internal")] private static extern void AppleSocket_Close(int socketId);

    private static int nextId = 1;
    private static readonly Dictionary<int, GameSocket> live = new Dictionary<int, GameSocket>();
    private int id;
#else
    private NativeWebSocket.WebSocket socket;
#endif

    public GameSocket(string url)
    {
        this.url = url;
    }

    public Task Connect()
    {
#if UNITY_IOS && !UNITY_EDITOR
        id = nextId++;
        live[id] = this;
        AppleSocket_Open(id, url, AppleSocketReceiver.Name);
        return Task.CompletedTask;
#else
        socket = new NativeWebSocket.WebSocket(url);
        socket.OnOpen += () => { IsOpen = true; OnOpen?.Invoke(); };
        socket.OnMessage += bytes => OnMessage?.Invoke(Encoding.UTF8.GetString(bytes));
        socket.OnError += error => OnError?.Invoke(error);
        socket.OnClose += code => { IsOpen = false; OnClose?.Invoke(code.ToString()); };
        return socket.Connect();
#endif
    }

    public void SendText(string text)
    {
        if (!IsOpen) return;
#if UNITY_IOS && !UNITY_EDITOR
        AppleSocket_Send(id, text);
#else
        _ = socket.SendText(text);
#endif
    }

    public Task Close()
    {
        IsOpen = false;
#if UNITY_IOS && !UNITY_EDITOR
        live.Remove(id);
        AppleSocket_Close(id);
        return Task.CompletedTask;
#else
        return socket != null ? socket.Close() : Task.CompletedTask;
#endif
    }

    /// <summary>Call every frame (NativeWebSocket queues messages off the main thread)</summary>
    public void Dispatch()
    {
#if !(UNITY_IOS && !UNITY_EDITOR) && (!UNITY_WEBGL || UNITY_EDITOR)
        socket?.DispatchMessageQueue();
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    // Called by AppleSocketReceiver with "id|payload"
    internal static void Deliver(string message, Action<GameSocket, string> handle)
    {
        int bar = message.IndexOf('|');
        if (bar < 0 || !int.TryParse(message.Substring(0, bar), out int socketId)) return;
        if (live.TryGetValue(socketId, out GameSocket s)) handle(s, message.Substring(bar + 1));
    }

    internal void RaiseOpen() { IsOpen = true; OnOpen?.Invoke(); }
    internal void RaiseMessage(string text) => OnMessage?.Invoke(text);
    internal void RaiseError(string error) => OnError?.Invoke(error);
    internal void RaiseClose(string reason)
    {
        IsOpen = false;
        live.Remove(id);
        OnClose?.Invoke(reason);
    }
#endif
}

#if UNITY_IOS && !UNITY_EDITOR
/// <summary>Receives UnitySendMessage callbacks from AppleWebSocket.mm</summary>
public class AppleSocketReceiver : MonoBehaviour
{
    public const string Name = "AppleSocketReceiver";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var go = new GameObject(Name);
        DontDestroyOnLoad(go);
        go.AddComponent<AppleSocketReceiver>();
    }

    public void OnAppleSocketOpen(string message) => GameSocket.Deliver(message, (s, _) => s.RaiseOpen());
    public void OnAppleSocketMessage(string message) => GameSocket.Deliver(message, (s, text) => s.RaiseMessage(text));
    public void OnAppleSocketError(string message) => GameSocket.Deliver(message, (s, text) => s.RaiseError(text));
    public void OnAppleSocketClose(string message) => GameSocket.Deliver(message, (s, text) => s.RaiseClose(text));
}
#endif
