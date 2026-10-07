using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Connection to the Cloudflare game server (server-cf): guest sign-in, creating and
/// joining rooms, Find Match, and the room's WebSocket. Lives across scene loads.
/// Works on iPhone and WebGL (NativeWebSocket uses the browser's WebSocket on web).
/// </summary>
public class OnlineClient : MonoBehaviour
{
    // Production server. Development builds can override with PlayerPrefs "ServerUrl".
    public const string DefaultServerUrl = "https://3d-chess.bomsapostudios.workers.dev";

    public static OnlineClient Instance { get; private set; }

    public string ServerUrl
    {
        get
        {
            string custom = PlayerPrefs.GetString("ServerUrl", "");
            if (!string.IsNullOrEmpty(custom)) return custom.TrimEnd('/');
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Editor and development (Simulator) builds use the local server: `npm run dev` in server-cf
            return "http://localhost:8787";
#else
            return DefaultServerUrl;
#endif
        }
    }

    // Session state
    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
    public string RoomCode { get; private set; }
    public PieceColor LocalColor { get; private set; }
    public bool IsHost { get; private set; }
    public string PlayerName => PlayerPrefs.GetString("OnlineName", "");

    // Events (raised on the main thread)
    public event Action<ServerMessage> OnMessage;
    public event Action<string> OnClosed;

    private WebSocket socket;
    private WebSocket matchSocket;
    private int nextRequestId = 1;
    private readonly Dictionary<int, Action<ServerMessage>> pending = new Dictionary<int, Action<ServerMessage>>();
    private bool intentionalClose;

