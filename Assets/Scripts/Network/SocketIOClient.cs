/**
 * Native Socket.IO Client for Unity
 * Cross-platform Socket.IO implementation using Unity's WebSocket support
 * Works on: Windows, Mac, Linux, iOS, Android, WebGL
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ChessNetwork
{
    /// <summary>
    /// Socket.IO client implementation using Unity's native networking
    /// </summary>
    public class SocketIOClient
    {
        // Connection state
        public bool IsConnected { get; private set; }
        public string SocketId { get; private set; }

        private string serverUrl;
        private string authToken;
        private MonoBehaviour coroutineRunner;

        // Event handlers
        private Dictionary<string, List<Action<string>>> eventHandlers = new Dictionary<string, List<Action<string>>>();
        private Action onConnected;
        private Action<string> onDisconnected;
        private Action<string> onError;

        // Polling for communication (Socket.IO uses polling initially, then upgrades to WebSocket)
        private bool isPolling = false;
        private Coroutine pollingCoroutine;
        private float pingInterval = 25f; // Socket.IO default
        private float lastPingTime;

        // Session data
        private string sessionId;

        public SocketIOClient(MonoBehaviour runner)
        {
            coroutineRunner = runner;
        }

        /// <summary>
        /// Connect to Socket.IO server
        /// </summary>
        public void Connect(string url, string token = "UNITY", Action onSuccess = null, Action<string> onFail = null)
        {
            serverUrl = url.TrimEnd('/');
            authToken = token;
            onConnected = onSuccess;
            onError = onFail;

            Debug.Log($"🔌 SocketIOClient: Connecting to {serverUrl}...");

            // Start connection process
            coroutineRunner.StartCoroutine(ConnectCoroutine());
        }

        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect()
        {
            if (!IsConnected) return;

            Debug.Log("🔌 SocketIOClient: Disconnecting...");

            IsConnected = false;
            isPolling = false;

            if (pollingCoroutine != null)
            {
                coroutineRunner.StopCoroutine(pollingCoroutine);
                pollingCoroutine = null;
            }

            onDisconnected?.Invoke("client_disconnect");
        }

        /// <summary>
        /// Register event handler
        /// </summary>
        public void On(string eventName, Action<string> callback)
        {
            if (!eventHandlers.ContainsKey(eventName))
            {
                eventHandlers[eventName] = new List<Action<string>>();
            }

            eventHandlers[eventName].Add(callback);
        }

        /// <summary>
        /// Remove event handler
        /// </summary>
        public void Off(string eventName, Action<string> callback = null)
        {
            if (!eventHandlers.ContainsKey(eventName)) return;

            if (callback == null)
            {
                eventHandlers[eventName].Clear();
            }
            else
            {
                eventHandlers[eventName].Remove(callback);
            }
        }

        /// <summary>
        /// Emit event to server
        /// </summary>
        public void Emit(string eventName, string data = "{}", Action<string> callback = null)
        {
            if (!IsConnected)
            {
                Debug.LogWarning($"⚠️ SocketIOClient: Cannot emit '{eventName}' - not connected");
                callback?.Invoke("{\"success\":false,\"error\":\"Not connected\"}");
                return;
            }

            coroutineRunner.StartCoroutine(EmitCoroutine(eventName, data, callback));
        }

        /// <summary>
        /// Emit event with JSON object
        /// </summary>
        public void EmitJson(string eventName, object dataObject, Action<string> callback = null)
        {
            string json = JsonUtility.ToJson(dataObject);
            Emit(eventName, json, callback);
        }

        // Connection coroutine
        private IEnumerator ConnectCoroutine()
        {
            // Build Socket.IO handshake URL
            string handshakeUrl = $"{serverUrl}/socket.io/?EIO=4&transport=polling&token={authToken}";

            Debug.Log($"🔌 Socket.IO Handshake: {handshakeUrl}");

            using (UnityWebRequest request = UnityWebRequest.Get(handshakeUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"❌ Socket.IO Connection failed: {request.error}");
                    onError?.Invoke(request.error);
                    yield break;
                }

                // Parse handshake response
                string response = request.downloadHandler.text;
                Debug.Log($"📨 Socket.IO Handshake response: {response}");

                // Socket.IO response format: "0{...json...}"
                if (response.StartsWith("0"))
                {
                    string jsonData = response.Substring(1);

                    try
                    {
                        // Parse the handshake data
                        HandshakeData handshake = JsonUtility.FromJson<HandshakeData>(jsonData);
                        sessionId = handshake.sid;
                        SocketId = handshake.sid;
                        pingInterval = handshake.pingInterval / 1000f; // Convert to seconds

                        Debug.Log($"✅ Socket.IO Connected! Session ID: {sessionId}");
                        Debug.Log($"⏱️ Ping interval: {pingInterval}s");

                        IsConnected = true;
                        lastPingTime = Time.time;

                        // Start polling for messages
                        isPolling = true;
                        pollingCoroutine = coroutineRunner.StartCoroutine(PollingCoroutine());

                        // Trigger connected callback
                        onConnected?.Invoke();
                        TriggerEvent("connect", "{}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Failed to parse handshake: {e.Message}");
                        onError?.Invoke($"Handshake parse error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Invalid handshake response: {response}");
                    onError?.Invoke("Invalid handshake response");
                }
            }
        }

        // Polling coroutine for receiving messages
        private IEnumerator PollingCoroutine()
        {
            while (isPolling && IsConnected)
            {
                // Check if we need to send a ping
                if (Time.time - lastPingTime >= pingInterval)
                {
                    yield return SendPing();
                    lastPingTime = Time.time;
                }

                // Poll for messages
                string pollUrl = $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

                using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
                {
                    #if UNITY_WEBGL && !UNITY_EDITOR
                    request.timeout = 30; // WebGL: Shorter timeout to avoid browser issues
                    #else
                    request.timeout = (int)pingInterval + 5; // Timeout slightly longer than ping interval
                    #endif
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;

                        if (!string.IsNullOrEmpty(response))
                        {
                            ProcessMessages(response);
                        }
                    }
                    else if (request.result != UnityWebRequest.Result.InProgress)
                    {
                        Debug.LogWarning($"⚠️ Polling error: {request.error}");

                        // Connection lost
                        if (request.error.Contains("timeout") || request.error.Contains("Cannot connect"))
                        {
                            Debug.LogError("❌ Socket.IO Connection lost");
                            IsConnected = false;
                            isPolling = false;
                            onDisconnected?.Invoke("connection_lost");
                            yield break;
                        }
                    }
                }

                // Small delay between polls
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Send ping to keep connection alive
        private IEnumerator SendPing()
        {
            string pingUrl = $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

            // Socket.IO ping message format: "2" (just the packet type)
            byte[] bodyRaw = Encoding.UTF8.GetBytes("2");

            using (UnityWebRequest request = new UnityWebRequest(pingUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "text/plain");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("🏓 Ping sent successfully");
                }
            }
        }

        // Emit coroutine
        private IEnumerator EmitCoroutine(string eventName, string data, Action<string> callback)
        {
            string emitUrl = $"{serverUrl}/socket.io/?EIO=4&transport=polling&sid={sessionId}";

            // Socket.IO message format: 42["eventName",{data}]
            string message = $"42[\"{eventName}\",{data}]";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(message);

            using (UnityWebRequest request = new UnityWebRequest(emitUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "text/plain");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"📤 Emitted '{eventName}' successfully");

                    // Parse response if there is one
                    string response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response))
                    {
                        callback?.Invoke(response);
                    }
                    else
                    {
                        callback?.Invoke("{\"success\":true}");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Failed to emit '{eventName}': {request.error}");
                    callback?.Invoke($"{{\"success\":false,\"error\":\"{request.error}\"}}");
                }
            }
        }

        // Process received messages
        private void ProcessMessages(string messages)
        {
            // Socket.IO can send multiple messages in one response
            // Format: 42["event",{data}] or 3 (pong) or others

            if (messages.StartsWith("3"))
            {
                // Pong response
                Debug.Log("🏓 Pong received");
                return;
            }

            if (messages.StartsWith("42"))
            {
                // Event message: 42["eventName",{data}]
                try
                {
                    string content = messages.Substring(2); // Remove "42"

                    // Parse the array format
                    if (content.StartsWith("["))
                    {
                        content = content.Substring(1, content.Length - 2); // Remove [ and ]

                        // Find the event name
                        int firstQuote = content.IndexOf('"');
                        int secondQuote = content.IndexOf('"', firstQuote + 1);

                        if (firstQuote >= 0 && secondQuote > firstQuote)
                        {
                            string eventName = content.Substring(firstQuote + 1, secondQuote - firstQuote - 1);

                            // Find the data (after the comma)
                            int commaPos = content.IndexOf(',', secondQuote);
                            string eventData = commaPos > 0 ? content.Substring(commaPos + 1).Trim() : "{}";

                            Debug.Log($"📨 Received event: {eventName}");
                            TriggerEvent(eventName, eventData);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Failed to parse message: {e.Message}\nMessage: {messages}");
                }
            }
            else if (messages.StartsWith("40"))
            {
                // Connection acknowledgment
                Debug.Log("✅ Socket.IO connection acknowledged");
            }
        }

        // Trigger event handlers
        private void TriggerEvent(string eventName, string data)
        {
            if (eventHandlers.ContainsKey(eventName))
            {
                foreach (var handler in eventHandlers[eventName])
                {
                    try
                    {
                        handler?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Error in event handler for '{eventName}': {e.Message}");
                    }
                }
            }
        }

        // Data classes
        [Serializable]
        private class HandshakeData
        {
            public string sid;
            public string[] upgrades;
            public int pingInterval;
            public int pingTimeout;
            public int maxPayload;
        }
    }
}
