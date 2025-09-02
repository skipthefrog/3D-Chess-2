using UnityEngine;

[System.Serializable]
public class SceneSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool setupOnAwake = true;
    
    [Header("Components")]
    public Camera mainCamera;
    
    private void Awake()
    {
        if (setupOnAwake)
        {
            // Check if we came from the menu (SceneController exists) or loading directly
            bool cameFromMenu = SceneController.Instance != null;
            SetupScene(cameFromMenu);
        }
    }
    
    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        // Default context menu call - assume direct load for testing
        SetupScene(false);
    }
    
    /// <summary>
    /// Setup the scene with different behavior based on how it was loaded
    /// </summary>
    /// <param name="cameFromMenu">True if loaded via SceneController from menu, false if direct load</param>
    public void SetupScene(bool cameFromMenu)
    {
        if (cameFromMenu)
        {
            Debug.Log("Setting up 3D Chess scene (loaded from menu - minimal setup)...");
            
            // Only do essential setup when coming from menu
            SetupCamera();
            SetupGameManager();
            
            // Don't add tester components when coming from menu
            Debug.Log("3D Chess scene setup complete (menu mode)!");
        }
        else
        {
            Debug.Log("Setting up 3D Chess scene (direct load - full setup)...");
            
            // Full setup for direct scene loading (development/testing)
            SetupCamera();
            SetupGameManager();
            SetupTester();
            
            // For direct loading, also ensure we start in placement mode instead of waiting
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsWaitingForConfiguration())
            {
                Debug.Log("SceneSetup: Direct load detected - transitioning to PiecePlacement mode");
                GameStateManager.Instance.ChangeState(GameState.PiecePlacement);
            }
            
            Debug.Log("3D Chess scene setup complete (direct mode)!");
        }
    }
    
    private void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        if (mainCamera != null)
        {
            // Add camera controller if not present
            CameraController cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<CameraController>();
                Debug.Log("Added CameraController to main camera");
            }
            
            // Position camera for good initial view of the board
            mainCamera.transform.position = new Vector3(-5f, 5f, -5f);
            mainCamera.transform.LookAt(Vector3.zero);
        }
        else
        {
            Debug.LogWarning("No camera found in scene!");
        }
    }
    
    private void SetupGameManager()
    {
        // Check if GameManager already exists
        GameManager existingGameManager = FindFirstObjectByType<GameManager>();
        if (existingGameManager == null)
        {
            GameObject gameManagerObject = new GameObject("Game Manager");
            gameManagerObject.AddComponent<GameManager>();
            Debug.Log("Created GameManager");
        }
        else
        {
            Debug.Log("GameManager already exists");
        }
    }
    
    private void SetupTester()
    {
        // Add tester component to this object if not present
        ChessBoardTester tester = GetComponent<ChessBoardTester>();
        if (tester == null)
        {
            gameObject.AddComponent<ChessBoardTester>();
            Debug.Log("Added ChessBoardTester to scene");
        }
        
        // Add debug helper
        DebugHelper debugHelper = GetComponent<DebugHelper>();
        if (debugHelper == null)
        {
            gameObject.AddComponent<DebugHelper>();
            Debug.Log("Added DebugHelper to scene");
        }
    }
}