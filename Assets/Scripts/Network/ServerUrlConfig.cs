using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Helper component to configure server URL at runtime
/// for the legacy Socket.IO NetworkManager (online play now uses OnlineClient)
/// </summary>
public class ServerUrlConfig : MonoBehaviour
{
    [Header("Server URL Configuration")]
    [FormerlySerializedAs("serverUrl")]
    [SerializeField] private string serverUrl = "";
    [SerializeField] private bool setOnStart = true;
    [SerializeField] private bool saveToConfig = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;
    
    private void Start()
    {
        if (setOnStart && !string.IsNullOrEmpty(serverUrl))
        {
            SetServerUrl();
        }
    }
    
    /// <summary>
    /// Set the server URL on NetworkManager
    /// </summary>
    [ContextMenu("Set Server URL")]
    public void SetServerUrl()
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            Debug.LogWarning("⚠️ ServerUrlConfig: No URL specified");
            return;
        }
        
        var networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            Debug.LogError("❌ ServerUrlConfig: NetworkManager instance not found");
            return;
        }
        
        // Clean up the URL (ensure it starts with http/https)
        string cleanUrl = CleanUrl(serverUrl);
        
        if (enableDebugLogging)
            Debug.Log($"🌐 ServerUrlConfig: Setting server URL to {cleanUrl}");
        
        networkManager.SetProductionUrl(cleanUrl);
        
        if (saveToConfig)
        {
            networkManager.SaveServerUrlToConfig(cleanUrl);
        }
    }
    
    /// <summary>
    /// Clean and validate the URL format
    /// </summary>
    private string CleanUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        
        url = url.Trim();
        
        // Add protocol if missing
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }
        
        // Remove trailing slash
        if (url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }
        
        return url;
    }
    
    /// <summary>
    /// Set URL from external code
    /// </summary>
    public void SetUrl(string url)
    {
        serverUrl = url;
        SetServerUrl();
    }
    
    /// <summary>
    /// Reset to localhost
    /// </summary>
    [ContextMenu("Reset to Localhost")]
    public void ResetToLocalhost()
    {
        var networkManager = NetworkManager.Instance;
        if (networkManager != null)
        {
            networkManager.SetServerUrl("http://localhost:3000");
            
            if (enableDebugLogging)
                Debug.Log("🌐 ServerUrlConfig: Reset to localhost:3000");
        }
    }
    
    /// <summary>
    /// Test connection with current URL
    /// </summary>
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        var networkManager = NetworkManager.Instance;
        if (networkManager != null)
        {
            if (networkManager.IsConnected)
            {
                Debug.Log("✅ Already connected to server");
            }
            else
            {
                Debug.Log("🔌 Attempting to connect...");
                networkManager.ConnectToServer();
            }
        }
    }
}