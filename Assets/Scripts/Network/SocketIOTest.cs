using UnityEngine;

// Test different Socket.IO package possibilities
#if UNITY_EDITOR
using System;

// Try to detect which Socket.IO package is installed
#endif

/// <summary>
/// Test script to verify Socket.IO installation and functionality
/// </summary>
public class SocketIOTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private string serverUrl = "http://localhost:3000";
    [SerializeField] private bool autoTest = true;
    
    [Header("Test Results")]
    [SerializeField] private bool packageDetected = false;
    [SerializeField] private string packageInfo = "";
    [SerializeField] private bool connectionTested = false;
    
    private void Start()
    {
        Debug.Log("🧪 SocketIOTest: Starting Socket.IO package detection and testing");
        
        if (autoTest)
        {
            TestSocketIOInstallation();
        }
    }
    
    /// <summary>
    /// Test if Socket.IO package is properly installed
    /// </summary>
    [ContextMenu("Test Socket.IO Installation")]
    public void TestSocketIOInstallation()
    {
        Debug.Log("🧪 Testing Socket.IO package installation...");
        
        // Test 1: Check if SocketIOClient namespace is available
        TestNamespaceAvailability();
        
        // Test 2: Try to create a socket instance
        if (packageDetected)
        {
            TestSocketCreation();
        }
        
        // Test 3: Test connection (if server is running)
        if (packageDetected)
        {
            TestConnection();
        }
        
        // Log results
        LogTestResults();
    }
    
    private void TestNamespaceAvailability()
    {
        try
        {
            Debug.Log("🧪 Test 1: Checking namespace availability...");
            
            // Try to reference Socket.IO types
            #if SOCKETIO_UNITY_AVAILABLE
            packageInfo = "SocketIOUnity package detected";
            packageDetected = true;
            Debug.Log("✅ SocketIOUnity namespace is available");
            #else
            // Try alternative package detection
            var uriType = Type.GetType("System.Uri");
            if (uriType != null)
            {
                Debug.Log("🔍 System.Uri available, checking for Socket.IO client...");
                
                // Check for common Socket.IO Unity packages
                var socketTypes = new string[]
                {
                    "SocketIOClient.SocketIO",
                    "SocketIOUnity.SocketIOUnity", 
                    "BestHTTP.SocketIO.SocketManager",
                    "NativeWebSocket.WebSocket",
                    "SocketIOUnity.SocketIOComponent",
                    "SocketIOUnity.SocketIO"
                };
                
                foreach (var typeName in socketTypes)
                {
                    var type = Type.GetType(typeName);
                    if (type != null)
                    {
                        packageInfo = $"Socket.IO type found: {typeName}";
                        packageDetected = true;
                        Debug.Log($"✅ Socket.IO package detected: {typeName}");
                        break;
                    }
                }
            }
            #endif
            
            if (!packageDetected)
            {
                Debug.LogWarning("⚠️ Socket.IO package not detected in compile time");
                // Try runtime detection
                TestRuntimePackageDetection();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error testing namespace: {e.Message}");
        }
    }
    
    private void TestRuntimePackageDetection()
    {
        try
        {
            Debug.Log("🧪 Attempting runtime package detection...");
            
            // Look for Socket.IO related assemblies
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.FullName;
                
                if (assemblyName.Contains("SocketIO") || 
                    assemblyName.Contains("BestHTTP") ||
                    assemblyName.Contains("NativeWebSocket"))
                {
                    packageInfo = $"Socket.IO assembly found: {assemblyName}";
                    packageDetected = true;
                    Debug.Log($"✅ Socket.IO assembly detected: {assemblyName}");
                    break;
                }
            }
            
            if (!packageDetected)
            {
                packageInfo = "No Socket.IO package detected";
                Debug.LogWarning("⚠️ No Socket.IO assemblies found");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error in runtime detection: {e.Message}");
        }
    }
    
    private void TestSocketCreation()
    {
        try
        {
            Debug.Log("🧪 Test 2: Testing socket creation...");
            
            // Try to create a SocketIOUnity instance
            var socketIOType = Type.GetType("SocketIOUnity.SocketIOComponent");
            if (socketIOType != null)
            {
                Debug.Log("✅ SocketIOComponent type found, attempting creation...");
                
                // Create a temporary GameObject to test component attachment
                GameObject testObj = new GameObject("SocketIOTest");
                var component = testObj.AddComponent(socketIOType);
                
                if (component != null)
                {
                    Debug.Log("✅ SocketIOComponent successfully created!");
                    packageInfo += " - Component creation successful";
                }
                else
                {
                    Debug.LogWarning("⚠️ Component creation returned null");
                }
                
                // Clean up test object
                DestroyImmediate(testObj);
            }
            else
            {
                Debug.Log("💡 SocketIOComponent type not found, trying alternative approach...");
                
                // Try direct SocketIO creation
                var socketType = Type.GetType("SocketIOUnity.SocketIO");
                if (socketType != null)
                {
                    Debug.Log("✅ SocketIO type found!");
                    packageInfo += " - Direct type access successful";
                }
            }
            
            Debug.Log("✅ Socket creation test completed");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error creating socket: {e.Message}");
        }
    }
    
    private void TestConnection()
    {
        try
        {
            Debug.Log("🧪 Test 3: Testing connection...");
            
            // This will be implemented with actual Socket.IO connection
            Debug.Log("💡 Connection test - would attempt connection to server");
            
            // For now, just test if server is reachable via HTTP
            StartCoroutine(TestServerReachability());
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error testing connection: {e.Message}");
        }
    }
    
    private System.Collections.IEnumerator TestServerReachability()
    {
        Debug.Log($"🌐 Testing server reachability at {serverUrl}");
        
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(serverUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Server is reachable!");
                connectionTested = true;
                
                // Try to parse server response
                try
                {
                    string response = request.downloadHandler.text;
                    Debug.Log($"📡 Server response: {response}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Could not parse server response: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Server not reachable: {request.error}");
                Debug.LogWarning("💡 Make sure your Node.js server is running with 'npm run dev'");
            }
        }
    }
    
    private void LogTestResults()
    {
        Debug.Log("=== SOCKET.IO TEST RESULTS ===");
        Debug.Log($"Package Detected: {(packageDetected ? "✅ YES" : "❌ NO")}");
        Debug.Log($"Package Info: {packageInfo}");
        Debug.Log($"Server Reachable: {(connectionTested ? "✅ YES" : "❌ NO")}");
        Debug.Log($"Server URL: {serverUrl}");
        Debug.Log("===============================");
        
        if (!packageDetected)
        {
            Debug.LogError("❌ SOCKET.IO PACKAGE NOT DETECTED!");
            Debug.LogError("💡 Please check your package installation:");
            Debug.LogError("   1. Make sure you installed the package correctly");
            Debug.LogError("   2. Try restarting Unity");
            Debug.LogError("   3. Check the Console for import errors");
            Debug.LogError("   4. Verify files are in Assets/SocketIOUnity/");
        }
        else
        {
            Debug.Log("✅ SOCKET.IO PACKAGE SUCCESSFULLY DETECTED!");
            Debug.Log("💡 Ready to implement real networking functionality");
        }
    }
    
    /// <summary>
    /// Manual test method accessible from Inspector
    /// </summary>
    [ContextMenu("Run Full Test Suite")]
    public void RunFullTestSuite()
    {
        Debug.Log("🧪 Running full Socket.IO test suite...");
        TestSocketIOInstallation();
    }
    
    /// <summary>
    /// Test specific package type
    /// </summary>
    [ContextMenu("Test SocketIOUnity Package")]
    public void TestSocketIOUnityPackage()
    {
        Debug.Log("🧪 Testing specific SocketIOUnity package...");
        
        try
        {
            // Try to access SocketIOUnity specific types
            var type = System.Type.GetType("SocketIOUnity.SocketIOUnity, SocketIOUnity");
            if (type != null)
            {
                Debug.Log("✅ SocketIOUnity package is properly installed!");
                packageDetected = true;
                packageInfo = "SocketIOUnity package confirmed";
            }
            else
            {
                Debug.LogWarning("⚠️ SocketIOUnity package not found");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error testing SocketIOUnity: {e.Message}");
        }
    }
    
    /// <summary>
    /// List all available assemblies for debugging
    /// </summary>
    [ContextMenu("List All Assemblies")]
    public void ListAllAssemblies()
    {
        Debug.Log("🔍 Listing all loaded assemblies:");
        
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            Debug.Log($"📦 Assembly: {assembly.GetName().Name}");
        }
        
        Debug.Log($"📊 Total assemblies: {assemblies.Length}");
    }
    
    private void OnGUI()
    {
        // Simple GUI for quick testing
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Socket.IO Test Panel", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        
        GUILayout.Space(10);
        
        GUILayout.Label($"Package: {(packageDetected ? "✅ Detected" : "❌ Not Found")}");
        GUILayout.Label($"Server: {(connectionTested ? "✅ Reachable" : "❌ Not Tested")}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run Test"))
        {
            TestSocketIOInstallation();
        }
        
        if (GUILayout.Button("Test Server"))
        {
            StartCoroutine(TestServerReachability());
        }
        
        GUILayout.EndArea();
    }
}