    public static OnlineClient GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("Online Client");
        DontDestroyOnLoad(go);
        return go.AddComponent<OnlineClient>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        socket?.DispatchMessageQueue();
        matchSocket?.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        intentionalClose = true;
        if (socket != null) await socket.Close();
        if (matchSocket != null) await matchSocket.Close();
    }

    // ───────────────────────── Sign-in ─────────────────────────

    /// <summary>Get (or reuse) a guest token. The same token rejoins the same seat.</summary>
    public IEnumerator EnsureSignedIn(string name, Action<bool, string> done)
    {
        string token = PlayerPrefs.GetString("OnlineToken", "");
        if (!string.IsNullOrEmpty(token) && PlayerName == name)
        {
            done(true, null);
            yield break;
        }

        string body = JsonUtility.ToJson(new NameBody { name = name });
        using (var request = Post("/auth/guest", body, null))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                done(false, FriendlyError(request));
                yield break;
            }
            var auth = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            PlayerPrefs.SetString("OnlineToken", auth.token);
            PlayerPrefs.SetString("OnlineName", auth.name);
            PlayerPrefs.Save();
            done(true, null);
        }
    }

    private string Token => PlayerPrefs.GetString("OnlineToken", "");

    // ───────────────────────── Rooms ─────────────────────────

    public IEnumerator CreateRoom(string boardSize, Action<string, string> done)
    {
        string body = "{\"config\":{\"boardSize\":\"" + boardSize + "\",\"playerCount\":2,\"chaosMode\":false,\"timedMode\":false}}";
        using (var request = Post("/rooms", body, Token))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                done(null, FriendlyError(request));
                yield break;
            }
            var room = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);
            done(room.code, null);
        }
    }

    /// <summary>Open the room's WebSocket. room:joined arrives through OnMessage.</summary>
    public async void JoinRoom(string code)
    {
        await CloseRoom();
        RoomCode = code.Trim().ToUpperInvariant();
        intentionalClose = false;
        string url = ServerUrl.Replace("https://", "wss://").Replace("http://", "ws://") +
                     $"/rooms/{RoomCode}/connect?token={UnityWebRequest.EscapeURL(Token)}";

        socket = new WebSocket(url);
        socket.OnMessage += bytes => Handle(Encoding.UTF8.GetString(bytes));
        socket.OnError += error => Debug.LogWarning($"OnlineClient: socket error {error}");
        socket.OnClose += code2 =>
        {
            if (!intentionalClose) OnClosed?.Invoke(code2 == WebSocketCloseCode.Normal ? "Connection closed" : "Connection lost");
        };
        await socket.Connect();
    }

    public async System.Threading.Tasks.Task CloseRoom()
    {
        if (socket == null) return;
        intentionalClose = true;
        var old = socket;
        socket = null;
        pending.Clear();
        try { await old.Close(); } catch { /* already closed */ }
    }

    public async void LeaveOnline()
    {
        await CloseRoom();
        await CloseMatchmaking();
        RoomCode = null;
        OnlineSession.End();
    }

    // ───────────────────────── Find Match ─────────────────────────

    public async void FindMatch(string boardSize, Action<string> onFound, Action<string> onError)
    {
        await CloseMatchmaking();
        string url = ServerUrl.Replace("https://", "wss://").Replace("http://", "ws://") +
                     $"/matchmaking/connect?token={UnityWebRequest.EscapeURL(Token)}";
        matchSocket = new WebSocket(url);
        matchSocket.OnOpen += () =>
            matchSocket.SendText("{\"type\":\"matchmaking:join\",\"config\":{\"boardSize\":\"" + boardSize + "\",\"playerCount\":2}}");
        matchSocket.OnMessage += bytes =>
        {
            var msg = JsonUtility.FromJson<ServerMessage>(Encoding.UTF8.GetString(bytes));
            if (msg.type == "matchmaking:found") onFound(msg.roomCode);
        };
        matchSocket.OnError += error => onError("Couldn't reach the game server");
        await matchSocket.Connect();
    }

    public async System.Threading.Tasks.Task CloseMatchmaking()
    {
        if (matchSocket == null) return;
        var old = matchSocket;
        matchSocket = null;
        try { await old.Close(); } catch { /* already closed */ }
    }

    // ───────────────────────── Game messages ─────────────────────────

    public void SendPlacePiece(ChessPieceType type, BoardPosition position) =>
        Send("{\"type\":\"game:placePiece\",\"pieceType\":\"" + type + "\",\"position\":" + Pos(position) + "}");

    public void SendReady() => Send("{\"type\":\"game:ready\"}");

    public void SendMove(BoardPosition from, BoardPosition to) =>
        Send("{\"type\":\"game:move\",\"from\":" + Pos(from) + ",\"to\":" + Pos(to) + "}");

    public void SendPromotion(BoardPosition position, ChessPieceType type) =>
        Send("{\"type\":\"game:promotePawn\",\"position\":" + Pos(position) + ",\"pieceType\":\"" + type + "\"}");

    public void SendResign() => Send("{\"type\":\"game:resign\"}");

    /// <summary>Send a request; the server's ack (ok/error) goes to the callback if given</summary>
    private void Send(string json, Action<ServerMessage> onAck = null)
    {
        if (!IsConnected)
        {
            Debug.LogWarning($"OnlineClient: not connected, dropped {json}");
            return;
        }
        int id = nextRequestId++;
        if (onAck != null) pending[id] = onAck;
        socket.SendText(json.Insert(json.Length - 1, ",\"requestId\":" + id));
    }

    private void Handle(string text)
    {
        ServerMessage msg;
        try { msg = JsonUtility.FromJson<ServerMessage>(text); }
        catch (Exception e) { Debug.LogWarning($"OnlineClient: bad message {e.Message}"); return; }

        if (msg.type == "ack")
        {
            if (pending.TryGetValue(msg.requestId, out var callback))
            {
                pending.Remove(msg.requestId);
                callback(msg);
            }
            if (!msg.ok) Debug.LogWarning($"OnlineClient: request {msg.requestId} refused: {msg.error}");
            return;
        }

        if (msg.type == "room:joined")
        {
            RoomCode = msg.roomCode;
            IsHost = msg.isHost;
            if (Enum.TryParse(msg.yourColor, out PieceColor color)) LocalColor = color;
        }
        OnMessage?.Invoke(msg);
    }

    // ───────────────────────── Helpers ─────────────────────────

    private UnityWebRequest Post(string path, string json, string token)
    {
        var request = new UnityWebRequest(ServerUrl + path, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
        request.timeout = 15;
        return request;
    }

    private static string FriendlyError(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError) return "Couldn't reach the game server";
        if (request.responseCode == 401)
        {
            PlayerPrefs.DeleteKey("OnlineToken");
            return "Sign-in expired, try again";
        }
        return $"Server error ({request.responseCode})";
    }

    private static string Pos(BoardPosition p) => "{\"x\":" + p.x + ",\"y\":" + p.y + ",\"z\":" + p.z + "}";

    [Serializable] private class NameBody { public string name; }
    [Serializable] private class AuthResponse { public string token; public string userId; public string name; }
    [Serializable] private class CreateRoomResponse { public string code; }
}

/// <summary>Everything the server sends, flattened. Fields a message doesn't use stay empty.</summary>
[Serializable]
public class ServerMessage
{
    public string type;
    public int requestId;
    public bool ok;
    public string error;

    public string roomCode;
    public string yourColor;
    public bool isHost;
    public string color;
    public string name;
    public string phase;
    public string reason;
    public string winner;
    public string pieceType;
    public NetPos from;
    public NetPos to;
    public NetPos position;
    public NetState state;
}

[Serializable]
public class NetPos
{
    public int x, y, z;
    public BoardPosition ToBoard() => new BoardPosition(x, y, z);
}

[Serializable]
public class NetState
{
    public string phase;
    public string currentTurn;
    public int turnNumber;
    public NetConfig config;
    public NetPlayer[] players;
}

[Serializable]
public class NetConfig
{
    public string boardSize;
    public int playerCount;
}

[Serializable]
public class NetPlayer
{
    public string username;
    public string color;
    public bool isConnected;
}
