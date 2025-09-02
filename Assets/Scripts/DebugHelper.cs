using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool showDebugInfo = false;
    public KeyCode toggleKey = KeyCode.F1;
    
    private void Start()
    {
        Debug.Log("=== DEBUG HELPER STARTED ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        
        // Check if our key GameObjects exist
        CheckGameObjects();
    }
    
    private void Update()
    {
        if (InputHelper.GetKeyDown(toggleKey))
        {
            CheckGameObjects();
            CheckCameraPosition();
            CheckBoardStatus();
        }
    }
    
    private void CheckGameObjects()
    {
        Debug.Log("=== GAMEOBJECT CHECK ===");
        
        // Check for our key components
        SceneSetup sceneSetup = FindFirstObjectByType<SceneSetup>();
        Debug.Log($"SceneSetup found: {sceneSetup != null}");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        Debug.Log($"GameManager found: {gameManager != null}");
        
        ChessBoard chessBoard = FindFirstObjectByType<ChessBoard>();
        Debug.Log($"ChessBoard found: {chessBoard != null}");
        Debug.Log($"ChessBoard.Instance: {ChessBoard.Instance != null}");
        
        InputManager inputManager = FindFirstObjectByType<InputManager>();
        Debug.Log($"InputManager found: {inputManager != null}");
        
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        Debug.Log($"CameraController found: {cameraController != null}");
        
        // Count all GameObjects in the scene
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log($"Total GameObjects in scene: {allObjects.Length}");
        
        // List root GameObjects
        Debug.Log("=== ROOT GAMEOBJECTS ===");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount; i++)
        {
            GameObject rootObj = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[i];
            Debug.Log($"Root GameObject: {rootObj.name}");
        }
    }
    
    private void CheckCameraPosition()
    {
        Debug.Log("=== CAMERA CHECK ===");
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"Camera position: {mainCamera.transform.position}");
            Debug.Log($"Camera rotation: {mainCamera.transform.rotation.eulerAngles}");
            Debug.Log($"Camera forward: {mainCamera.transform.forward}");
            
            // Show what the camera is looking at
            Vector3 lookAtPoint = mainCamera.transform.position + mainCamera.transform.forward * 10f;
            Debug.Log($"Camera looking toward: {lookAtPoint}");
        }
        else
        {
            Debug.LogError("No main camera found!");
        }
    }
    
    private void CheckBoardStatus()
    {
        Debug.Log("=== BOARD STATUS CHECK ===");
        
        if (ChessBoard.Instance != null)
        {
            Debug.Log("ChessBoard Instance exists!");
            
            // Try to find board cells
            GameObject[] boardCells = GameObject.FindGameObjectsWithTag("Untagged");
            int cellCount = 0;
            foreach (GameObject obj in boardCells)
            {
                if (obj.name.StartsWith("Cell_"))
                {
                    cellCount++;
                }
            }
            Debug.Log($"Found {cellCount} board cells in scene");
            
            // Check if board container exists
            GameObject boardContainer = GameObject.Find("Board Container");
            if (boardContainer != null)
            {
                Debug.Log($"Board Container found at: {boardContainer.transform.position}");
                Debug.Log($"Board Container children: {boardContainer.transform.childCount}");
            }
            else
            {
                Debug.LogWarning("Board Container not found!");
            }
        }
        else
        {
            Debug.LogError("ChessBoard.Instance is null!");
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUI.Box(new Rect(10, 10, 300, 120), "Debug Info (Press F1 to refresh)");
        
        int yPos = 30;
        GUI.Label(new Rect(20, yPos, 280, 20), $"ChessBoard: {(ChessBoard.Instance != null ? "✓" : "✗")}");
        yPos += 20;
        GUI.Label(new Rect(20, yPos, 280, 20), $"GameManager: {(FindFirstObjectByType<GameManager>() != null ? "✓" : "✗")}");
        yPos += 20;
        GUI.Label(new Rect(20, yPos, 280, 20), $"InputManager: {(FindFirstObjectByType<InputManager>() != null ? "✓" : "✗")}");
        yPos += 20;
        GUI.Label(new Rect(20, yPos, 280, 20), $"Camera: {(Camera.main != null ? "✓" : "✗")}");
        yPos += 20;
        GUI.Label(new Rect(20, yPos, 280, 20), $"Total Objects: {FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length}");
    }
}