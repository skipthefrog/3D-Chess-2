using UnityEngine;

/// <summary>
/// Auto-setup script to ensure NetworkManager and ServerUrlConfig are properly initialized
/// Add this to a GameObject in your scene to auto-configure networking
/// </summary>
public class NetworkSetup : MonoBehaviour
{
    [Header("Auto Setup Configuration")]
    [SerializeField] private bool setupOnAwake = true;
    [SerializeField] private bool createNetworkManagerIfMissing = true;
    [SerializeField] private bool createServerConfigIfMissing = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = true;
    
    private void Awake()
    {
        if (setupOnAwake)
        {
            SetupNetworking();
        }
    }
    
    /// <summary>
    /// Setup networking components automatically
    /// </summary>
    [ContextMenu("Setup Networking")]
    public void SetupNetworking()
    {
        if (enableDebugLogging)
            Debug.Log("🔧 NetworkSetup: Starting automatic network setup...");
        
        // Setup NetworkManager
        SetupNetworkManager();
        
        // Setup ServerUrlConfig
        SetupServerUrlConfig();
        
        if (enableDebugLogging)
            Debug.Log("✅ NetworkSetup: Network setup completed!");
    }
    
    private void SetupNetworkManager()
    {
        var networkManager = NetworkManager.Instance;
        
        if (networkManager == null && createNetworkManagerIfMissing)
        {
            // Create NetworkManager GameObject
            GameObject networkManagerObj = new GameObject("NetworkManager");
            networkManager = networkManagerObj.AddComponent<NetworkManager>();
            DontDestroyOnLoad(networkManagerObj);
            
            if (enableDebugLogging)
                Debug.Log("🌐 NetworkSetup: Created NetworkManager instance");
        }
        
        // No production URL is set here: the old tunnel address is gone, and online play
        // now goes through OnlineClient and the Cloudflare server
    }
    
    private void SetupServerUrlConfig()
    {
        var serverConfig = FindFirstObjectByType<ServerUrlConfig>();
        
        if (serverConfig == null && createServerConfigIfMissing)
        {
            // Create ServerUrlConfig GameObject
            GameObject serverConfigObj = new GameObject("ServerUrlConfig");
            serverConfig = serverConfigObj.AddComponent<ServerUrlConfig>();
            
            if (enableDebugLogging)
                Debug.Log("🔧 NetworkSetup: Created ServerUrlConfig instance");
        }
        
        // ServerUrlConfig applies its own configured URL (if any) on Start
    }
    
    /// <summary>
    /// Test connection after setup
    /// </summary>
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        var networkManager = NetworkManager.Instance;
        if (networkManager != null)
        {
            if (enableDebugLogging)
                Debug.Log("🔌 NetworkSetup: Testing connection to server...");
            
            networkManager.ConnectToServer();
        }
        else
        {
            Debug.LogError("❌ NetworkSetup: NetworkManager not found!");
        }
    }
    
    /// <summary>
    /// Show current network status
    /// </summary>
    [ContextMenu("Show Network Status")]
    public void ShowNetworkStatus()
    {
        var networkManager = NetworkManager.Instance;
        if (networkManager != null)
        {
            networkManager.LogNetworkState();
        }
        else
        {
            Debug.LogWarning("⚠️ NetworkSetup: NetworkManager not found");
        }
    }
}