using UnityEngine;
using System;

/// <summary>
/// Comprehensive test for Socket.IO package installation and functionality
/// </summary>
public class SocketIOPackageTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private string serverUrl = "http://localhost:3000";
    [SerializeField] private bool runTestOnStart = true;
    
    [Header("Test Results")]
    [SerializeField] private bool packageFound = false;
    [SerializeField] private string packageType = "";
    [SerializeField] private bool connectionTested = false;
    [SerializeField] private bool serverReachable = false;
    
    private void Start()
    {
        if (runTestOnStart)
        {
            TestSocketIOPackage();
        }
    }
    
    [ContextMenu("Test Socket.IO Package")]
    public void TestSocketIOPackage()
    {
        Debug.Log("🧪 Starting comprehensive Socket.IO package test...");
        
        // Test 1: Package Detection
        TestPackageDetection();
        
        // Test 2: Component Creation
        if (packageFound)
        {
            TestComponentCreation();
        }
        
        // Test 3: Server Reachability
        StartCoroutine(TestServerReachability());
        
        // Final Results
        LogTestResults();
    }
    
    private void TestPackageDetection()
    {
        Debug.Log("🔍 Test 1: Package Detection...");
        
        // Check multiple possible Socket.IO types
        string[] typeNames = {
            "SocketIOUnity.SocketIOComponent",
            "SocketIOUnity.SocketIOUnity", 
            "SocketIOUnity.SocketIO",
            "BestHTTP.SocketIO.SocketManager",
            "SocketIOClient.SocketIO"
        };
        
        foreach (string typeName in typeNames)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                packageFound = true;
                packageType = typeName;
                Debug.Log($"✅ Found Socket.IO type: {typeName}");
                break;
            }
        }
        
        if (!packageFound)
        {
            Debug.LogWarning("⚠️ No Socket.IO types found");
            packageType = "None detected";
        }
    }
    
    private void TestComponentCreation()
    {
        Debug.Log("🔧 Test 2: Component Creation...");
        
        try
        {
            Type componentType = Type.GetType(packageType);
            if (componentType != null && typeof(MonoBehaviour).IsAssignableFrom(componentType))
            {
                // Create a test GameObject and add the component
                GameObject testObj = new GameObject("SocketIOTest");
                Component component = testObj.AddComponent(componentType);
                
                if (component != null)
                {
                    Debug.Log("✅ Component creation successful!");
                    
                    // Try to set basic properties
                    var urlField = componentType.GetField("url");
                    if (urlField != null)
                    {
                        urlField.SetValue(component, serverUrl);
                        Debug.Log($"✅ URL field set to: {serverUrl}");
                    }
                    
                    var autoConnectField = componentType.GetField("autoConnect");
                    if (autoConnectField != null)
                    {
                        autoConnectField.SetValue(component, false); // Don't auto-connect in test
                        Debug.Log("✅ AutoConnect field configured");
                    }
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
                Debug.LogWarning($"⚠️ Type {packageType} is not a MonoBehaviour component");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Component creation failed: {e.Message}");
        }
    }
    
    private System.Collections.IEnumerator TestServerReachability()
    {
        Debug.Log("🌐 Test 3: Server Reachability...");
        
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(serverUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                serverReachable = true;
                Debug.Log("✅ Server is reachable!");
                
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
                serverReachable = false;
                Debug.LogWarning($"⚠️ Server not reachable: {request.error}");
                Debug.LogWarning("💡 Make sure your Node.js server is running with 'npm start'");
            }
        }
        
        connectionTested = true;
        LogFinalResults();
    }
    
    private void LogTestResults()
    {
        Debug.Log("=== SOCKET.IO PACKAGE TEST RESULTS ===");
        Debug.Log($"Package Found: {(packageFound ? "✅ YES" : "❌ NO")}");
        Debug.Log($"Package Type: {packageType}");
        Debug.Log($"Server URL: {serverUrl}");
        Debug.Log("========================================");
    }
    
    private void LogFinalResults()
    {
        Debug.Log("=== FINAL TEST RESULTS ===");
        Debug.Log($"Package Found: {(packageFound ? "✅ YES" : "❌ NO")}");
        Debug.Log($"Package Type: {packageType}");
        Debug.Log($"Server Reachable: {(serverReachable ? "✅ YES" : "❌ NO")}");
        Debug.Log($"Server URL: {serverUrl}");
        
        if (packageFound && serverReachable)
        {
            Debug.Log("🎉 ALL TESTS PASSED! Socket.IO is ready for implementation.");
        }
        else if (packageFound && !serverReachable)
        {
            Debug.LogWarning("⚠️ Package found but server not running. Start server with 'npm start'");
        }
        else if (!packageFound)
        {
            Debug.LogError("❌ Socket.IO package not properly installed. Check installation.");
        }
        
        Debug.Log("==============================");
    }
    
    /// <summary>
    /// Manual trigger for testing from Inspector
    /// </summary>
    [ContextMenu("Quick Package Check")]
    public void QuickPackageCheck()
    {
        TestPackageDetection();
        Debug.Log($"Quick Check Result: Package = {(packageFound ? "Found" : "Not Found")}, Type = {packageType}");
    }
}