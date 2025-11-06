using UnityEngine;

/// <summary>
/// Simple test runner to execute Socket.IO tests automatically
/// </summary>
public class SocketIOTestRunner : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private float delayBeforeTest = 1f;
    
    private SocketIOTest socketTest;
    
    private void Start()
    {
        if (runOnStart)
        {
            // Create the test component
            socketTest = gameObject.AddComponent<SocketIOTest>();
            
            // Run test after a short delay
            Invoke(nameof(RunTest), delayBeforeTest);
        }
    }
    
    private void RunTest()
    {
        if (socketTest != null)
        {
            Debug.Log("🎬 SocketIOTestRunner: Starting Socket.IO package verification...");
            socketTest.TestSocketIOInstallation();
        }
        else
        {
            Debug.LogError("❌ SocketIOTestRunner: Could not create SocketIOTest component");
        }
    }
    
    [ContextMenu("Run Socket.IO Test")]
    public void ManualTest()
    {
        if (socketTest == null)
        {
            socketTest = gameObject.AddComponent<SocketIOTest>();
        }
        
        RunTest();
    }
}