/**
 * Network Test UI
 * Simple UI for testing Socket.IO connection and network features
 * Attach to a GameObject with NetworkManager in the scene
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NetworkTestUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_InputField serverUrlInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private ScrollRect logScrollRect;

    private NetworkManager networkManager;
    private List<string> logMessages = new List<string>();
    private const int MAX_LOG_LINES = 50;

    private void Start()
    {
        // Get NetworkManager instance
        networkManager = NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogError("❌ NetworkManager not found! Make sure it exists in the scene.");
            AddLog("ERROR: NetworkManager not found!", Color.red);
            return;
        }

        // Set default values
        if (playerNameInput != null)
            playerNameInput.text = "TestPlayer_" + Random.Range(1000, 9999);

        if (serverUrlInput != null)
            serverUrlInput.text = "http://localhost:3000";

        // Setup button listeners
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        // Subscribe to NetworkManager events
        SubscribeToNetworkEvents();

        // Initial UI update
        UpdateUI();
        AddLog("🎮 Network Test UI Initialized", Color.green);
        AddLog("📡 Server: http://localhost:3000", Color.cyan);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (networkManager != null)
        {
            UnsubscribeFromNetworkEvents();
        }

        // Remove button listeners
        if (connectButton != null)
            connectButton.onClick.RemoveAllListeners();

        if (disconnectButton != null)
            disconnectButton.onClick.RemoveAllListeners();

        if (createRoomButton != null)
            createRoomButton.onClick.RemoveAllListeners();

        if (joinRoomButton != null)
            joinRoomButton.onClick.RemoveAllListeners();
    }

    private void SubscribeToNetworkEvents()
    {
        if (networkManager == null) return;

        networkManager.OnConnectionStateChanged += OnConnectionStateChanged;
        networkManager.OnConnectionError += OnConnectionError;
        networkManager.OnRoomCreated += OnRoomCreated;
        networkManager.OnRoomJoined += OnRoomJoined;
        networkManager.OnRoomError += OnRoomError;
        networkManager.OnPlayerJoined += OnPlayerJoined;
        networkManager.OnPlayerLeft += OnPlayerLeft;
        networkManager.OnMatchFound += OnMatchFound;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (networkManager == null) return;

        networkManager.OnConnectionStateChanged -= OnConnectionStateChanged;
        networkManager.OnConnectionError -= OnConnectionError;
        networkManager.OnRoomCreated -= OnRoomCreated;
        networkManager.OnRoomJoined -= OnRoomJoined;
        networkManager.OnRoomError -= OnRoomError;
        networkManager.OnPlayerJoined -= OnPlayerJoined;
        networkManager.OnPlayerLeft -= OnPlayerLeft;
        networkManager.OnMatchFound -= OnMatchFound;
    }

    // Button Handlers
    private void OnConnectClicked()
    {
        if (networkManager == null) return;

        string url = serverUrlInput?.text ?? "http://localhost:3000";
        AddLog($"🔌 Connecting to {url}...", Color.yellow);

        networkManager.ConnectToServer();
    }

    private void OnDisconnectClicked()
    {
        if (networkManager == null) return;

        AddLog("🔌 Disconnecting...", Color.yellow);
        networkManager.DisconnectFromServer();
    }

    private void OnCreateRoomClicked()
    {
        if (networkManager == null || !networkManager.IsConnected)
        {
            AddLog("⚠️ Not connected to server!", Color.red);
            return;
        }

        string playerName = playerNameInput?.text ?? "TestPlayer";
        AddLog($"🏠 Creating room as {playerName}...", Color.yellow);

        networkManager.CreateRoom(playerName);
    }

    private void OnJoinRoomClicked()
    {
        if (networkManager == null || !networkManager.IsConnected)
        {
            AddLog("⚠️ Not connected to server!", Color.red);
            return;
        }

        string roomCode = roomCodeInput?.text?.ToUpper() ?? "";
        if (string.IsNullOrEmpty(roomCode))
        {
            AddLog("⚠️ Please enter a room code!", Color.red);
            return;
        }

        string playerName = playerNameInput?.text ?? "TestPlayer";
        AddLog($"🚪 Joining room {roomCode} as {playerName}...", Color.yellow);

        networkManager.JoinRoom(roomCode, playerName);
    }

    // Network Event Handlers
    private void OnConnectionStateChanged(bool isConnected)
    {
        if (isConnected)
        {
            AddLog("✅ Connected to server!", Color.green);
        }
        else
        {
            AddLog("❌ Disconnected from server", Color.red);
        }
        UpdateUI();
    }

    private void OnConnectionError(string error)
    {
        AddLog($"❌ Connection Error: {error}", Color.red);
        UpdateUI();
    }

    private void OnRoomCreated(string roomCode, PieceColor assignedColor)
    {
        AddLog($"✅ Room Created! Code: {roomCode}", Color.green);
        AddLog($"🎨 Assigned Color: {assignedColor}", Color.cyan);

        if (roomCodeInput != null)
            roomCodeInput.text = roomCode;

        UpdateUI();
    }

    private void OnRoomJoined(string roomCode, PieceColor assignedColor)
    {
        AddLog($"✅ Joined Room: {roomCode}", Color.green);
        AddLog($"🎨 Assigned Color: {assignedColor}", Color.cyan);
        UpdateUI();
    }

    private void OnRoomError(string error)
    {
        AddLog($"❌ Room Error: {error}", Color.red);
    }

    private void OnPlayerJoined(ChessNetwork.NetworkPlayerInfo playerInfo)
    {
        AddLog($"👤 Player Joined: {playerInfo.playerName} ({playerInfo.assignedColor})", Color.cyan);
    }

    private void OnPlayerLeft(ChessNetwork.NetworkPlayerInfo playerInfo)
    {
        AddLog($"👋 Player Left: {playerInfo.playerName}", Color.yellow);
    }

    private void OnMatchFound(string roomCode, PieceColor assignedColor)
    {
        AddLog($"🎯 Match Found! Room: {roomCode}", Color.green);
        AddLog($"🎨 Playing as: {assignedColor}", Color.cyan);

        if (roomCodeInput != null)
            roomCodeInput.text = roomCode;

        UpdateUI();
    }

    // UI Update Methods
    private void UpdateUI()
    {
        if (networkManager == null) return;

        // Update status text
        if (statusText != null)
        {
            string status = networkManager.IsConnected ? "🟢 CONNECTED" : "🔴 DISCONNECTED";
            string room = !string.IsNullOrEmpty(networkManager.RoomCode)
                ? $"\nRoom: {networkManager.RoomCode}"
                : "";
            string color = networkManager.IsConnected && !string.IsNullOrEmpty(networkManager.RoomCode)
                ? $"\nColor: {networkManager.AssignedColor}"
                : "";

            statusText.text = $"{status}{room}{color}";
            statusText.color = networkManager.IsConnected ? Color.green : Color.red;
        }

        // Update button interactivity
        if (connectButton != null)
            connectButton.interactable = !networkManager.IsConnected;

        if (disconnectButton != null)
            disconnectButton.interactable = networkManager.IsConnected;

        if (createRoomButton != null)
            createRoomButton.interactable = networkManager.IsConnected && string.IsNullOrEmpty(networkManager.RoomCode);

        if (joinRoomButton != null)
            joinRoomButton.interactable = networkManager.IsConnected && string.IsNullOrEmpty(networkManager.RoomCode);
    }

    private void AddLog(string message, Color color)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string formattedMessage = $"<color=#{colorHex}>[{timestamp}] {message}</color>";

        logMessages.Add(formattedMessage);

        // Keep only last MAX_LOG_LINES messages
        if (logMessages.Count > MAX_LOG_LINES)
        {
            logMessages.RemoveAt(0);
        }

        // Update log text
        if (logText != null)
        {
            logText.text = string.Join("\n", logMessages);

            // Auto-scroll to bottom
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        Debug.Log(message);
    }

    // Update status periodically
    private void Update()
    {
        // Update status every second
        if (Time.frameCount % 60 == 0)
        {
            UpdateUI();
        }
    }
}
