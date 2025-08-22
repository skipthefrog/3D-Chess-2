using UnityEngine;

/// <summary>
/// Main UI coordinator for the 3D Chess game
/// Manages and initializes all UI components including check indicators
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    public bool enableCheckIndicator = false; // Disabled - using persistent status instead
    public bool enablePersistentCheckStatus = true;
    public bool enableBoardRotationUI = true;
    public bool enablePlacementUI = true;
    public bool enableReturnToMenuButton = true;
    
    // UI Component references
    private CheckIndicatorUI checkIndicator;
    private CheckStatusUI checkStatus;
    private PersistentCheckStatusUI persistentCheckStatus;
    private BoardRotationUI boardRotation;
    private PlacementUI placementUI;
    
    public static UIManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("UIManager: Instance created");
            
            // Initialize UI components
            InitializeUIComponents();
        }
        else
        {
            Debug.LogWarning("UIManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Ensure all UI components are properly set up
        ValidateUIComponents();
    }
    
    /// <summary>
    /// Initialize all UI components
    /// </summary>
    private void InitializeUIComponents()
    {
        Debug.Log("UIManager: Initializing UI components");
        
        // Create Check Indicator UI if enabled
        if (enableCheckIndicator)
        {
            CreateCheckIndicatorUI();
        }
        
        // Create Persistent Check Status UI if enabled
        if (enablePersistentCheckStatus)
        {
            CreatePersistentCheckStatusUI();
        }
        
        // Find existing UI components
        FindExistingUIComponents();
        
        // Create Return to Menu button if enabled
        if (enableReturnToMenuButton)
        {
            CreateReturnToMenuButton();
        }
        
        Debug.Log("UIManager: UI components initialization complete");
    }
    
    /// <summary>
    /// Create the new dedicated check indicator
    /// </summary>
    private void CreateCheckIndicatorUI()
    {
        Debug.Log("UIManager.CreateCheckIndicatorUI: ENTRY");
        
        GameObject checkIndicatorObject = new GameObject("CheckIndicatorUI");
        checkIndicatorObject.transform.SetParent(transform);
        
        checkIndicator = checkIndicatorObject.AddComponent<CheckIndicatorUI>();
        
        Debug.Log($"UIManager.CreateCheckIndicatorUI: Created CheckIndicatorUI component on GameObject '{checkIndicatorObject.name}'");
        Debug.Log($"UIManager.CreateCheckIndicatorUI: CheckIndicatorUI.Instance = {(CheckIndicatorUI.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"UIManager.CreateCheckIndicatorUI: checkIndicator reference = {(checkIndicator != null ? "EXISTS" : "NULL")}");
    }
    
    /// <summary>
    /// Create the persistent check status display
    /// </summary>
    private void CreatePersistentCheckStatusUI()
    {
        Debug.Log("UIManager.CreatePersistentCheckStatusUI: ENTRY");
        
        GameObject persistentStatusObject = new GameObject("PersistentCheckStatusUI");
        persistentStatusObject.transform.SetParent(transform);
        
        persistentCheckStatus = persistentStatusObject.AddComponent<PersistentCheckStatusUI>();
        
        Debug.Log($"UIManager.CreatePersistentCheckStatusUI: Created PersistentCheckStatusUI component on GameObject '{persistentStatusObject.name}'");
        Debug.Log($"UIManager.CreatePersistentCheckStatusUI: PersistentCheckStatusUI.Instance = {(PersistentCheckStatusUI.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"UIManager.CreatePersistentCheckStatusUI: persistentCheckStatus reference = {(persistentCheckStatus != null ? "EXISTS" : "NULL")}");
    }
    
    /// <summary>
    /// Find existing UI components in the scene
    /// </summary>
    private void FindExistingUIComponents()
    {
        // Find existing CheckStatusUI
        checkStatus = FindFirstObjectByType<CheckStatusUI>();
        if (checkStatus != null)
        {
            Debug.Log("UIManager: Found existing CheckStatusUI");
        }
        
        // Find BoardRotationUI
        boardRotation = FindFirstObjectByType<BoardRotationUI>();
        if (boardRotation != null)
        {
            Debug.Log("UIManager: Found existing BoardRotationUI");
        }
        
        // Find PlacementUI
        placementUI = FindFirstObjectByType<PlacementUI>();
        if (placementUI != null)
        {
            Debug.Log("UIManager: Found existing PlacementUI");
        }
    }
    
    /// <summary>
    /// Validate that all required UI components are available
    /// </summary>
    private void ValidateUIComponents()
    {
        Debug.Log("UIManager: Validating UI components");
        
        if (enableCheckIndicator && checkIndicator == null)
        {
            Debug.LogWarning("UIManager: CheckIndicatorUI not found but is enabled");
        }
        
        if (enablePersistentCheckStatus && persistentCheckStatus == null)
        {
            Debug.LogWarning("UIManager: PersistentCheckStatusUI not found but is enabled");
        }
        
        if (checkStatus == null)
        {
            Debug.LogWarning("UIManager: CheckStatusUI not found in scene");
        }
        
        // Check if CheckDetectionManager is available
        if (CheckDetectionManager.Instance == null)
        {
            Debug.LogWarning("UIManager: CheckDetectionManager not found - check detection UI will not function");
        }
        
        Debug.Log("UIManager: UI component validation complete");
    }
    
    /// <summary>
    /// Get the check indicator UI component
    /// </summary>
    public CheckIndicatorUI GetCheckIndicator()
    {
        return checkIndicator;
    }
    
    /// <summary>
    /// Get the check status UI component
    /// </summary>
    public CheckStatusUI GetCheckStatus()
    {
        return checkStatus;
    }
    
    /// <summary>
    /// Get the persistent check status UI component
    /// </summary>
    public PersistentCheckStatusUI GetPersistentCheckStatus()
    {
        return persistentCheckStatus;
    }
    
    /// <summary>
    /// Test all check UI components
    /// </summary>
    [ContextMenu("Test Check UI")]
    public void TestCheckUI()
    {
        Debug.Log("UIManager: Testing all check UI components");
        
        if (checkIndicator != null)
        {
            checkIndicator.TestCheckIndicator(PieceColor.White, false);
        }
        
        if (persistentCheckStatus != null)
        {
            persistentCheckStatus.TestCheckStatus();
        }
        
        if (checkStatus != null)
        {
            checkStatus.TestCheckUI();
        }
    }
    
    /// <summary>
    /// Show check for a specific player using persistent status only
    /// </summary>
    public void ShowPlayerCheck(PieceColor playerColor)
    {
        Debug.Log($"UIManager: Showing check for {playerColor} (persistent status only)");
        
        // Update persistent status display (only UI system enabled)
        if (persistentCheckStatus != null && enablePersistentCheckStatus)
        {
            bool whiteInCheck = (playerColor == PieceColor.White);
            bool blackInCheck = (playerColor == PieceColor.Black);
            persistentCheckStatus.SetCheckStatus(whiteInCheck, blackInCheck);
        }
        
        // Note: CheckIndicatorUI and CheckStatusUI disabled for cleaner UI
    }
    
    /// <summary>
    /// Reset all check status to safe (persistent status only)
    /// </summary>
    public void HideAllCheckIndicators()
    {
        Debug.Log("UIManager: Resetting check status to safe (persistent status only)");
        
        // Reset persistent status to safe (only UI system enabled)
        if (persistentCheckStatus != null)
        {
            persistentCheckStatus.SetCheckStatus(false, false);
        }
        
        // Note: CheckIndicatorUI and CheckStatusUI disabled for cleaner UI
    }
    
    /// <summary>
    /// Show checkmate for a specific player (persistent status only)
    /// </summary>
    public void ShowPlayerCheckmate(PieceColor checkmatedPlayer)
    {
        Debug.Log($"UIManager: Showing checkmate for {checkmatedPlayer} (persistent status only)");
        
        // Update persistent status to show checkmate via check status
        if (persistentCheckStatus != null && enablePersistentCheckStatus)
        {
            bool whiteInCheck = (checkmatedPlayer == PieceColor.White);
            bool blackInCheck = (checkmatedPlayer == PieceColor.Black);
            persistentCheckStatus.SetCheckStatus(whiteInCheck, blackInCheck);
            // Note: Persistent status will show "CHECK!" for checkmate - could be enhanced later
        }
        
        // Note: CheckIndicatorUI and CheckStatusUI disabled for cleaner UI
    }
    
    /// <summary>
    /// Create a return to menu button for returning to the main menu
    /// </summary>
    private void CreateReturnToMenuButton()
    {
        Debug.Log("UIManager: Creating Return to Menu button");
        
        // Find or create a Canvas for the button
        Canvas uiCanvas = FindFirstObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            Debug.LogWarning("UIManager: No Canvas found for Return to Menu button");
            return;
        }
        
        // Create the button GameObject
        GameObject buttonObj = new GameObject("ReturnToMenuButton");
        buttonObj.transform.SetParent(uiCanvas.transform, false);
        
        // Add UI components
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        
        // Position the button in the top-right corner
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-120, -30);
        rectTransform.sizeDelta = new Vector2(100, 40);
        
        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TMPro.TextMeshProUGUI buttonText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        buttonText.text = "Menu";
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Position the text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Style the button
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark semi-transparent
        
        // Add click listener
        button.onClick.AddListener(() => {
            Debug.Log("UIManager: Return to Menu button clicked");
            ReturnToMainMenu();
        });
        
        Debug.Log("UIManager: Return to Menu button created successfully");
    }
    
    /// <summary>
    /// Return to the main menu scene
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("UIManager: Returning to main menu");
        
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("UIManager: SceneController.Instance not found - cannot return to menu");
        }
    }
}